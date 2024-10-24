using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Managers.InputManagement;
using WitchDoctor.Utils;

namespace WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers
{
    public class PlayerMovementManager : GameEntityManager<PlayerMovementManager, PlayerMovementManagerContext>
    {
        #region Movement and Physics
        private Transform _characterRenderTransform;
        private Transform _cameraFollowTransform;
        private Animator _animator;
        private TrailRenderer _dashTrail;

        private Transform _groundTransform;
        private Transform _ledgeTransform;
        private Transform _roofTransform;

        private Rigidbody2D _rb;
        private PlayerStates _playerStates;
        #endregion

        #region Entity Managers
        private PlayerCameraManager _playerCameraManager;
        #endregion

        private float _defaultGravity;
        private int _stepsXRecoiled = 0;
        private int _stepsYRecoiled = 0;
        private int _stepsJumped = 0;
        private bool _animateRecoil = false;
        private bool _blockInput = false;

        private Vector2 _movement;
        private Vector2 _damageDir;
        private Vector2 _dashDir;

        private Coroutine _dashCancelCoroutine;
        private Coroutine _dashLedgeCancelCoroutine;

        #region Serialized Fields
        [SerializeField] private PlayerStats _baseStats;

        [Space(5)]
        [Header("Debug Options")]
        [SerializeField] private bool _groundRaycastDims;
        [SerializeField] private bool _ledgeRaycastDims, _roofRaycastDims;
        #endregion

        #region Platform Checks
        public bool IsGrounded => Physics2D.BoxCast(_groundTransform.position, new Vector2(_baseStats.GroundCheckX, _baseStats.GroundCheckY),
            0f, Vector2.down, _baseStats.GroundCheckDist, _baseStats.GroundLayer);

        public bool IsNextToLedge => !Physics2D.BoxCast(_ledgeTransform.position, new Vector2(_baseStats.LedgeCheckX, _baseStats.LedgeCheckY),
            0f, Vector2.down, _baseStats.LedgeCheckDist, _baseStats.GroundLayer);
        public bool IsRoofed => Physics2D.BoxCast(_roofTransform.position, new Vector2(_baseStats.RoofCheckX, _baseStats.RoofCheckY),
            0f, Vector2.up, _baseStats.RoofCheckDist, _baseStats.GroundLayer);
        #endregion

        private Coroutine _inputWaitingCoroutine;

        // private bool _cameraFollowFacingRight = true;

        private bool CharacterRenderFacingRight => _characterRenderTransform.rotation.eulerAngles.y == 0f;

        #region Overrides
        public override void InitManager()
        {
            base.InitManager();
            
            _characterRenderTransform = InitializationContext.CharacterRenderTransform;
            _cameraFollowTransform = InitializationContext.CameraFollowTransform;
            _animator = InitializationContext.Animator;
            _dashTrail = InitializationContext.DashTrail;
            _baseStats = InitializationContext.BaseStats;
            _groundTransform = InitializationContext.GroundTransform;
            _ledgeTransform = InitializationContext.LedgeTransform;
            _roofTransform = InitializationContext.RoofTransform;
            _rb = InitializationContext.Rb;
            _playerStates = InitializationContext.PlayerStates;
            _playerCameraManager = InitializationContext.PlayerCameraManager;

            _defaultGravity = _rb.gravityScale;
            _inputWaitingCoroutine = StartCoroutine(AddListeners()); // We're waiting for the input system to initialize to avoid race conditions
        }

        public override void DeInitManager()
        {
            if (_inputWaitingCoroutine != null)
            {
                StopCoroutine(_inputWaitingCoroutine);
                _inputWaitingCoroutine = null;
            }

            ResetManager();

            RemoveListeners();

            base.DeInitManager();
        }

        public override void OnEntityDeath()
        {
            ResetManager();
            _blockInput = true;
        }

        protected override void DisplayDebugElements()
        {
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
        }
        #endregion

        #region Private Methods
        private IEnumerator AddListeners()
        {
            yield return new WaitUntil(() => InputManager.IsInstantiated);

            InputManager.Player.Movement.performed += MovementPerformed;
            InputManager.Player.Jump.performed += Jump_performed;
            InputManager.Player.Dash.performed += Dash_performed;

            _inputWaitingCoroutine = null;
        }

        private void RemoveListeners()
        {
            if (!InputManager.IsInstantiated) return;
            InputManager.Player.Movement.performed -= MovementPerformed;
            InputManager.Player.Jump.performed -= Jump_performed;
            InputManager.Player.Dash.performed -= Dash_performed;
        }

        private void ResetManager()
        {
            InterruptMovementActions(true, true);
            _rb.gravityScale = _defaultGravity;
            _stepsXRecoiled = 0;
            _stepsYRecoiled = 0;
            _stepsJumped = 0;
            _animateRecoil = false;
            _blockInput = false;
            _movement = Vector2.zero;
            _damageDir = Vector2.zero;
            _dashDir = Vector2.zero;

            if (_dashCancelCoroutine != null)
            {
                StopCoroutine(_dashCancelCoroutine);
                _dashCancelCoroutine = null;
            }
            if (_dashCancelCoroutine != null)
            {
                StopCoroutine(_dashLedgeCancelCoroutine);
                _dashLedgeCancelCoroutine = null;
            }
        }

