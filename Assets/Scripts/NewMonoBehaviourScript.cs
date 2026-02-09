using UnityEngine;

public class GlitchPlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float moveSpeed = 10f;
    public float acceleration = 50f;
    public float decceleration = 50f;
    public float velPower = 0.9f; // Higher makes it more "snappy"

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    [Range(0, 1)] public float jumpCutMultiplier = 0.5f; // How much jump is cut when you let go early
    public float fallMultiplier = 2.5f; // Gravity boost when falling for "weight"
    public int extraJumpsValue = 1;

    [Header("Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private int extraJumps;

    private bool isFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) extraJumps = extraJumpsValue;

        // Jump Input
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W)) && (isGrounded || extraJumps > 0))
        {
            Jump();
            if (!isGrounded) extraJumps--;
        }

        // --- VARIABLE JUMP HEIGHT ---
        // If we release the jump button while moving upward, cut the velocity
        if ((Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.W)) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // Flip the player if they are moving left or right
        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }

        if (GetComponent<Animator>() != null)
        {
            GetComponent<Animator>().SetFloat("Speed", Mathf.Abs(moveInput));
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravityScale();
    }

    void ApplyMovement()
    {
        // Calculate the direction we want to move and the desired velocity
        float targetSpeed = moveInput * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;

        // Change acceleration rate depending on if we are accelerating or deccelerating
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;

        // Applies acceleration to speed difference and raises to a set power so acceleration increases with higher values
        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        rb.AddForce(movement * Vector2.right);
    }

    void ApplyGravityScale()
    {
        // Makes the player fall faster than they rise (feels more "meaty")
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    void Jump()
    {
        // Reset vertical velocity for a consistent jump height (even if falling)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // 4. The Flip Function
    void Flip()
    {
        isFacingRight = !isFacingRight;
        
        // Multiply the player's x local scale by -1
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}