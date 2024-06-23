using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WitchDoctor.Managers.InputManagement;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public class PlayerManager : PlayerEntity
    {
        private Animator animator;
        private Rigidbody2D rb;
        private Vector2 movement;
        private bool facingRight = true;
        private bool isGrounded = false;

        private Coroutine _inputWaitingCoroutine;

        #region Overrides
        protected override void InitCharacter()
        {
            base.InitCharacter();

            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();

            _inputWaitingCoroutine = StartCoroutine(AddListeners()); // We're waiting for the input system to initialize to avoid race conditions
        }

        protected override void DeInitCharacter()
        {
            if (_inputWaitingCoroutine != null)
            {
                StopCoroutine(_inputWaitingCoroutine);
                _inputWaitingCoroutine = null;
            }

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
            }
        }
        
        private void Jump_performed(InputAction.CallbackContext obj)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        // Update is called once per frame
        void Update()
        {
            // Get input from keyboard
            // movement.x = Input.GetAxis("Horizontal");

            // Set animation parameters
            animator.SetFloat("Speed", Mathf.Abs(movement.x));


            // Flip the character based on movement direction
            if (movement.x > 0 && !facingRight)
            {
                Flip();
            }
            else if (movement.x < 0 && facingRight)
            {
                Flip();
            }

            // Check if jump is pressed
            //if (Input.GetButtonDown("Jump") && isGrounded)
            //{
            //    rb.AddForce(new Vector2(0f, _jumpForce), ForceMode2D.Impulse);
            //    animator.SetBool("IsJumping", true);
            //    Debug.Log("Jumping");
            //}

        }

        void FixedUpdate()
        {
            // Move the character
            rb.velocity = new Vector2(movement.x * _speed, rb.velocity.y);
            // Debug.Log("Horizontal Input: " + movement.x + ", Velocity: " + rb.velocity);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                animator.SetBool("IsJumping", false);
                Debug.Log("Player landed on the ground.");
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = false;
                Debug.Log("Player left the ground.");
            }
        }

        void Flip()
        {
            // Switch the way the player is labeled as facing
            facingRight = !facingRight;

            // Multiply the player's x local scale by -1
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }
}