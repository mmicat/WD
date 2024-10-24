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
        [SerializeField, Tooltip("The Amount of time (in seconds) to pause after an obstacle is encountered")]
        private float _patrolStopWaitTime = 0f; 

        [Space(5)]

        [Header("Patrol Distance")]
        
        [SerializeField]
        private float _patrolLineGuideSize = 2f;
        [SerializeField]
        private float _leftTravelDistance = 3f;
        [SerializeField]
        private float _rightTravelDistance = 3f;
        [SerializeField]
        private float _hurtRefreshSeconds = 0.8f;
        private Vector3 _referencePosition;

        private bool CharacterRenderFacingRight => _characterRenderTransform.rotation.eulerAngles.y == 0f;

        private bool _isAirborne = true;
        private bool _prevAirborneCheck = true;
        private bool _flipStarted = false;

        private float _defaultGravity;
        private int _stepsXRecoiled = 0;
        private int _stepsYRecoiled = 0;
        private bool _animateRecoil = false;
        private Vector2 _damageDir;
        [SerializeField]
        private float dist_Debug;
        [SerializeField]
        private bool _charFacingRight_Debug;

        private Coroutine _flipCoroutine;
        private Coroutine _playerDetectionCoroutine;

        [Space(5)]
        [Header("Debug Options")]
        [SerializeField] private bool _groundRaycastDims;
        [SerializeField] private bool _ledgeRaycastDims, _roofRaycastDims, _obstacleRaycastDims;
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
            _rb = GetComponent<Rigidbody2D>();

            _maxHealth = _baseStats.BaseHealth;
            _contactDamage = _baseStats.ContactDamage;
            _rangedAttackDamage = _baseStats.BaseAttackDamage;
            _defaultGravity = _rb.gravityScale;

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

            ResetManager();
            _amalgamStates.Reset();

            base.DeInitCharacter();
        }

        protected override void SetManagerContexts()
        {

        }

        protected override void OnDamageTaken(int damage, Transform attacker)
        {
            base.OnDamageTaken(damage, attacker);

            Debug.Log($"Amalgam Took Damage: {damage}.\nCurrent Health: {CurrHealth}");

            _amalgamStates.alert = true;
            _playerTransform = attacker;
            _damageDir = Vector3.Normalize(attacker.position - transform.position);
            SetRecoil();
        }

        protected override void OnDeath()
        {
            ResetManager();
            Debug.Log("Amalgam Died");
            _amalgamStates.dead = true;

            base.OnDeath();
        }

        protected override void UpdateAmalgamStates()
        {
            _isAirborne = !IsGrounded;

            if (_prevAirborneCheck != _isAirborne) // gotta check the difference for certain actions
            {
                if (!_isAirborne)
                {
                    // Setup patrol distances
                    _referencePosition = transform.position;
                }
            }

            _prevAirborneCheck = _isAirborne;
            float dist = transform.position.x - _referencePosition.x;
            
            // dist_Debug = transform.position.x - _referencePosition.x;
            // _charFacingRight_Debug = CharacterRenderFacingRight;

            if (!_isAirborne)
            {
                _amalgamStates.blockedByObstacle = 
                    (dist <= -_leftTravelDistance && !CharacterRenderFacingRight) || 
                    (dist >= _rightTravelDistance && CharacterRenderFacingRight) || 
                    IsBlocked || IsNextToLedge;
                _amalgamStates.walking = !_amalgamStates.blockedByObstacle && !_amalgamStates.IsRecoiling;
            }
            else
                _amalgamStates.walking = false;
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

            if (_obstacleRaycastDims)
            {
                Vector3 centerPos = _obstacleTransform.position + (Vector3.up * _baseStats.ObstacleCheckDist);
                Vector3 cubeSize = new Vector3(_baseStats.ObstacleCheckX, _baseStats.ObstacleCheckY, 0f);
                Gizmos.DrawLine(_obstacleTransform.position, centerPos);
                Gizmos.DrawWireCube(centerPos, cubeSize);
            }
#endif
        }
        #endregion

        #region Unity Methods
        private void Update()
        {
            UpdateAmalgamStates();
            UpdateAnimations();
            LookAtPlayer();
            Recoil();
        }

        private void FixedUpdate()
        {
            // Walk();
            ObstacleCheck();
            ProcessRecoil();
        }
        #endregion

        #region Movement
        private void Walk()
        {
            if (!_isAirborne && !_amalgamStates.dead)
            {
                if (_amalgamStates.walking)
                {
                    float dir = CharacterRenderFacingRight ? 1f : -1f;
                    _rb.velocity = new Vector2(dir * _baseStats.MovementSpeed, _rb.velocity.y);
                }
                else
                    _rb.velocity = Vector3.zero;
            }
        }

        private void ObstacleCheck()
        {
            if (_amalgamStates.blockedByObstacle && !_flipStarted && !_amalgamStates.dead)
            {

                if (_amalgamStates.alert)
                {
                    _amalgamStates.walking = false;
                    return;
                }
                
                if (_flipCoroutine != null)
                {
                    StopCoroutine(_flipCoroutine);
                }

                _flipCoroutine = StartCoroutine(ObstacleCoroutine());
            }
        }

        private IEnumerator ObstacleCoroutine()
        {
            _flipStarted = true;
            yield return new WaitForSeconds(_patrolStopWaitTime);
            Flip();
            _flipStarted = false;
        }

        private void Flip()
        {
            var orientation = CharacterRenderFacingRight;
            if (!orientation)
            {
                var rotator = new Vector3(transform.rotation.x, 0, transform.rotation.y);
                _characterRenderTransform.rotation = Quaternion.Euler(rotator);
            }
            else if (orientation)
            {
                var rotator = new Vector3(transform.rotation.x, 180, transform.rotation.y);
                _characterRenderTransform.rotation = Quaternion.Euler(rotator);
            }
        }

        private void SetRecoil()
        {
            if (_amalgamStates.dead)
                return;
            if (!IsGrounded) _amalgamStates.recoilingY = true;
            _amalgamStates.recoilingX = true;

            StartCoroutine(SetHurtAnimation());
        }

        /// <summary>
        /// Adds a recoil force to the player
        /// </summary>
        /// <param name="runAnimation">Determines whether to run associated animation</param>
        /// <param name="cancelActions">determines whether to cancel other actions</param>
        private void Recoil(bool runAnimation = true)
        {
            if (_amalgamStates.recoilingX || _amalgamStates.recoilingY)
            {
                //if (cancelActions)
                //    InterruptMovementActions(true, true);

                _animateRecoil = runAnimation;

                var finalVelocity = Vector2.zero;

                //since this is run after Walk, it takes priority, and effects momentum properly.
                if (_amalgamStates.recoilingX)
                {
                    if (CharacterRenderFacingRight)
                    {
                        finalVelocity = new Vector2(-_baseStats.recoilXSpeed, 0);
                    }
                    else
                    {
                        finalVelocity = new Vector2(_baseStats.recoilXSpeed, 0);
                    }
                }
                if (_amalgamStates.recoilingY)
                {
                    if (_damageDir.y < 0)
                    {
                        finalVelocity = new Vector2(finalVelocity.x, _baseStats.recoilYSpeed);
                        _rb.gravityScale = _baseStats.recoilGravityScale;
                    }
                    else
                    {
                        finalVelocity = new Vector2(finalVelocity.x, -_baseStats.recoilYSpeed);
                        _rb.gravityScale = _baseStats.recoilGravityScale;
                    }

                }

                var decayFactor = Mathf.Pow(_baseStats.recoilSpeedDecayConstant, Mathf.Max(_stepsYRecoiled, _stepsXRecoiled));
                _rb.velocity = finalVelocity / decayFactor;
            }
            else
            {
                _rb.gravityScale = _defaultGravity;
            }
        }

        private void ProcessRecoil()
        {
            if (!_amalgamStates.IsRecoiling)
            {
                if (_animateRecoil)
                {
                    _damageDir = Vector2.zero;
                    _animateRecoil = false;
                }

                return;
            }

            if (_amalgamStates.recoilingX == true && _stepsXRecoiled < _baseStats.recoilXSteps)
            {
                _stepsXRecoiled++;
            }
            else
            {
                StopRecoilX();
            }
            if (_amalgamStates.recoilingY == true && _stepsYRecoiled < _baseStats.recoilYSteps)
            {
                _stepsYRecoiled++;
            }
            else
            {
                StopRecoilY();
            }
            if (IsGrounded)
            {
                StopRecoilY();
            }
        }

        private void StopRecoilX()
        {
            _stepsXRecoiled = 0;
            _amalgamStates.recoilingX = false;
        }

        private void StopRecoilY()
        {
            _stepsYRecoiled = 0;
            _amalgamStates.recoilingY = false;
        }
        #endregion

        #region Utils
        protected void UpdateAnimations()
        {
            _animator.SetBool("Walking", _amalgamStates.walking);
            _animator.SetBool("Hurt", _amalgamStates.hurt);
        }

        private void ResetManager()
        {
            _rb.gravityScale = _defaultGravity;
            _stepsXRecoiled = 0;
            _stepsYRecoiled = 0;
            _animateRecoil = false;
            _damageDir = Vector2.zero;
        }

        private void LookAtPlayer()
        {
            if (_amalgamStates.alert)
            {
                float xDistFromPlayer = (_playerTransform == null ? 0 : _playerTransform.position.x - transform.position.x);
                bool playerBehindAmalgam = (xDistFromPlayer < 0 && CharacterRenderFacingRight) ||
                    (xDistFromPlayer > 0 && !CharacterRenderFacingRight);

                if (playerBehindAmalgam)
                {
                    Flip();
                }
            }
        }

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

                        if (_amalgamStates.alert)
                        {
                            alertCounter = 0;
                            _playerTransform = colliders[i].transform;
                        }
                    }
                }
                else if (_amalgamStates.alert)
                {
                    alertCounter++;
                    if (alertCounter > _alertMaxSteps)
                    {
                        _amalgamStates.alert = false;
                        _playerTransform = null;
                        alertCounter = 0;
                    }
                }

                yield return new WaitForSeconds(_visionConeRefreshTime);
            }
        }

        private IEnumerator SetHurtAnimation()
        {
            _amalgamStates.hurt = true;
            yield return new WaitForSeconds(_hurtRefreshSeconds);
            _amalgamStates.hurt = false;
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
            return mesh;
        }
        #endregion
    }
}