using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Managers.InputManagement;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public class PlayerManager : PlayerEntity
    {
        [SerializeField]
        private Animator _animator;
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
        private bool facingRight = true;

        private bool IsGrounded => Physics2D.Raycast(_groundTransform.position, Vector2.down, _baseStats.GroundCheckY, _baseStats.GroundLayer) 
            || Physics2D.Raycast(_groundTransform.position + new Vector3(-_baseStats.GroundCheckX, 0), Vector2.down, _baseStats.GroundCheckY, _baseStats.GroundLayer) 
            || Physics2D.Raycast(_groundTransform.position + new Vector3(_baseStats.GroundCheckX, 0), Vector2.down, _baseStats.GroundCheckY, _baseStats.GroundLayer);

        private bool IsRoofed => Physics2D.Raycast(_roofTransform.position, Vector2.up, _baseStats.RoofCheckY, _baseStats.GroundLayer)
            || Physics2D.Raycast(_roofTransform.position + new Vector3(-_baseStats.RoofCheckX, 0), Vector2.up, _baseStats.RoofCheckY, _baseStats.GroundLayer)
            || Physics2D.Raycast(_roofTransform.position + new Vector3(_baseStats.RoofCheckX, 0), Vector2.up, _baseStats.RoofCheckY, _baseStats.GroundLayer);

        private Coroutine _inputWaitingCoroutine;

        #region Overrides
        protected override void InitCharacter()
        {
            base.InitCharacter();

            if (_animator == null) throw new System.NullReferenceException("Missing Animator Component");

            if (_rb == null) _rb = GetComponent<Rigidbody2D>();

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

            _inputWaitingCoroutine = null;
        }


        private void RemoveListeners()
        {
            if (!InputManager.IsInstantiated) return;
            InputManager.InputActions.Player.Movement.performed -= MovementPerformed;
            InputManager.InputActions.Player.Jump.performed -= Jump_performed;
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
                _playerStates.jumping = true;
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
            if (movement.x > 0)
            {
                transform.localScale = new Vector2(1, transform.localScale.y);
            }
            else if (movement.x < 0)
            {
                transform.localScale = new Vector2(-1, transform.localScale.y);
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
        private void CheckJumpStates()
        {
            _animator.SetBool("Grounded", IsGrounded);
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
            CheckJumpStates();
            Flip();
            Walk(movement.x);
           
        }

        void FixedUpdate()
        {
            Jump();
            LimitFallSpeed();
        }
        #endregion
    }
}