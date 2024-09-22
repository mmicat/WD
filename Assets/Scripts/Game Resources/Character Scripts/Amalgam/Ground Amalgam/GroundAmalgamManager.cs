using System.Collections;
using UnityEngine;
using WitchDoctor.Utils;
using WitchDoctor.GameResources.Utils.ScriptableObjects;

namespace WitchDoctor.GameResources.CharacterScripts.Amalgam.GroundAmalgam
{
    public class GroundAmalgamManager : AmalgamEntity
    {
        #region Private Properties
        [Space(5)]

        [Header("General")]
        [SerializeField]
        private Transform _characterRenderTransform;
        [SerializeField]
        private AmalgamStats _baseStats;
        private Rigidbody2D _rb;
        private AmalgamStates _amalgamStates;
        private int _rangedAttackDamage;

        [Space(5)]

        [Header("Animations and Visual Effects")]
        [SerializeField]
        private Animator _animator;

        [Space(5)]

        [Header("Platform Checks")]
        [SerializeField]
        private Transform _groundTransform;
        [SerializeField]
        private Transform _ledgeTransform;
        [SerializeField]
        private Transform _roofTransform;
        [SerializeField]
        private Transform _obstacleTransform;
        [SerializeField, Tooltip("The Amount of time (in seconds) to pause after an obstacle is encounter")]
        private float _patrolStopWaitTime = 0f; 

        [Space(5)]

        [Header("Patrol Distance")]
        
        [SerializeField]
        private float _patrolLineGuideSize = 2f;
        [SerializeField]
        private float _leftTravelDistance = 3f;
        [SerializeField]
        private float _rightTravelDistance = 3f;
        private Vector3 _referencePosition;

        private bool CharacterRenderFacingRight => _characterRenderTransform.rotation.eulerAngles.y == 0f;

        private bool _isAirborne = true;

        private Coroutine _flipCoroutine;
        private Coroutine _playerDetectionCoroutine;

        [Space(5)]
        [Header("Debug Options")]
        [SerializeField] private bool _groundRaycastDims;
        [SerializeField] private bool _ledgeRaycastDims, _roofRaycastDims;
        [SerializeField] private bool _displayPatrolLines = false;
        #endregion

        #region Platform Checks
        public bool IsGrounded => Physics2D.BoxCast(_groundTransform.position, new Vector2(_baseStats.GroundCheckX, _baseStats.GroundCheckY),
            0f, Vector2.down, _baseStats.GroundCheckDist, _baseStats.GroundLayer);

        public bool IsNextToLedge => !Physics2D.BoxCast(_ledgeTransform.position, new Vector2(_baseStats.LedgeCheckX, _baseStats.LedgeCheckY),
            0f, Vector2.down, _baseStats.LedgeCheckDist, _baseStats.GroundLayer);

        public bool IsRoofed => Physics2D.BoxCast(_roofTransform.position, new Vector2(_baseStats.RoofCheckX, _baseStats.RoofCheckY),
            0f, Vector2.up, _baseStats.RoofCheckDist, _baseStats.GroundLayer);

        public bool IsBlocked => Physics2D.BoxCast(_obstacleTransform.position, new Vector2(_baseStats.ObstacleCheckX, _baseStats.ObstacleCheckY),
            0f, Vector2.up, _baseStats.ObstacleCheckDist, _baseStats.GroundLayer);
        #endregion

        #region Overrides
        protected override void InitCharacter()
        {
            _maxHealth = _baseStats.BaseHealth;
            _contactDamage = _baseStats.ContactDamage;
            _rangedAttackDamage = _baseStats.BaseAttackDamage;

            GetVisionConeMesh();

            if (_amalgamStates == null) _amalgamStates = new AmalgamStates();
            _amalgamStates.Reset();

            if (_playerDetectionCoroutine != null)
            {
                StopCoroutine(_playerDetectionCoroutine);
                _playerDetectionCoroutine = null;
            }

            _playerDetectionCoroutine = StartCoroutine(CheckForPlayer());

            base.InitCharacter();
        }

        protected override void DeInitCharacter()
        {
            if (_playerDetectionCoroutine != null)
            {
                StopCoroutine( _playerDetectionCoroutine );
                _playerDetectionCoroutine = null;
            }

            _amalgamStates.Reset();

            base.DeInitCharacter();
        }

        protected override void SetManagerContexts()
        {

        }

        protected override void OnDamageTaken(int damage)
        {
            base.OnDamageTaken(damage);

            Debug.Log($"Amalgam Took Damage: {damage}.\nCurrent Health: {CurrHealth}");
        }

        protected override void OnDeath()
        {
            base.OnDeath();

            Debug.Log("Amalgam Died");
        }

        protected override void UpdateAmalgamStates()
        {
            if (IsGrounded)
            {
                _isAirborne = false;
            }
            else
            {
                _isAirborne = true;
            }
        }

