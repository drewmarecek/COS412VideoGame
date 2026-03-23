using UnityEngine;
using System.Collections;

public class SkeletonAI : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public float moveSpeed = 2.5f;
    public int damageValue = 1;
    public float deathDisappearDelay = 1.0f;

    [Header("Detection & Attack")]
    [Tooltip("How close the player must be before the skeleton notices them")]
    public float detectionRange = 5f;
    [Tooltip("How close the skeleton gets before swinging its sword")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.8f;

    [Header("Attack Hitbox")]
    [Tooltip("Drag the sword hitbox child object's collider here")]
    public Collider2D swordHitbox;

    [Header("Knockback")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.2f;

    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool isKnockedBack = false;
    private float nextAttackTime;
    private float knockbackTimer;
    private float attackResetTimer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
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

        if (isAttacking)
        {
            attackResetTimer -= Time.deltaTime;
            if (attackResetTimer <= 0)
                ResetAttack();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > detectionRange)
        {
            anim.SetFloat("speed", 0f);
            return;
        }

        FacePlayer();

        if (distance <= attackRange)
        {
            anim.SetFloat("speed", 0f);
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
        anim.SetFloat("speed", 1f);
        Vector2 target = new Vector2(player.position.x, rb.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        isAttacking = true;
        attackResetTimer = 1.0f;
        anim.SetFloat("speed", 0f);
        anim.SetTrigger("attack" + Random.Range(1, 3));
        nextAttackTime = Time.time + attackCooldown;
    }

    // CALLED BY ANIMATION EVENT — sword swing starts
    public void OpenHitbox()
    {
        if (swordHitbox != null) swordHitbox.enabled = true;
    }

    // CALLED BY ANIMATION EVENT — sword swing ends
    public void CloseHitbox()
    {
        if (swordHitbox != null) swordHitbox.enabled = false;
    }

    // CALLED BY ANIMATION EVENT — attack animation finished
    public void ResetAttack()
    {
        isAttacking = false;
        CloseHitbox();
    }

    // Contact damage — player takes damage when walking into the skeleton
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                TriggerPlayerHitEffects(playerHealth, damageValue);
                playerHealth.TakeDamage(damageValue);
            }
        }
    }

    void TriggerPlayerHitEffects(PlayerHealth playerHealth, int damage)
    {
        if (playerHealth.IsInvincible) return;
        bool willDie = playerHealth.health <= damage;
        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null)
        {
            if (willDie) camShake.ShakeKill();
            else camShake.ShakeHit();
        }
        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null)
        {
            if (willDie) hitStop.Stop(0.25f, 15);
            else hitStop.Stop(0.07f, 45);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        health -= damage;

        if (health <= 0)
        {
            Die();
            return;
        }

        ApplyKnockback();
        anim.SetTrigger("takeHit");
    }

    void ApplyKnockback()
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

    void Die()
    {
        isDead = true;
        isAttacking = false;
        isKnockedBack = false;
        CloseHitbox();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        anim.SetFloat("speed", 0f);
        anim.SetTrigger("death");

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null) hitStop.Stop(0.2f, 12);

        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        Color[] originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i].color;

        float elapsed = 0f;
        while (elapsed < deathDisappearDelay)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / deathDisappearDelay);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    Color c = originalColors[i];
                    c.a = originalColors[i].a * alpha;
                    sprites[i].color = c;
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
