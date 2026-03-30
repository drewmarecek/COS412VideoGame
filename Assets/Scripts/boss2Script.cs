using System.Collections;
using UnityEngine;

public class boss2Script : MonoBehaviour
{
    [Header("Activation")]
    public bool isActive = false;
    public bool IsDefeated { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody2D rb;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Health")]
    [SerializeField] private int maxHealth = 120;
    [SerializeField] private float deathDisappearDelay = 1.5f;
    private int currentHealth;
    private bool isDead;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float flyVerticalBoost = 1.0f;
    [SerializeField] private float flySpeed = 5.4f;
    [SerializeField] private float xStopDistance = 1.6f;

    [Header("Phase Mix")]
    [SerializeField] private Vector2 walkPhaseDuration = new Vector2(1.2f, 2.4f);
    [SerializeField] private Vector2 flyPhaseDuration = new Vector2(4.5f, 7.0f);
    [SerializeField] private float minFlyYAbovePlayer = 2.8f;
    [SerializeField] private float maxFlyYAbovePlayer = 5.0f;
    [SerializeField] private float flyRetargetInterval = 0.7f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private float attackRecoverFallback = 0.9f;

    [Header("Attack Hitboxes")]
    [SerializeField] private Collider2D[] attackHitboxes;

    private enum MovePhase { Walking, Flying }
    private MovePhase currentPhase = MovePhase.Walking;
    private float phaseTimer;
    private float attackTimer;
    private bool isAttacking;
    private float flyRetargetTimer;
    private Vector2 flyTarget;
    private SpriteRenderer[] sprites;
    private Color[] originalSpriteColors;
    private Collider2D[] allColliders;
    private Vector3 startPosition;
    private Vector3 startScale;
    private float startGravityScale = 1f;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Start()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        originalSpriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalSpriteColors[i] = sprites[i].color;

        allColliders = GetComponentsInChildren<Collider2D>(true);
        startPosition = transform.position;
        startScale = transform.localScale;
        if (rb != null) startGravityScale = rb.gravityScale;

        currentHealth = maxHealth;
        phaseTimer = Random.Range(walkPhaseDuration.x, walkPhaseDuration.y);
        EnsureHitboxesCanDamagePlayer();
        SetHitboxesEnabled(false);
    }

    private void Update()
    {
        if (isDead)
        {
            UpdateAnimatorParams();
            return;
        }

        if (!isActive || player == null)
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimatorParams();
            return;
        }

        attackTimer -= Time.deltaTime;
        phaseTimer -= Time.deltaTime;

        if (!isAttacking && phaseTimer <= 0f)
            TogglePhase();

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        FacePlayer();

        if (!isAttacking && attackTimer <= 0f && distanceToPlayer <= attackRange)
        {
            StartAttack();
        }
        else if (!isAttacking)
        {
            if (currentPhase == MovePhase.Walking)
                DoWalkMovement();
            else
                DoFlyMovement();
        }

