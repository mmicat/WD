using System;
using System.Collections;
using UnityEngine;
using Cinemachine;
using WitchDoctor.CoreResources.Utils.Singleton;
using System.Linq;

namespace WitchDoctor.CoreResources.Managers.CameraManagement
{
    public class CameraManager : MonoSingleton<CameraManager>
    {
        private CinemachineVirtualCamera _currentCamera;
        [SerializeField] private CinemachineVirtualCamera[] _allVirtualCameras;

        private Coroutine _lerpTPanCoroutine;
        private CinemachineFramingTransposer _framingTransposer;

        #region Player Camera Tuning Variables
        [Header("Controls for lerping the Y Damping during player jump/fall")]
        [SerializeField] private float _fallPanAmount = 0.25f;
        [SerializeField] private float _fallYPanTime = 0.35f;
        
        public static bool IsLerpingYDamping { get; private set; }
        public static bool LerpedFromPlayerFalling { get; private set; }


        private float _normYPanAmount;
        #endregion

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            for (int i = 0; i < _allVirtualCameras.Length; i++)
            {
                if (_allVirtualCameras[i].enabled)
                {
                    // set the current active camera
                    _currentCamera = _allVirtualCameras[i];

                    // set the framing transposer
                    _framingTransposer = _currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

                }
            }

            // set the YDamping amount so that it's based on the inspector value
            _normYPanAmount = _framingTransposer.m_YDamping;
        }

        public override void CleanSingleton()
        {
            base.CleanSingleton();
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Lerp the Y Damping value of the framing transposer 
        /// for the current virtual camera. Helps improve the 
        /// camera when falling
        /// </summary>
        /// <param name="isPlayerFalling">
        /// Changes the lerp method depending on whether the 
        /// player is falling
        /// </param>
        public void LerpYDamping(bool isPlayerFalling)
        {
            _lerpTPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
        }


        /// <summary>
        /// Change the confiner for the main player camera for each round 
        /// </summary>
        /// <param name="confiningShape">
        /// The collider to confine the camera in
        /// </param>
        public void ChangeCameraConfiner(Collider2D confiningShape)
        {
            var playerCamera = FindPlayerCamera();

            var confiner = playerCamera.GetComponent<CinemachineConfiner>();
            confiner.m_BoundingShape2D = confiningShape;
        }

        /// <summary>
        /// Set the follow target for the player camera
        /// </summary>
        /// <param name="toFollow">
        /// Transform to follow
        /// </param>
        public void SetCameraFollow(Transform toFollow)
        {
            var playerCamera = FindPlayerCamera();

            playerCamera.Follow = toFollow;
        }
        #endregion

        #region Internal Methods
        private CinemachineVirtualCamera FindPlayerCamera()
        {
            if (_allVirtualCameras == null || _allVirtualCameras.Length == 0)
                return null;

            var cam = _allVirtualCameras.First((x) => x.tag == "PlayerCamera");

            return cam;
        }

        private IEnumerator LerpYAction(bool isPlayerFalling)
        {
            IsLerpingYDamping = true;

            // grab the starting damping amount
            float startDampAmount = _framingTransposer.m_YDamping;
            float endDampAmount = 0f;

            // determine the end of the damping amount
            if (isPlayerFalling)
            {
                endDampAmount = _fallPanAmount;
                LerpedFromPlayerFalling = true;
            }

            else
            {
                LerpedFromPlayerFalling = false;
                endDampAmount = _normYPanAmount;
            }

            // lerp the pan amount
            float elapsedTime = 0f;
            while (elapsedTime < _fallYPanTime)
            {
                elapsedTime += Time.deltaTime;

                float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, (elapsedTime / _fallYPanTime));
                _framingTransposer.m_YDamping = lerpedPanAmount;

                yield return null;
            }

            IsLerpingYDamping = false;
        }
        #endregion

    }
}