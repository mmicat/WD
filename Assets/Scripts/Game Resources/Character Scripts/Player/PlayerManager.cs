using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Managers.InputManagement;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public class PlayerManager : PlayerEntity
    {
        #region Movement and Physics
        [SerializeField]
        private Transform _characterRenderTransform;
        [SerializeField]
        private Transform _cameraFollowTransform;
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private TrailRenderer _dashTrail;
        [SerializeField]
        private Transform _groundTransform;
        [SerializeField]
        private Transform _roofTransform;
        [SerializeField]
        private PlayerStats _baseStats;
        
        private Rigidbody2D _rb;
        private PlayerStates _playerStates;

        private float xAxis;
        private float yAxis;
        private float gravity;
        private int stepsJumped;

        private Vector2 movement;

        // Dashing
        private Vector2 _dashDir;
        
        private Coroutine _dashingCoroutine;

        private bool IsGrounded => Physics2D.Raycast(_groundTransform.position, Vector2.down, _baseStats.GroundCheckY, _baseStats.GroundLayer) 
            || Physics2D.Raycast(_groundTransform.position + new Vector3(-_baseStats.GroundCheckX, 0), Vector2.down, _baseStats.GroundCheckY, _baseStats.GroundLayer) 
            || Physics2D.Raycast(_groundTransform.position + new Vector3(_baseStats.GroundCheckX, 0), Vector2.down, _baseStats.GroundCheckY, _baseStats.GroundLayer);

        private bool IsRoofed => Physics2D.Raycast(_roofTransform.position, Vector2.up, _baseStats.RoofCheckY, _baseStats.GroundLayer)
            || Physics2D.Raycast(_roofTransform.position + new Vector3(-_baseStats.RoofCheckX, 0), Vector2.up, _baseStats.RoofCheckY, _baseStats.GroundLayer)
            || Physics2D.Raycast(_roofTransform.position + new Vector3(_baseStats.RoofCheckX, 0), Vector2.up, _baseStats.RoofCheckY, _baseStats.GroundLayer);
        #endregion

        private Coroutine _inputWaitingCoroutine;

        [SerializeField] private float _flipRotationTime = 0.4f;
        private bool _cameraFollowFacingRight = true;
        private bool CharacterRenderFacingRight => _characterRenderTransform.rotation.eulerAngles.y == 0f;

        #region Entity Managers
        [SerializeField] private PlayerCameraManager _playerCameraManager;
        #endregion

        #region Overrides
        protected override void SetManagerContexts()
        {
            _managerList = new List<IGameEntityManager>()
            {
                _playerCameraManager.SetContext(
                    new PlayerCameraManagerContext(
                        _rb,
                        _characterRenderTransform, 
                        _cameraFollowTransform, 
                        _flipRotationTime))
            };
        }

        protected override void InitCharacter()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_animator == null) throw new System.NullReferenceException("Missing Animator Component");

            base.InitCharacter();

            gravity = _rb.gravityScale;
            _playerStates.Reset();

            _inputWaitingCoroutine = StartCoroutine(AddListeners()); // We're waiting for the input system to initialize to avoid race conditions
        }

        protected override void DeInitCharacter()
        {
            if (_inputWaitingCoroutine != null)
            {
                StopCoroutine(_inputWaitingCoroutine);
                _inputWaitingCoroutine = null;
            }

            _playerStates.Reset();

            RemoveListeners();

            base.DeInitCharacter();
        }
        #endregion

        #region Private Methods
        private IEnumerator AddListeners()
        {
            yield return new WaitUntil(() => InputManager.IsInstantiated);

            InputManager.InputActions.Player.Movement.performed += MovementPerformed;
            InputManager.InputActions.Player.Jump.performed += Jump_performed;
            InputManager.InputActions.Player.Dash.performed += Dash_performed;

            _inputWaitingCoroutine = null;
        }

        private void RemoveListeners()
        {
            if (!InputManager.IsInstantiated) return;
            InputManager.InputActions.Player.Movement.performed -= MovementPerformed;
            InputManager.InputActions.Player.Jump.performed -= Jump_performed;
            InputManager.InputActions.Player.Dash.performed -= Dash_performed;
        }
        #endregion

        #region Event Listeners
        private void MovementPerformed(InputAction.CallbackContext obj)
        {
            if (obj.performed)
            {
                movement.x = obj.ReadValue<Vector2>().x;
                movement.y = obj.ReadValue<Vector2>().y;
            }
        }
        
        private void Jump_performed(InputAction.CallbackContext obj)
        {
            if (obj.performed && IsGrounded)
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
            if (obj.performed && _playerStates.dashConditionsMet)
            {
                if (_playerStates.jumping)
                {
                    StopJumpSlow();
                }

                _playerStates.dashing = true;
            }
        }
        #endregion

        #region Movement Scripts
        /// <summary>
        /// Walk the character in the defined direction
        /// </summary>
        /// <param name="MoveDirection">
        /// Direction of movement
        /// </param>
        private void Walk(float MoveDirection)
        {
            _rb.velocity = new Vector2(MoveDirection * _baseStats.WalkSpeed, _rb.velocity.y);

            _playerStates.walking = Mathf.Abs(_rb.velocity.x) > 0f;

            _animator.SetBool("Walking", _playerStates.walking);
        }

        /// <summary>
        /// Flip the characters direction based on 
        /// the x axis motion
        /// </summary>
        private void Flip()
        {
            var orientation = CharacterRenderFacingRight;
            if (movement.x > 0 && !orientation)
            {
                var rotator = new Vector3(transform.rotation.x, 0, transform.rotation.y);
                _characterRenderTransform.rotation = Quaternion.Euler(rotator);
                _playerCameraManager.FlipCameraFollow();
            }
            else if (movement.x < 0 && orientation)
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
                if (stepsJumped < _baseStats.JumpSteps && !IsRoofed)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x, _baseStats.JumpSpeed);
                    stepsJumped++;
                }
                else
                {
                    StopJumpSlow();
                }
            }
        }

        private void Dash()
        {
            if (_playerStates.dashing)
            {
                

                _playerStates.dashRefreshed = false;
                _playerStates.dashConditionsMet = false;

                _dashTrail.emitting = true;
                _dashDir = CharacterRenderFacingRight ? Vector2.right : Vector2.left;

                _rb.velocity = _dashDir * _baseStats._dashingVelocity;

                _dashingCoroutine = StartCoroutine(StopDash_Coroutine());
            }
        }

        private IEnumerator StopDash_Coroutine()
        {
            yield return new WaitForSeconds(_baseStats._dashingTime);
            _playerStates.dashing = false;
            _dashTrail.emitting = false;
            yield return new WaitForSeconds(_baseStats._dashingRefreshTime);
            _playerStates.dashRefreshed = true;

            _dashingCoroutine = null;
        }

        private void StopDashQuick()
        {
            _playerStates.dashing = false;
            _dashTrail.emitting = false;
            _playerStates.dashRefreshed = true;

            _rb.velocity = new Vector2(0, _rb.velocity.y);
        }

        /// <summary>
        /// This limits how fast the player can fall
        /// Since platformers generally have increased 
        /// gravity, you don't want them to fall so 
        /// fast they clip trough all the floors.
        /// </summary>
        private void LimitFallSpeed()
        {
            
            if (_rb.velocity.y < -Mathf.Abs(_baseStats.FallSpeed))
            {
                _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Clamp(_rb.velocity.y, -Mathf.Abs(_baseStats.FallSpeed), Mathf.Infinity));
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
            _animator.SetFloat("YVelocity", _rb.velocity.y);

            if (InputManager.InputActions.Player.Jump.WasReleasedThisFrame())
            {
                if (stepsJumped < _baseStats.JumpSteps
                    && stepsJumped > _baseStats.JumpThreshold
                    && _playerStates.jumping)
                {
                    StopJumpQuick();
                }
                else if (stepsJumped < _baseStats.JumpThreshold
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

        /// <summary>
        /// Stops the player jump immediately, 
        /// causing them to start falling as 
        /// soon as the button is released
        /// </summary>
        private void StopJumpQuick()
        {
            stepsJumped = 0;
            _playerStates.jumping = false;
            _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        }

        /// <summary>
        /// stops the jump but lets the player 
        /// hang in the air for awhile.
        /// </summary>
        private void StopJumpSlow()
        {
            stepsJumped = 0;
            _playerStates.jumping = false;
        }
        #endregion

        #region Unity Methods
        void Update()
        {
            UpdateMovementStates();
            Walk(movement.x);
        }

        void FixedUpdate()
        {
            Flip();
            Jump();
            Dash();
            LimitFallSpeed();
        }

        #endregion
    }
}