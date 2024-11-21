using System;
using System.Collections;
using UnityEngine;
using Cinemachine;
using WitchDoctor.CoreResources.Utils.Singleton;
using System.Linq;
using System.Collections.Generic;

namespace WitchDoctor.CoreResources.Managers.CameraManagement
{
    public class CameraManager : DestroyableMonoSingleton<CameraManager>
    {
        private CinemachineVirtualCamera _currentCamera;
        [SerializeField] private CinemachineVirtualCamera[] _allVirtualCameras;
        [SerializeField] private float _cameraResetPanTime = 1.5f;

        private Coroutine _lerpTPanCoroutine;
        private Coroutine _panCameraCoroutine;

        private CinemachineFramingTransposer _framingTransposer;

        #region Player Camera Tuning Variables
        [Header("Controls for lerping the Y Damping during player jump/fall")]
        [SerializeField] private float _fallPanAmount = 0.25f;
        [SerializeField] private float _fallYPanTime = 0.35f;

        private float _initialYPanAmount;
        private Vector3 _initialTrackedObjectOffset;

        public static bool IsLerpingYDamping { get; private set; }
        public static bool LerpedFromPlayerFalling { get; private set; }
        #endregion

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            for (int i = 0; i < _allVirtualCameras.Length; i++)
            {
                if (_allVirtualCameras[i].enabled)
                {
                    SetCurrentCamera(_allVirtualCameras[i]);
                }
            }

            if (_framingTransposer == null) return;

            // set the initial YDamping amount so that it's based on the inspector value
            _initialYPanAmount = _framingTransposer.m_YDamping;

            // set the initial position of the tracked object offset
            _initialTrackedObjectOffset = _framingTransposer.m_TrackedObjectOffset;
        }

        public override void CleanSingleton()
        {
            ResetCurrentCamera(true);

            base.CleanSingleton();
        }
        #endregion

        #region Public Methods
        public void SetupCameraSystem(CinemachineVirtualCamera[] virtualCameras)
        {
            _allVirtualCameras = virtualCameras;

            for (int i = 0; i < _allVirtualCameras.Length; i++)
            {
                if (_allVirtualCameras[i].enabled)
                {
                    SetCurrentCamera(_allVirtualCameras[i]);
                }
            }
        }

        /// <summary>
        /// Change the confiner for the main player camera for each round 
        /// </summary>
        /// <param name="confiningShape">
        /// The collider to confine the camera in
        /// </param>
        public void ChangeCameraConfiner(Collider2D confiningShape)
        {
            var playerCameras = FindPlayerCameras();

            for (int i = 0; i < playerCameras.Count; i++)
            {
                var confiner = playerCameras[i].GetComponent<CinemachineConfiner>();
                confiner.m_BoundingShape2D = confiningShape;
            }
        }

        /// <summary>
        /// Set the follow target for the player camera
        /// </summary>
        /// <param name="toFollow">
        /// Transform to follow
        /// </param>
        public void SetCameraFollow(Transform toFollow)
        {
            var playerCameras = FindPlayerCameras();

            for (int i = 0; i < playerCameras.Count; i++)
            {
                playerCameras[i].Follow = toFollow;
            }
        }

        /// <summary>
        /// Lerp the Y Damping value of the framing transposer 
        /// for the current virtual camera. Helps improve the 
        /// camera when falling
        /// </summary>
        /// <param name="isPlayerFalling">
        /// Changes the lerp method depending on whether the 
        /// player is falling
        /// </param>
        public void LerpTransposerYDamping(bool isPlayerFalling)
        {
            if (_lerpTPanCoroutine != null)
            {
                StopCoroutine(_lerpTPanCoroutine);
            }

            _lerpTPanCoroutine = StartCoroutine(LerpYDamping_Coroutine(isPlayerFalling));
        }

