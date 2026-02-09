using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Stats")]
    public float speed = 2f;
    public float retreatSpeed = 2f;
    public float retreatTime = 1.0f; // How long to back up after hitting player

    [Header("Knockback Stats (When Enemy Gets Hit)")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.2f;

    private Transform player;
    private Rigidbody2D rb;
    
    // State Flags
    private bool isKnockedBack = false;
    private bool isRetreating = false;
    
    // Timers
    private float knockbackTimer;
    private float retreatTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // PRIORITY 1: Knockback (Getting hit by player)
        // This overrides everything else because physics impact is strongest
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero; // Stop the slide
            }
            return; // Skip normal movement while flying back
        }

        // PRIORITY 2: Retreating (After hitting player)
        if (isRetreating)
        {
            retreatTimer -= Time.deltaTime;
            
            // Move AWAY from player
            Vector2 direction = (transform.position - player.position).normalized;
            rb.linearVelocity = direction * retreatSpeed;

            if (retreatTimer <= 0)
            {
                isRetreating = false; // Time to attack again!
            }
        }
        // PRIORITY 3: Chasing (Normal State)
        else
        {
            // Move TOWARDS player
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }

        // Handle Facing Direction
        if (player.position.x > transform.position.x && transform.localScale.x < 0) Flip();
        else if (player.position.x < transform.position.x && transform.localScale.x > 0) Flip();
    }

    // Triggers when Enemy touches Player
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(1); // Deal damage
                StartRetreat();       // Back off!
            }
        }
    }

    // Called by EnemyHealth script when player hits US
    public void ApplyKnockback()
    {
        isKnockedBack = true;
        isRetreating = false; // Knockback cancels retreat
        knockbackTimer = knockbackDuration;

        // Push enemy away from player using physics impulse
        Vector2 direction = (transform.position - player.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }

    void StartRetreat()
    {
        isRetreating = true;
        retreatTimer = retreatTime;
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}