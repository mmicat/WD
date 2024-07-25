using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using WitchDoctor.CoreResources.Managers.CameraManagement;

namespace WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers
{
    public class PlayerCameraManager : GameEntityManager<PlayerCameraManager, PlayerCameraManagerContext>
    {
        private Rigidbody2D _rb;
        private Transform _characterRenderTransform;
        private Transform _cameraFollowTransform;

        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _cameraFollowTween;

        [SerializeField] private float _flipRotationTime = 0.4f;
        [SerializeField] private float _fallSpeedChangeThreshold = -15f;

        #region Overrides
        public override void InitManager()
        {
            base.InitManager();

            _rb = InitializationContext.RB;
            _characterRenderTransform = InitializationContext.CharacterRenderTransform;
            _cameraFollowTransform = InitializationContext.CameraFollowTransform;
        }

        public override void DeInitManager()
        {
            _rb = null;
            _characterRenderTransform = null;
            _cameraFollowTransform = null;

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


        private void Update()
        {
            // if we are falling past a certain speed threshold
            if (_rb.velocity.y < _fallSpeedChangeThreshold && !CameraManager.IsLerpingYDamping && !CameraManager.LerpedFromPlayerFalling)
            {
                CameraManager.Instance.LerpTransposerYDamping(true);
            }

            // if we are standing stil or still moving up
            if (_rb.velocity.y >= 0f && !CameraManager.IsLerpingYDamping && CameraManager.LerpedFromPlayerFalling)
            {
                CameraManager.Instance.LerpTransposerYDamping(false);
            }
        }
    }

    public class PlayerCameraManagerContext : GameEntityManagerContext<PlayerCameraManager, PlayerCameraManagerContext>
    {
        public Rigidbody2D RB { get; private set; }
        public Transform CharacterRenderTransform { get; private set; }
        public Transform CameraFollowTransform { get; private set; }

        public PlayerCameraManagerContext(Rigidbody2D rb, Transform characterRenderTransform, Transform cameraFollowTransform)
        {
            RB = rb;
            CharacterRenderTransform = characterRenderTransform;
            CameraFollowTransform = cameraFollowTransform;
        }
    }
}