        UpdateAnimatorParams();
    }

    private void TogglePhase()
    {
        if (currentPhase == MovePhase.Walking)
        {
            currentPhase = MovePhase.Flying;
            phaseTimer = Random.Range(flyPhaseDuration.x, flyPhaseDuration.y);
            flyRetargetTimer = 0f;
            if (rb != null) rb.gravityScale = 0f;
        }
        else
        {
            currentPhase = MovePhase.Walking;
            phaseTimer = Random.Range(walkPhaseDuration.x, walkPhaseDuration.y);
            if (rb != null) rb.gravityScale = 1f;
        }
    }

    private void DoWalkMovement()
    {
        if (rb == null) return;

        float dx = player.position.x - transform.position.x;
        float moveX = Mathf.Abs(dx) > xStopDistance ? Mathf.Sign(dx) * walkSpeed : 0f;
        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
    }

    private void DoFlyMovement()
    {
        if (rb == null) return;

        flyRetargetTimer -= Time.deltaTime;
        if (flyRetargetTimer <= 0f || Vector2.Distance(rb.position, flyTarget) < 0.25f)
        {
            flyRetargetTimer = flyRetargetInterval;
            float offsetX = Random.Range(-1.8f, 1.8f);
            float offsetY = Random.Range(minFlyYAbovePlayer, maxFlyYAbovePlayer);
            flyTarget = new Vector2(player.position.x + offsetX, player.position.y + offsetY);
        }

        Vector2 toTarget = (flyTarget - rb.position).normalized;
        Vector2 boostedDir = (toTarget + Vector2.up * flyVerticalBoost).normalized;
        Vector2 stepTarget = rb.position + boostedDir;
        Vector2 newPos = Vector2.MoveTowards(rb.position, stepTarget, flySpeed * Time.deltaTime);
        rb.MovePosition(newPos);
        rb.linearVelocity = Vector2.zero;
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anim != null)
        {
            if (Random.value < 0.5f) anim.SetTrigger("att1");
            else anim.SetTrigger("att2");
        }

        // Safety: if ResetAttack event is missing, recover automatically.
        StartCoroutine(AttackRecoverFailSafe());
    }

    private IEnumerator AttackRecoverFailSafe()
    {
        yield return new WaitForSeconds(attackRecoverFallback);
        if (isAttacking)
            ResetAttack();
    }

    // Animation Event
    public void OpenHitbox()
    {
        SetHitboxesEnabled(true);
    }

    // Animation Event
    public void CloseHitbox()
    {
        SetHitboxesEnabled(false);
    }

    // Animation Event
    public void ResetAttack()
    {
        isAttacking = false;
        SetHitboxesEnabled(false);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || !isActive) return;

        currentHealth -= amount;
        if (anim != null) anim.SetTrigger("takeHit");

        // Critical: interrupted attack must not leave hitboxes enabled.
        SetHitboxesEnabled(false);
        isAttacking = false;

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        IsDefeated = true;

        // Critical: disable attack hitboxes on death as well.
        SetHitboxesEnabled(false);
        isAttacking = false;

        if (anim != null) anim.SetTrigger("die");
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetAllCollidersEnabled(false);
        if (rb != null) rb.simulated = false;

        StartCoroutine(FadeOut());
    }

    public void ActivateBoss()
    {
        if (isDead) return;
        isActive = true;
    }

    public void DeactivateBoss()
    {
        isActive = false;
        isAttacking = false;
        SetHitboxesEnabled(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void ResetBoss()
    {
        StopAllCoroutines();

        currentHealth = maxHealth;
        isDead = false;
        IsDefeated = false;
        isActive = false;
        isAttacking = false;
        attackTimer = 0f;
        phaseTimer = Random.Range(walkPhaseDuration.x, walkPhaseDuration.y);
        currentPhase = MovePhase.Walking;
        flyRetargetTimer = 0f;

        transform.position = startPosition;
        transform.localScale = startScale;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = startGravityScale;
        }

        RestoreSpriteAlpha();
        SetAllCollidersEnabled(true);
        SetHitboxesEnabled(false);

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        gameObject.SetActive(true);
    }

    private void SetHitboxesEnabled(bool enabled)
    {
        if (attackHitboxes == null) return;
        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            if (attackHitboxes[i] != null)
                attackHitboxes[i].enabled = enabled;
        }
    }

    private void SetAllCollidersEnabled(bool enabled)
    {
        if (allColliders == null) return;
        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i] != null)
                allColliders[i].enabled = enabled;
        }
    }

    private void EnsureHitboxesCanDamagePlayer()
    {
        if (attackHitboxes == null) return;

        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            Collider2D hitbox = attackHitboxes[i];
            if (hitbox == null) continue;

            EnemyAttack enemyAttack = hitbox.GetComponent<EnemyAttack>();
            if (enemyAttack == null)
                enemyAttack = hitbox.gameObject.AddComponent<EnemyAttack>();

            enemyAttack.damage = meleeDamage;
            hitbox.isTrigger = true;
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (player.position.x > transform.position.x ? 1 : -1);
        transform.localScale = scale;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void UpdateAnimatorParams()
    {
        if (anim == null || rb == null) return;
        anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isGrounded", IsGrounded());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < deathDisappearDelay)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / deathDisappearDelay);

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color c = originalSpriteColors[i];
                c.a = originalSpriteColors[i].a * alpha;
                sprites[i].color = c;
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void RestoreSpriteAlpha()
    {
        if (sprites == null || originalSpriteColors == null) return;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = originalSpriteColors[i];
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}