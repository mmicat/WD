using System.Collections;
using System.Collections.Generic;
using WitchDoctor.Utils;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using UnityEngine;
using Pathfinding;
using System.Linq;

namespace WitchDoctor.GameResources.CharacterScripts.Amalgam.GroundAmalgam
{
    public class AerialAmalgamManager : AmalgamEntity
    {
        #region Private Attributes
        [Space(5)]

        [Header("General")]
        [SerializeField]
        private Transform _characterRenderTransform;
        [SerializeField]
        private AmalgamStats _baseStats;
        private Rigidbody2D _rb;
        private AmalgamStates _amalgamStates;

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

        [Space(5)]

        [Header("Pathfinding")]
        [SerializeField]
        private AIPath _pathfinder;
        [SerializeField]
        private AIDestinationSetter _destinationSetter;
        [SerializeField, Tooltip("Define the positions where the enemy should cycle between during patrol")]
        private Vector2[] _targetPatrolPositions = new Vector2[1];
        private int _currWaypointIndex = 0;
        [SerializeField]
        private float _waypointRadiusDebug = 1f;
        private Vector3 _refrerencePosition;
        [SerializeField]
        private float _patrolStopWaitTime = 0f;
        [SerializeField]
        private float _flipThreshold = 0.5f;


        private bool CharacterRenderFacingRight => _characterRenderTransform.rotation.eulerAngles.y == 0f;

        private Coroutine _playerDetectionCoroutine;
        private Coroutine _waypointTraversalCoroutine;

        [Space(5)]
        [Header("Debug Options")]
        [SerializeField] private bool _groundRaycastDims;
        [SerializeField] private bool _ledgeRaycastDims, _roofRaycastDims, _obstacleRaycastDims, _showPatrolPositions;
        #endregion

        #region Overrides
        protected override void InitCharacter()
        {
            _rb = GetComponent<Rigidbody2D>();
            // _rb.gravityScale = 0;

            _maxHealth = _baseStats.BaseHealth;
            _contactDamage = _baseStats.ContactDamage;

            if (_pathfinder == null) _pathfinder = GetComponent<AIPath>();
            _pathfinder.maxSpeed = _baseStats.MovementSpeed;
            if (_destinationSetter == null) _destinationSetter = GetComponent<AIDestinationSetter>();
            _refrerencePosition = transform.position;
            _pathfinder.destination = _refrerencePosition;

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

        protected override void DisplayDebugElements()
        {
#if UNITY_EDITOR
            Gizmos.color = new Color(1, 0, 1, 0.3f);

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

            Gizmos.color = new Color(0, 1, 0, 0.8f);
            if (_showPatrolPositions)
            {
                var refPos = Application.isPlaying ? _refrerencePosition : transform.position;
                var initPos = refPos + (Vector3)_targetPatrolPositions[0];
                Gizmos.DrawSphere(initPos, _waypointRadiusDebug);
                
                for (int i = 1; i < _targetPatrolPositions.Length; i++)
                {
                    var pos = refPos + (Vector3)_targetPatrolPositions[i];
                    var prevPos = refPos + (Vector3)_targetPatrolPositions[i - 1];
                    Gizmos.DrawLine(prevPos, pos);
                    Gizmos.DrawSphere(pos, _waypointRadiusDebug);
                }

                if (_targetPatrolPositions.Length > 2)
                {
                    var finalPos = refPos + (Vector3)_targetPatrolPositions[_targetPatrolPositions.Length - 1];
                    Gizmos.DrawLine(finalPos, initPos);
                }
            }

            Gizmos.color = new Color(1, 0, 1, 0.3f);
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

        protected override void SetManagerContexts()
        {

        }

        protected override void UpdateAmalgamStates()
        {
            if (!_amalgamStates.alert && _waypointTraversalCoroutine == null)
            {
                _waypointTraversalCoroutine = StartCoroutine(TraverseWaypoints());
            }
        }
        #endregion

        #region Unity Methods
        private void Update()
        {
            UpdateAmalgamStates();
        }

        private void FixedUpdate()
        {
            CheckForFlip();
        }
        #endregion

        #region Movement
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

        private IEnumerator TraverseWaypoints()
        {
            while (!_amalgamStates.alert)
            {
                var waypoint = _refrerencePosition + (Vector3)_targetPatrolPositions[_currWaypointIndex];
                _pathfinder.destination = waypoint;
                _pathfinder.SearchPath();

                yield return new WaitUntil(() => _pathfinder.reachedDestination);
                yield return new WaitForSeconds(_patrolStopWaitTime);
                _currWaypointIndex = _currWaypointIndex == _targetPatrolPositions.Length - 1 ? 0 : _currWaypointIndex + 1;
            }
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
                    // Check if there is a collider
                    _amalgamStates.alert = true;
                    alertCounter = 0;
                    _playerTransform = colliders.First().transform;
                    _destinationSetter.target = _playerTransform;

                    if (_waypointTraversalCoroutine != null)
                    {
                        StopCoroutine(_waypointTraversalCoroutine);
                        _waypointTraversalCoroutine = null;
                    }
                }
                else if (_amalgamStates.alert)
                {
                    alertCounter++;
                    if (alertCounter > _alertMaxSteps)
                    {
                        _amalgamStates.alert = false;
                        _playerTransform = null;
                        _destinationSetter.target = null;
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
            int resolution = _visionConeResolution;
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

        private void CheckForFlip()
        {
            var currVelocity = _pathfinder.velocity;
            // Debug.Log(currVelocity);
            if (Mathf.Abs(currVelocity.x) > _flipThreshold)
            {
                if ((currVelocity.x < 0 && CharacterRenderFacingRight) || 
                    (currVelocity.x > 0 && !CharacterRenderFacingRight))
                {
                    Flip();
                }
            }
        }
        #endregion
    }
}