        #region Movement Scripts
        /// <summary>
        /// Walk the character in the defined direction
        /// </summary>
        /// <param name="MoveDirection">
        /// Direction of movement
        /// </param>
        private void Walk(float MoveDirection)
        {
            if (!_playerStates.CanMove)
            {
                StopWalk();
                return;
            }

            _rb.velocity = new Vector2(MoveDirection * _baseStats.WalkSpeed, _rb.velocity.y);

            _playerStates.walking = Mathf.Abs(_rb.velocity.x) > 0f;

            _animator.SetBool("Walking", _playerStates.walking);
        }

        private void StopWalk()
        {
            _rb.velocity = Vector2.zero;
            _playerStates.walking = false;
            _animator.SetBool("Walking", false);
        }

        /// <summary>
        /// Flip the characters direction based on 
        /// the x axis motion
        /// </summary>
        private void Flip()
        {
            var orientation = CharacterRenderFacingRight;
            if (_movement.x > 0 && !orientation)
            {
                var rotator = new Vector3(transform.rotation.x, 0, transform.rotation.y);
                _characterRenderTransform.rotation = Quaternion.Euler(rotator);
                _playerCameraManager.FlipCameraFollow();
            }
            else if (_movement.x < 0 && orientation)
            {
                var rotator = new Vector3(transform.rotation.x, 180, transform.rotation.y);
                _characterRenderTransform.rotation = Quaternion.Euler(rotator);
                _playerCameraManager.FlipCameraFollow();
            }
        }

