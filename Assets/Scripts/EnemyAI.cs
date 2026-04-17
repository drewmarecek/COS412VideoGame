using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Stats")]
    public float speed = 2f;
    public float retreatSpeed = 2f;
    public float retreatTime = 1.0f; // How long to back up after hitting player

    [Header("Detection")]
    [Tooltip("How close the player must be for the enemy to attack")]
    public float detectionRange = 4f;

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

            // Move AWAY from player (horizontal only - allow falling but not flying)
            float dirX = Mathf.Sign(transform.position.x - player.position.x);
            float vy = Mathf.Min(0f, rb.linearVelocity.y);
            rb.linearVelocity = new Vector2(dirX * retreatSpeed, vy);

            if (retreatTimer <= 0)
            {
                isRetreating = false; // Time to attack again!
            }
        }
        // PRIORITY 3: Chasing (only when player is close enough, horizontal only)
        else
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= detectionRange)
            {
                // Move TOWARDS player (horizontal only - allow falling but not flying)
                float dirX = Mathf.Sign(player.position.x - transform.position.x);
                float vy = Mathf.Min(0f, rb.linearVelocity.y);
                rb.linearVelocity = new Vector2(dirX * speed, vy);
            }
            else
            {
                // Player too far - stay idle (allow falling)
                float vy = Mathf.Min(0f, rb.linearVelocity.y);
                rb.linearVelocity = new Vector2(0f, vy);
            }
        }

        // Handle Facing Direction
        if (player.position.x > transform.position.x && transform.localScale.x < 0) Flip();
        else if (player.position.x < transform.position.x && transform.localScale.x > 0) Flip();
    }

    // Body contact with the player: back off, but do NOT deal damage.
    // Damage is handled exclusively by attack hitboxes (EnemyAttack on child colliders).
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            StartRetreat();
    }

    // Called by EnemyHealth script when player hits US
    public void ApplyKnockback()
    {
        isKnockedBack = true;
        isRetreating = false; // Knockback cancels retreat
        knockbackTimer = knockbackDuration;

        // Push enemy away from player (horizontal only - no flying)
        float dirX = Mathf.Sign(transform.position.x - player.position.x);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dirX * knockbackForce, 0f), ForceMode2D.Impulse);
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