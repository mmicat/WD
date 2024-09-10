using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WitchDoctor.GameResources.CharacterScripts;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Utils;

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

        [Space(5)]

        [Header("Patrol Distance")]
        [SerializeField]
        private bool _displayPatrolLines = false;
        [SerializeField]
        private float _patrolLineGuideSize = 2f;
        [SerializeField]
        private float _leftTravelDistance = 3f;
        [SerializeField]
        private float _rightTravelDistance = 3f;
        private Vector3 _referencePosition;
        #endregion

        #region Overrides
        protected override void InitCharacter()
        {
            _maxHealth = _baseStats.BaseHealth;
            _contactDamage = _baseStats.ContactDamage;
            _rangedAttackDamage = _baseStats.BaseAttackDamage;

            GetVisionConeMesh();

            base.InitCharacter();
        }

        protected override void DeInitCharacter()
        {
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
#endif
            if (_displayVisionCone)
            {
                //SetupVisionConeMesh();
                if (_visionConeMesh == null)
                {
                    var mesh = GetVisionConeMesh();
                }
                
                Gizmos.DrawMesh(_visionConeMesh, _visionCone.position, _visionCone.rotation);
            }
        }
        #endregion

        #region Movement

        #endregion

        #region Utils
        private Mesh GetVisionConeMesh()
        {
            Mesh mesh = new Mesh();

            Vector3 origin = Vector3.zero;
            float fov = _viewAngle;
            int resolution = _visionConeResolution;
            float angle = 0f;
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