using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Cinemachine;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public class PlayerCameraManager : GameEntityManager<PlayerCameraManager, PlayerCameraManagerContext>
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Transform _characterRenderTransform;
        [SerializeField] private Transform _cameraFollowTransform;
        [SerializeField] private float _flipRotationTime = 0.4f;

        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _cameraFollowTween;

        [SerializeField] private CinemachineVirtualCamera[] _allVirtualCameras;

        [Header("Controls for lerping the Y Damping during player jump/fall")]
        [SerializeField] private float _fallPanAmount = 0.25f;
        [SerializeField] private float _fallYPanTime = 0.35f;
        public float _fallSpeedChangeThreshold = -15f;

        public bool IsLerpingYDamping { get; private set; }
        public bool LerpedFromPlayerFalling { get; private set; }

        private Coroutine _lerpTPanCoroutine;

        private CinemachineVirtualCamera _currentCamera;
        private CinemachineFramingTransposer _framingTransposer;

        private float _normYPanAmount;

        #region Overrides
        public override void InitManager()
        {
            base.InitManager();

            _rb = InitializationContext.RB;
            _characterRenderTransform = InitializationContext.CharacterRenderTransform;
            _cameraFollowTransform = InitializationContext.CameraFollowTransform;
            _flipRotationTime = InitializationContext.FlipRotationTime;

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

        public override void DeInitManager()
        {
            _characterRenderTransform = null;

            base.DeInitManager();
        }
        #endregion

        /// <summary>
        /// Flip the Camera Follow Game Object with a slight delay
        /// </summary>
        public void FlipCameraFollow()
        {
            if (_cameraFollowTween != null && _cameraFollowTween.IsPlaying())
            {
                _cameraFollowTween.Kill();
            }
            _cameraFollowTween = _cameraFollowTransform.DORotate(_characterRenderTransform.rotation.eulerAngles, _flipRotationTime);
        }

        public void LerpYDamping(bool isPlayerFalling)
        {
            _lerpTPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
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

        private void Update()
        {
            // if we are falling past a certain speed threshold
            if (_rb.velocity.y < _fallSpeedChangeThreshold && !IsLerpingYDamping && !LerpedFromPlayerFalling)
            {
                LerpYDamping(true);
            }

            // if we are standing stil or still moving up
            if (_rb.velocity.y >= 0f && !IsLerpingYDamping && LerpedFromPlayerFalling)
            {
                LerpedFromPlayerFalling = false;

                LerpYDamping(false);
            }
        }
    }

    public class PlayerCameraManagerContext : GameEntityManagerContext<PlayerCameraManager, PlayerCameraManagerContext>
    {
        public Rigidbody2D RB { get; private set; }
        public Transform CharacterRenderTransform { get; private set; }
        public Transform CameraFollowTransform { get; private set; }
        public float FlipRotationTime { get; private set; }

        public PlayerCameraManagerContext(Rigidbody2D rb, Transform characterRenderTransform, Transform cameraFollowTransform, float flipRotationTime)
        {
            RB = rb;
            CharacterRenderTransform = characterRenderTransform;
            CameraFollowTransform = cameraFollowTransform;
            FlipRotationTime = flipRotationTime;
        }
    }
}