        /// <summary>
        /// Carry out jumping movement as long as
        /// the jump button is not released
        /// </summary>
        private void Jump()
        {
            if (_playerStates.jumping)
            {
                if (_stepsJumped < _baseStats.JumpSteps && !IsRoofed)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x, _baseStats.JumpSpeed);
                    _stepsJumped++;
                }
                else
                {
                    StopJumpSlow();
                }
            }
        }

        /// <summary>
        /// Stops the player jump immediately, 
        /// causing them to start falling as 
        /// soon as the button is released
        /// </summary>
        private void StopJumpQuick()
        {
            _stepsJumped = 0;
            _playerStates.jumping = false;
            _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        }

        /// <summary>
        /// stops the jump but lets the player 
        /// hang in the air for awhile.
        /// </summary>
        private void StopJumpSlow()
        {
            _stepsJumped = 0;
            _playerStates.jumping = false;
        }

        private void Dash()
        {
            if (_playerStates.dashing)
            {
                _playerStates.dashRefreshed = false;
                _playerStates.dashConditionsMet = false;

                _dashTrail.emitting = true;
                _dashDir = CharacterRenderFacingRight ? Vector2.right : Vector2.left;

                if (!IsGrounded)
                {
                    _rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                }

                _rb.velocity = _dashDir * _baseStats.DashingVelocity;

                _dashCancelCoroutine = StartCoroutine(StopDash_Coroutine());

                // If the dash started on the ground, make sure the dash doesn't overshoot past a ledge
                if (IsGrounded)
                    _dashLedgeCancelCoroutine = StartCoroutine(StopDashLedge_Coroutine());
            }
        }

        private IEnumerator StopDash_Coroutine()
        {
            yield return new WaitForSeconds(_baseStats.DashingTime);
            _playerStates.dashing = false;
            _dashTrail.emitting = false;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            yield return new WaitForSeconds(_baseStats.DashingRefreshTime);
            _playerStates.dashRefreshed = true;

            _dashCancelCoroutine = null;
        }

        private IEnumerator StopDashLedge_Coroutine()
        {
            yield return new WaitUntil(() => IsNextToLedge);
            StopDashQuick();
        }

        private void StopDashQuick()
        {
            _playerStates.dashing = false;
            _dashTrail.emitting = false;
            _playerStates.dashRefreshed = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            _rb.velocity = new Vector2(0, _rb.velocity.y);
        }

        private void SetRecoil()
        {
            if (!IsGrounded) _playerStates.recoilingY = true;
            _playerStates.recoilingX = true;
        }

        /// <summary>
        /// Adds a recoil force to the player
        /// </summary>
        /// <param name="runAnimation">Determines whether to run associated animation</param>
        /// <param name="cancelActions">determines whether to cancel other actions</param>
        private void Recoil(bool runAnimation = true, bool cancelActions = true)
        {
            if (_playerStates.recoilingX || _playerStates.recoilingY)
            {
                if (cancelActions)
                    InterruptMovementActions(true, true);

                _animateRecoil = runAnimation;

                var finalVelocity = Vector2.zero;

                //since this is run after Walk, it takes priority, and effects momentum properly.
                if (_playerStates.recoilingX)
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
                if (_playerStates.recoilingY)
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
            if (!_playerStates.IsRecoiling)
            {
                if (_animateRecoil)
                {
                    _damageDir = Vector2.zero;
                    _animateRecoil = false;
                }

                return;
            }

            if (_playerStates.recoilingX == true && _stepsXRecoiled < _baseStats.recoilXSteps)
            {
                _stepsXRecoiled++;
            }
            else
            {
                StopRecoilX();
            }
            if (_playerStates.recoilingY == true && _stepsYRecoiled < _baseStats.recoilYSteps)
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
            _playerStates.recoilingX = false;
        }

        private void StopRecoilY()
        {
            _stepsYRecoiled = 0;
            _playerStates.recoilingY = false;
        }

        /// <summary>
        /// This limits how fast the player can fall
        /// Since platformers generally have increased 
        /// gravity, you don't want them to fall so 
        /// fast they clip trough all the floors.
        /// </summary>
        private void LimitFallSpeed()
        {
            if (_rb.velocity.y < -Mathf.Abs(_baseStats.MaxFallSpeed))
            {
                _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Clamp(_rb.velocity.y, -Mathf.Abs(_baseStats.MaxFallSpeed), Mathf.Infinity));
            }
        }

        /// <summary>
        /// Update jump states if the jump button
        /// is released
        /// </summary>
        private void UpdateMovementStates()
        {
            _animator.SetBool("Grounded", IsGrounded);
            _animator.SetBool("Dash", _playerStates.dashing);
            _animator.SetBool("Recoiling", _playerStates.IsRecoiling && _animateRecoil);
            _animator.SetFloat("YVelocity", _rb.velocity.y);

            if (InputManager.Player.Jump.WasReleasedThisFrame())
            {
                if (_stepsJumped < _baseStats.JumpSteps
                    && _stepsJumped > _baseStats.JumpThreshold
                    && _playerStates.jumping)
                {
                    StopJumpQuick();
                }
                else if (_stepsJumped < _baseStats.JumpThreshold
                    && _playerStates.jumping)
                {
                    StopJumpSlow();
                }
            }

            if (!_playerStates.dashConditionsMet && _playerStates.dashRefreshed && IsGrounded)
            {
                _playerStates.dashConditionsMet = true;
            }
        }
        #endregion
        #endregion

        #region Public Methods
        public void InterruptMovementActions(bool interruptJump = false, bool interruptMove = false)
        {
            if (interruptJump)
                StopJumpQuick();
            if (interruptMove)
                StopWalk();

            StopDashQuick();
        }

        public void ProcessEnemyCollision(Transform collision)
        {
            _damageDir = Vector3.Normalize(collision.position - transform.position);
            SetRecoil();
        }
        #endregion

        #region Event Listeners
        private void MovementPerformed(InputAction.CallbackContext obj)
        {
            if (_blockInput) return;

            if (obj.performed)
            {
                _movement.x = obj.ReadValue<Vector2>().x;
                _movement.y = obj.ReadValue<Vector2>().y;
            }
        }

        private void Jump_performed(InputAction.CallbackContext obj)
        {
            if (_blockInput) return;

            if (obj.performed && IsGrounded && _playerStates.CanMove)
            {
                if (_playerStates.dashing)
                {
                    StopDashQuick();
                }

                _playerStates.jumping = true;
            }
        }

        private void Dash_performed(InputAction.CallbackContext obj)
        {
            if (_blockInput) return;

            if (obj.performed && _playerStates.dashConditionsMet && _playerStates.CanMove)
            {
                if (_playerStates.jumping)
                {
                    StopJumpSlow();
                }

                _playerStates.dashing = true;
            }
        }
        #endregion

        #region Unity Methods
        void Update()
        {
            Walk(_movement.x);
            Recoil();
            UpdateMovementStates();
        }

        void FixedUpdate()
        {
            Flip();
            Jump();
            Dash();
            ProcessRecoil();
            LimitFallSpeed();
        }

#if UNITY_EDITOR
        
#endif
        #endregion
    }

    public class PlayerMovementManagerContext : GameEntityManagerContext<PlayerMovementManager, PlayerMovementManagerContext>
    {
        public Transform CharacterRenderTransform;
        public Transform CameraFollowTransform;
        public Animator Animator;
        public TrailRenderer DashTrail;
        public PlayerStats BaseStats;

        public Transform GroundTransform;
        public Transform LedgeTransform;
        public Transform RoofTransform;

        public Rigidbody2D Rb;
        public PlayerStates PlayerStates;

        public PlayerCameraManager PlayerCameraManager;

        public PlayerMovementManagerContext(Transform characterRenderTransform, Transform cameraFollowTransform, Animator animator, 
            TrailRenderer dashTrail, PlayerStats baseStats, Transform groundTransform, Transform ledgeTransform, Transform roofTransform,
            Rigidbody2D rb, PlayerStates playerStates, PlayerCameraManager playerCameraManager)
        {
            CharacterRenderTransform = characterRenderTransform;
            CameraFollowTransform = cameraFollowTransform;
            Animator = animator;
            DashTrail = dashTrail;
            BaseStats = baseStats;
            GroundTransform = groundTransform;
            LedgeTransform = ledgeTransform;
            RoofTransform = roofTransform;
            Rb = rb;
            PlayerStates = playerStates;
            PlayerCameraManager = playerCameraManager;
        }
    }
}