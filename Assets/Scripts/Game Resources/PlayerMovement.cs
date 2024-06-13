using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float speed = 8f;
    public float jumpForce = 10f;
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;
    private bool isGrounded = false;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

    }

    // Update is called once per frame
    void Update()
    {
        // Get input from keyboard
        movement.x = Input.GetAxis("Horizontal");
        

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
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            animator.SetBool("IsJumping", true);
            Debug.Log("Jumping");
        }

    }

    void FixedUpdate()
    {
        // Move the character
        rb.velocity = new Vector2(movement.x * speed, rb.velocity.y);
        Debug.Log("Horizontal Input: " + movement.x + ", Velocity: " + rb.velocity);
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