        /// <summary>
        /// Pan Camera to a specified direction and 
        /// distance as an offset of the original 
        /// tracked object offset.
        /// </summary>
        /// <param name="panDistance">the total distance of the pan</param>
        /// <param name="panTime">the time for the pan to complete</param>
        /// <param name="panDirection">the direction of the pan</param>
        /// <param name="panToStartingPos">set true to set back to initial position</param>
        public void PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos, Action onPanComplete = null)
        {
            if (_panCameraCoroutine != null)
            {
                StopCoroutine(_panCameraCoroutine);
            }

            _panCameraCoroutine = StartCoroutine(PanCamera_Coroutine(panDistance, panTime, panDirection, panToStartingPos, onPanComplete));
        }

        public void SwapCamera(CinemachineVirtualCamera leftCam, CinemachineVirtualCamera rightCam, Vector2 triggerExitDirection)
        {
            // if the current camera is the camera on the left and our trigger exit direction was on the right
            if (_currentCamera == leftCam && triggerExitDirection.x > 0f)
            {
                // activate the new camera
                rightCam.enabled = true;

                // deactivate the old camera
                leftCam.enabled = false;

                // set the new camera as the current camera
                SetCurrentCamera(rightCam);
            }

            // if the current camera is the camera on the right and our trigger exit direction was on the left
            if (_currentCamera == rightCam && triggerExitDirection.x < 0f)
            {
                // activate the new camera
                leftCam.enabled = true;

                // deactivate the old camera
                rightCam.enabled = false;

                // set the new camera as the current camera
                SetCurrentCamera(leftCam);
            }
        }
        #endregion

        #region Internal Methods
        private void SetCurrentCamera(CinemachineVirtualCamera currCam)
        {
            if (_currentCamera == null)
            {
                _currentCamera = currCam;
                _framingTransposer = currCam.GetCinemachineComponent<CinemachineFramingTransposer>();
                return;
            }

            ResetCurrentCamera(false, () => { 
                _currentCamera = currCam;
                _framingTransposer = currCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            });
        }


        private List<CinemachineVirtualCamera> FindPlayerCameras()
        {
            if (_allVirtualCameras == null || _allVirtualCameras.Length == 0)
                return null;

            var cam = _allVirtualCameras.Where((x) => x.tag == "PlayerCamera").ToList();

            return cam;
        }

        private IEnumerator LerpYDamping_Coroutine(bool isPlayerFalling)
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
                endDampAmount = _initialYPanAmount;
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

        private IEnumerator PanCamera_Coroutine(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos, Action onPanComplete)
        {
            Vector2 endPos = Vector2.zero;
            Vector2 startingPos = Vector2.zero;

            // handle pan from trigger
            if (!panToStartingPos)
            {
                switch (panDirection)
                {
                    case PanDirection.Up:
                        endPos = Vector2.up;
                        break;
                    case PanDirection.Down:
                        endPos = Vector2.down;
                        break;
                    case PanDirection.Left:
                        endPos = Vector2.left;
                        break;
                    case PanDirection.Right:
                        endPos = Vector2.right;
                        break;
                }

                endPos *= panDistance;

                startingPos = _initialTrackedObjectOffset;

                endPos += startingPos;
            }
            // handle pan back to starting position
            else
            {
                startingPos = _framingTransposer.m_TrackedObjectOffset;
                endPos = _initialTrackedObjectOffset;
            }

            float elapsedTime = 0f;
            while (elapsedTime < panTime)
            {
                elapsedTime += Time.deltaTime;

                Vector2 panLerp = Vector2.Lerp(startingPos, endPos, (elapsedTime / panTime));
                _framingTransposer.m_TrackedObjectOffset = panLerp;

                yield return null;
            }

            onPanComplete?.Invoke();
        }

        private void ResetCurrentCamera(bool hardReset = false, Action onCameraReset = null)
        {
            if (hardReset)
            {
                if (_panCameraCoroutine != null) StopCoroutine(_panCameraCoroutine);
                if (_lerpTPanCoroutine != null) StopCoroutine(_lerpTPanCoroutine);
                _framingTransposer.m_TrackedObjectOffset = _initialTrackedObjectOffset;
                _framingTransposer.m_YDamping = _initialYPanAmount;
            }
            else
            {
                PanCamera(0f, _cameraResetPanTime, PanDirection.Up, true, onCameraReset);
            }
        }
        #endregion

    }
}