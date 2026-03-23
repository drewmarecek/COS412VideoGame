using UnityEngine;

public class SkeletonAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float attackRange = 1.5f;

    [Header("Attack")]
    public float attackCooldown = 1.8f;

    [Tooltip("Drag the sword hitbox child object's collider here")]
    public Collider2D swordHitbox;

    [Header("Knockback (when skeleton gets hit)")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.2f;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;

    private bool isAttacking;
    private bool isDead;
    private bool isKnockedBack;
    private float nextAttackTime;
    private float knockbackTimer;
    private float attackResetTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (swordHitbox != null) swordHitbox.enabled = false;
    }

    void Update()
    {
        if (isDead || player == null) return;

        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        FacePlayer();

        if (isAttacking)
        {
            attackResetTimer -= Time.deltaTime;
            if (attackResetTimer <= 0)
                ResetAttack();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (Time.time >= nextAttackTime)
            {
                StartAttack();
            }
        }
        else
        {
            Move();
        }
    }

    void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (player.position.x > transform.position.x ? 1 : -1);
        transform.localScale = scale;
    }

    void Move()
    {
        if (anim != null) anim.SetBool("isRunning", true);
        Vector2 target = new Vector2(player.position.x, rb.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        isAttacking = true;
        attackResetTimer = 1.0f;
        if (anim != null) anim.SetBool("isRunning", false);

        int choice = Random.Range(1, 3);
        if (anim != null) anim.SetTrigger("Attack" + choice);

        nextAttackTime = Time.time + attackCooldown;
    }

    // Called by Animation Event when the sword swing starts
    public void OpenHitbox()
    {
        if (swordHitbox != null) swordHitbox.enabled = true;
    }

    // Called by Animation Event when the sword swing ends
    public void CloseHitbox()
    {
        if (swordHitbox != null) swordHitbox.enabled = false;
    }

    // Called by Animation Event at the end of each attack animation
    public void ResetAttack()
    {
        isAttacking = false;
        CloseHitbox();
    }

    public void ApplyKnockback()
    {
        if (player == null) return;
        isKnockedBack = true;
        isAttacking = false;
        CloseHitbox();
        knockbackTimer = knockbackDuration;

        float dirX = Mathf.Sign(transform.position.x - player.position.x);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dirX * knockbackForce, 0f), ForceMode2D.Impulse);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        CloseHitbox();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (anim != null) anim.SetTrigger("Death");
    }

    // Called by Animation Event on the last frame of the Death animation
    public void DeathAnimationFinished()
    {
        Destroy(gameObject);
    }
}