        protected override void DisplayDebugElements()
        {
#if UNITY_EDITOR
            Gizmos.color = new Color(1, 0, 1, 0.3f);
            if (_displayPatrolLines && !Application.isPlaying)
            {
                var pos = transform.position;
                var left = pos + (_leftTravelDistance * Vector3.left);
                var right = pos + (_rightTravelDistance * Vector3.right);
                var guideSize = new Vector3(1, 3, 1);
                Gizmos.DrawLine(transform.position, left);
                Gizmos.DrawLine(transform.position, right);
                Gizmos.DrawCube(left, _patrolLineGuideSize * guideSize);
                Gizmos.DrawCube(right, _patrolLineGuideSize * guideSize);
            }

            if (_displayVisionCone)
            {
                //SetupVisionConeMesh();
                if (_visionConeMesh == null)
                {
                }
                    var mesh = GetVisionConeMesh();
                
                float zRot = CharacterRenderFacingRight ? 0 : 180;
                Quaternion rotation = Quaternion.Euler(0f, 0f, zRot);

                Gizmos.DrawMesh(_visionConeMesh, _visionConeCenter.position, rotation);
            }

            if (_groundRaycastDims)
            {
                Vector3 centerPos = _groundTransform.position + (Vector3.down * _baseStats.GroundCheckDist);
                Vector3 cubeSize = new Vector3(_baseStats.GroundCheckX, _baseStats.GroundCheckY, 0f);
                Gizmos.DrawLine(_groundTransform.position, centerPos);
                Gizmos.DrawWireCube(centerPos, cubeSize);
            }

            if (_ledgeRaycastDims)
            {
                Vector3 centerPos = _ledgeTransform.position + (Vector3.down * _baseStats.LedgeCheckDist);
                Vector3 cubeSize = new Vector3(_baseStats.LedgeCheckX, _baseStats.LedgeCheckY, 0f);
                Gizmos.DrawLine(_ledgeTransform.position, centerPos);
                Gizmos.DrawWireCube(centerPos, cubeSize);
            }

            if (_roofRaycastDims)
            {
                Vector3 centerPos = _roofTransform.position + (Vector3.up * _baseStats.RoofCheckDist);
                Vector3 cubeSize = new Vector3(_baseStats.RoofCheckX, _baseStats.RoofCheckY, 0f);
                Gizmos.DrawLine(_roofTransform.position, centerPos);
                Gizmos.DrawWireCube(centerPos, cubeSize);
            }
#endif
        }
        #endregion

        #region Unity Methods
        private void Update()
        {
            UpdateAmalgamStates();
        }
        #endregion

        #region Movement
        private void Walk()
        {
            if (_amalgamStates.walking)
            {
                float dir = CharacterRenderFacingRight ? 1f : -1f;
                _rb.velocity = new Vector2(dir * _baseStats.WalkSpeed, _rb.velocity.y);


                // _animator.SetBool("Walking", _playerStates.walking);
            }
        }

        private IEnumerator ObstacleCoroutine()
        {
            _amalgamStates.walking = false;           
            yield return new WaitForSeconds(_patrolStopWaitTime);
            Flip();
            _amalgamStates.walking = true;
        }

        private void Flip()
        {

        }
        #endregion

        #region Utils
        private IEnumerator CheckForPlayer()
        {
            int alertCounter = 0;
            while (true)
            {
                var colliders = Physics2D.OverlapCircleAll(_visionConeCenter.position, _visionRadius, _baseStats.AmalgamAttackableLayers);
                if (colliders != null && colliders.Length > 0)
                {
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        // Check if collider is in the specified range
                        Vector3 dist = colliders[i].transform.position - _visionConeCenter.position;
                        float angle = Vector3.Angle(colliders[i].transform.position, _visionConeCenter.position);
                        bool inFront = (CharacterRenderFacingRight == dist.x > 0);
                        bool inVisionCone = angle < _viewAngle / 2;
                        _amalgamStates.alert = inFront && inVisionCone;

                        if (_amalgamStates.alert) alertCounter = 0;
                    }
                }
                else if (_amalgamStates.alert)
                {
                    alertCounter++;
                    if (alertCounter > _alertMaxSteps)
                    {
                        _amalgamStates.alert = false;
                        alertCounter = 0;
                    }
                }

                yield return new WaitForSeconds(_visionConeRefreshTime);
            }
        }

        private Mesh GetVisionConeMesh()
        {
            Mesh mesh = new Mesh();

            Vector3 origin = Vector3.zero;
            float fov = _viewAngle;
            int resolution = _visionConeResolution ;
            float angle = 0f + (fov / 2);
            float angleIncrease = fov / resolution;
            float viewDistance = _visionRadius;

            Vector3[] vertices = new Vector3[resolution + 1 + 1];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[resolution * 3];

            vertices[0] = origin;

            int vertexIndex = 1;
            int triangleIndex = 0;

            for (int i = 0; i <= resolution; i++)
            {
                Vector3 vertex = origin + ConversionUtils.ConvertAngleToVector(angle) * viewDistance;
                vertices[vertexIndex] = vertex;

                if (i > 0)
                {

                    triangles[triangleIndex] = 0;
                    triangles[triangleIndex + 1] = vertexIndex - 1;
                    triangles[triangleIndex + 2] = vertexIndex;

                    triangleIndex += 3;
                }

                vertexIndex++;
                angle -= angleIncrease;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            _visionConeMesh = mesh;
            // _visionCone.GetComponent<MeshFilter>().mesh = mesh;
            return mesh;
        }
        #endregion
    }
}