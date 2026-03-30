using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2.5f;
    public float attackRange = 2.2f;
    public int damageValue = 1;
    public float attackCooldown = 2.0f;
    public float deathDisappearDelay = 1.5f;
    [Tooltip("Fallback timing used if animation events are missing.")]
    public float attackDamageDelay = 0.25f;
    [Tooltip("Failsafe: end attack state even if ResetAttack animation event is missing.")]
    public float attackRecoverTime = 0.8f;

    [Header("References")]
    [Tooltip("Treasure chest that drops when boss dies.")]
    public GameObject treasureChestPrefab;
    [Header("Attack Hitbox")]
    [Tooltip("Optional: drag boss weapon hitbox collider here (recommended).")]
    public Collider2D swordHitbox;
    [Tooltip("If true, boss damage should come from sword hitbox collider/EnemyAttack, not overlap radius.")]
    public bool useHitboxDamage = true;
    public Transform attackPoint;
    public float attackRadius = 1.5f;
    public LayerMask playerLayer;

    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer[] sprites;

    private float health;
    private float nextAttackTime;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool didDealDamageThisAttack = false;
    public bool isActive = false;
    public bool IsDefeated { get; private set; }

    private Vector3 startPosition;
    private Vector3 startScale;
    private GameObject spawnedChest;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        if (swordHitbox != null) swordHitbox.enabled = false;

        startPosition = transform.position;
        startScale = transform.localScale;
        health = maxHealth;
    }

    void Update()
    {
        if (!isActive || isDead || player == null) return;

        FacePlayer();

        if (isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (Time.time >= nextAttackTime)
                StartAttack();
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
        anim.SetBool("isRunning", true);
        Vector2 target = new Vector2(player.position.x, rb.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        isAttacking = true;
        didDealDamageThisAttack = false;
        anim.SetBool("isRunning", false);
        anim.SetTrigger("Attack" + Random.Range(1, 3));
        nextAttackTime = Time.time + attackCooldown;
        if (!useHitboxDamage)
            StartCoroutine(AttackDamageFallback());
        StartCoroutine(AttackRecoverFallback());
    }

    // CALLED BY ANIMATION EVENT (legacy circle-based damage path)
    public void DealDamage()
    {
        if (useHitboxDamage) return;
        if (didDealDamageThisAttack) return;
        didDealDamageThisAttack = true;

        Vector2 hitCenter = attackPoint != null ? attackPoint.position : transform.position;

        Collider2D hitPlayer = null;
        if (playerLayer.value != 0)
            hitPlayer = Physics2D.OverlapCircle(hitCenter, attackRadius, playerLayer);
        else
            hitPlayer = Physics2D.OverlapCircle(hitCenter, attackRadius);

        if (hitPlayer != null && hitPlayer.CompareTag("Player"))
        {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                TriggerPlayerHitEffects(playerHealth, damageValue);
                playerHealth.TakeDamage(damageValue);
            }
        }
    }

    public void ResetAttack()
    {
        isAttacking = false;
        CloseHitbox();
    }

    // CALLED BY ANIMATION EVENT — opens sword hitbox window
    public void OpenHitbox()
    {
        if (!useHitboxDamage) return;
        if (swordHitbox != null) swordHitbox.enabled = true;
    }

    // CALLED BY ANIMATION EVENT — closes sword hitbox window
    public void CloseHitbox()
    {
        if (swordHitbox != null) swordHitbox.enabled = false;
    }

    private IEnumerator AttackDamageFallback()
    {
        yield return new WaitForSeconds(attackDamageDelay);

        // If animation event was not configured, still apply one melee hit.
        if (isAttacking && !didDealDamageThisAttack)
            DealDamage();
    }

    private IEnumerator AttackRecoverFallback()
    {
        yield return new WaitForSeconds(attackRecoverTime);

        // If animation did not call ResetAttack(), unlock AI so boss can keep fighting.
        if (isAttacking)
            ResetAttack();

        // Safety: ensure hitbox is not left active.
        CloseHitbox();
    }

    // Intentionally no generic contact-damage.
    // Boss damages via sword hitbox (preferred) or DealDamage() fallback path.

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

    public void TakeDamage(float damage)
    {
        if (isDead || !isActive) return;
        health -= damage;
        anim.SetTrigger("TakeHit");
        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        IsDefeated = true;
        anim.SetTrigger("Death");

        if (treasureChestPrefab != null)
            spawnedChest = Instantiate(treasureChestPrefab, transform.position, Quaternion.identity);

        rb.simulated = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
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

        gameObject.SetActive(false);
    }

    public void ResetBoss()
    {
        StopAllCoroutines();

        if (spawnedChest != null)
        {
            Destroy(spawnedChest);
            spawnedChest = null;
        }

        health = maxHealth;
        isDead = false;
        isAttacking = false;
        isActive = false;
        nextAttackTime = 0f;

        transform.position = startPosition;
        transform.localScale = startScale;

        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                Color c = sprites[i].color;
                c.a = 1f;
                sprites[i].color = c;
            }
        }

        anim.SetBool("isRunning", false);
        anim.Rebind();
        anim.Update(0f);

        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
