using System.Collections;
using System.Collections.Generic;
using WitchDoctor.Utils;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using UnityEngine;

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

        private bool CharacterRenderFacingRight => _characterRenderTransform.rotation.eulerAngles.y == 0f;

        private Coroutine _playerDetectionCoroutine;

        [Space(5)]
        [Header("Debug Options")]
        [SerializeField] private bool _groundRaycastDims;
        [SerializeField] private bool _ledgeRaycastDims, _roofRaycastDims, _obstacleRaycastDims;
        #endregion

        #region Overrides
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
                // _playerCameraManager.FlipCameraFollow();
            }
            else if (orientation)
            {
                var rotator = new Vector3(transform.rotation.x, 180, transform.rotation.y);
                _characterRenderTransform.rotation = Quaternion.Euler(rotator);
                // _playerCameraManager.FlipCameraFollow();
            }
        }
        #endregion

        #region Utils
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
        #endregion
    }
}
