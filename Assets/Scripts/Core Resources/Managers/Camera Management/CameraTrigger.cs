using Cinemachine;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WitchDoctor.CoreResources.Managers.CameraManagement
{
    public enum PanDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [RequireComponent(typeof(Collider2D))]
    public class CameraTrigger : MonoBehaviour
    {
        public CamTriggerInspectorObjects inspectorObjects;
        
        private Collider2D _collider;


        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (inspectorObjects.panCameraOnContact)
                {
                    // Pan the camera based on the pan direction in the inspector
                    CameraManager.Instance.PanCamera(inspectorObjects.panDistance, inspectorObjects.panTime, inspectorObjects.panDirection, false);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Vector2 exitDirection = (collision.transform.position - _collider.bounds.center).normalized;

                if (inspectorObjects.swapCameras && inspectorObjects.leftCam != null && inspectorObjects.rightCam != null)
                {
                    CameraManager.Instance.SwapCamera(inspectorObjects.leftCam, inspectorObjects.rightCam, exitDirection);
                }

                else if (inspectorObjects.panCameraOnContact)
                {
                    // Pan the camera back to the starting position
                    CameraManager.Instance.PanCamera(inspectorObjects.panDistance, inspectorObjects.panTime, inspectorObjects.panDirection, true);
                }
            }
        }
    }

    [Serializable]
    public class CamTriggerInspectorObjects
    {
        public bool swapCameras = false;
        public bool panCameraOnContact = false;

        [HideInInspector] public CinemachineVirtualCamera leftCam;
        [HideInInspector] public CinemachineVirtualCamera rightCam;

        [HideInInspector] public PanDirection panDirection;
        [HideInInspector] public float panDistance = 3f;
        [HideInInspector] public float panTime = 0.35f;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CameraTrigger))]
    public class CamTriggerScriptEditor : Editor
    {
        private CameraTrigger cameraTrigger;

        private void OnEnable()
        {
            cameraTrigger = (CameraTrigger)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (cameraTrigger.inspectorObjects.swapCameras)
            {
                cameraTrigger.inspectorObjects.leftCam = 
                    EditorGUILayout.ObjectField(
                        "Camera on Left", 
                        cameraTrigger.inspectorObjects.leftCam, 
                        typeof(CinemachineVirtualCamera), 
                        true) as CinemachineVirtualCamera;

                cameraTrigger.inspectorObjects.rightCam =
                    EditorGUILayout.ObjectField(
                        "Camera on Right",
                        cameraTrigger.inspectorObjects.rightCam,
                        typeof(CinemachineVirtualCamera),
                        true) as CinemachineVirtualCamera;
            }

            if (cameraTrigger.inspectorObjects.panCameraOnContact)
            {
                cameraTrigger.inspectorObjects.panDirection = 
                    (PanDirection)EditorGUILayout.EnumPopup(
                        "Camera Pan Direction", 
                        cameraTrigger.inspectorObjects.panDirection);

                cameraTrigger.inspectorObjects.panDistance = EditorGUILayout.FloatField("Pan Distance", cameraTrigger.inspectorObjects.panDistance);
                cameraTrigger.inspectorObjects.panTime = EditorGUILayout.FloatField("Pan Time", cameraTrigger.inspectorObjects.panTime);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(cameraTrigger);
            }
        }
    }
#endif
}