using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    private const float SwordSwingSfxDelay = 0.3f;

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
    [Tooltip("Delay before boss hit feedback (camera shake + hit stop).")]
    public float playerHitEffectsDelay = 0.1f;
    [Header("Damage Shake Scaling")]
    [Tooltip("Shake intensity when boss is near full health.")]
    public float lowHealthShakeMinIntensity = 0.08f;
    [Tooltip("Shake intensity when boss is near death.")]
    public float lowHealthShakeMaxIntensity = 0.26f;
    [Tooltip("Shake duration when boss is near full health.")]
    public float lowHealthShakeMinDuration = 0.1f;
    [Tooltip("Shake duration when boss is near death.")]
    public float lowHealthShakeMaxDuration = 0.22f;

    [Header("Player Hit Shake Scaling")]
    [Tooltip("Shake intensity for player hit when player is at full health.")]
    public float playerHitShakeMinIntensity = 0.08f;
    [Tooltip("Shake intensity for player hit when player is near death (non-fatal).")]
    public float playerHitShakeMaxIntensity = 0.28f;
    [Tooltip("Shake duration for player hit when player is at full health.")]
    public float playerHitShakeMinDuration = 0.12f;
    [Tooltip("Shake duration for player hit when player is near death (non-fatal).")]
    public float playerHitShakeMaxDuration = 0.3f;

    [Header("Player Hit Pause")]
    [Tooltip("Hit-stop duration when the player is hit by this boss (non-fatal).")]
    public float playerHitPauseDuration = 0.12f;

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
    [Tooltip("Distance the boss must reach before starting an attack.")]
    public float requiredAttackDistance = 1.2f;
    [Tooltip("How close boss tries to stand to the player while chasing.")]
    public float followStopDistance = 2.25f;
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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

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
        float distance = Vector2.Distance(transform.position, player.position);
        float attackEngageRange = Mathf.Max(0.1f, requiredAttackDistance);
        float chaseStopRange = Mathf.Max(0.1f, followStopDistance);
        bool cooldownReady = Time.time >= nextAttackTime;

        // If attack started but player moved out of practical melee range, cancel and chase again.
        if (isAttacking)
        {
            if (distance > attackEngageRange)
            {
                ResetAttack();
                Move();
            }
            return;
        }

        if (cooldownReady && distance <= attackEngageRange)
        {
            StartAttack();
        }
        else if (cooldownReady && distance > attackEngageRange)
        {
            // Cooldown finished but still out of range: close distance until attack is possible.
            Move();
        }
        else if (distance > chaseStopRange)
        {
            Move();
        }
        else if (anim != null)
        {
            anim.SetBool("isRunning", false);
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
        float y = rb != null ? rb.position.y : transform.position.y;
        Vector2 target = new Vector2(player.position.x, y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        isAttacking = true;
        didDealDamageThisAttack = false;
        StartCoroutine(PlaySwordSwingSfxDelayed());
        if (anim != null)
        {
            anim.SetBool("isRunning", false);
            anim.SetTrigger("Attack" + Random.Range(1, 3));
        }
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
            if (playerHealth == null)
                playerHealth = hitPlayer.GetComponentInParent<PlayerHealth>();
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

    private IEnumerator PlaySwordSwingSfxDelayed()
    {
        yield return new WaitForSeconds(SwordSwingSfxDelay);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("Boss1Attack");
    }

    // Intentionally no generic contact-damage.
    // Boss damages via sword hitbox (preferred) or DealDamage() fallback path.

    void TriggerPlayerHitEffects(PlayerHealth playerHealth, int damage)
    {
        if (playerHealth.IsInvincible) return;
        bool willDie = playerHealth.health <= damage;
        StartCoroutine(PlayPlayerHitEffectsDelayed(willDie));
    }

    private IEnumerator PlayPlayerHitEffectsDelayed(bool willDie)
    {
        if (playerHitEffectsDelay > 0f)
            yield return new WaitForSeconds(playerHitEffectsDelay);

        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null)
        {
            if (willDie)
            {
                camShake.ShakeKill();
            }
            else
            {
                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                float hpRatio = (ph != null && ph.maxHealth > 0) ? Mathf.Clamp01((float)ph.health / ph.maxHealth) : 1f;
                float lowFactor = 1f - hpRatio;
                float intensity = Mathf.Lerp(playerHitShakeMinIntensity, playerHitShakeMaxIntensity, lowFactor);
                float duration = Mathf.Lerp(playerHitShakeMinDuration, playerHitShakeMaxDuration, lowFactor);
                camShake.Shake(intensity, duration);
            }
        }

        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null)
        {
            if (willDie) hitStop.Stop(0.25f, 15);
            else hitStop.Stop(playerHitPauseDuration, 45);
        }

        if (willDie)
        {
            SamuraiDeathVFX deathVfx = GetComponent<SamuraiDeathVFX>();
            if (deathVfx == null)
                deathVfx = gameObject.AddComponent<SamuraiDeathVFX>();
            deathVfx.TriggerDeathFlash(true);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead || !isActive) return;
        health -= damage;

        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null)
        {
            float healthRatio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
            float lowHealthFactor = 1f - healthRatio; // 0 at full HP, 1 near zero HP
            float shakeIntensity = Mathf.Lerp(lowHealthShakeMinIntensity, lowHealthShakeMaxIntensity, lowHealthFactor);
            float shakeDuration = Mathf.Lerp(lowHealthShakeMinDuration, lowHealthShakeMaxDuration, lowHealthFactor);
            camShake.Shake(shakeIntensity, shakeDuration);
        }

        if (anim != null) anim.SetTrigger("TakeHit");
        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        IsDefeated = true;
        if (anim != null) anim.SetTrigger("Death");
        SamuraiDeathVFX deathVfx = GetComponent<SamuraiDeathVFX>();
        if (deathVfx == null)
        {
            Debug.LogWarning("BossController: SamuraiDeathVFX not found on boss, adding fallback component.", this);
            deathVfx = gameObject.AddComponent<SamuraiDeathVFX>();
        }
        deathVfx.TriggerDeathFlash(false, true);

        if (treasureChestPrefab != null)
            spawnedChest = Instantiate(treasureChestPrefab, transform.position, Quaternion.identity);

        if (rb != null) rb.simulated = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        if (sprites == null || sprites.Length == 0)
        {
            gameObject.SetActive(false);
            yield break;
        }

        Color[] originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i] != null ? sprites[i].color : Color.white;

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

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    Color c = sprites[i].color;
                    c.a = 1f;
                    sprites[i].color = c;
                }
            }
        }

        if (anim != null)
        {
            anim.SetBool("isRunning", false);
            anim.Rebind();
            anim.Update(0f);
        }

        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
