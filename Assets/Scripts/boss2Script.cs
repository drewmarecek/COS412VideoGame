using System.Collections;
using UnityEngine;

/// <summary>
/// Level 2 boss: hover–evade → telegraph → fixed-target swoop → strike + vulnerability → ascend loop.
/// Melee only lands during the post-swoop vulnerability window; bullets can hit whenever the boss is active.
/// </summary>
public class boss2Script : MonoBehaviour
{
    public enum Boss2DamageSource
    {
        /// <summary>Projectiles — allowed outside vulnerability; tune hover motion for difficulty.</summary>
        Ranged,
        /// <summary>Player sword — only during the recovery vulnerability window.</summary>
        Melee,
        /// <summary>Same damage rules as <see cref="Ranged"/> (only <see cref="Melee"/> is gated).</summary>
        Unspecified
    }

    public enum CombatState
    {
        /// <summary>Before arena trigger — no combat movement, animator held idle.</summary>
        Dormant,
        HoverEvade,
        Telegraph,
        Swoop,
        RecoveryAscend
    }

    [Header("Activation")]
    public bool isActive = false;
    public bool IsDefeated { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody2D rb;

    [Header("Ground / swoop targeting")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Raycast upward from player before casting down, so we hit floor under them.")]
    [SerializeField] private float groundProbeYOffset = 2f;
    [Tooltip("How far down to search for ground under the locked X.")]
    [SerializeField] private float groundProbeDistance = 24f;
    [Tooltip("Extra ray length when clamping the boss above ground (tall arenas).")]
    [SerializeField] private float groundClampRayExtraDistance = 32f;
    [Tooltip("Minimum space between ground surface and the bottom of the ground-check circle.")]
    [SerializeField] private float minimumClearanceAboveGround = 0.1f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 120;
    [SerializeField] private float deathDisappearDelay = 1.5f;
    private int currentHealth;
    private bool isDead;

    [Header("Hover & evade")]
    [Tooltip("If true, hover uses a constant world Y (no vertical bob). If false, Y follows the player plus the offsets below.")]
    [SerializeField] private bool useFixedHoverWorldHeight = true;
    [Tooltip("World-space Y while hovering (when Use Fixed Hover World Height is on). Tune to your arena.")]
    [SerializeField] private float fixedHoverWorldY = 6f;
    [Tooltip("World-space height above the player used as the hover band center (when not using fixed hover).")]
    [SerializeField] private float hoverHeightAbovePlayer = 4.75f;
    [Tooltip("Boss Y will stay at least this far above the player (melee immunity band; when not using fixed hover).")]
    [SerializeField] private float minVerticalClearanceAbovePlayer = 2.35f;
    [Tooltip("Horizontal sway amplitude (sum of sines + Perlin).")]
    [SerializeField] private float hoverHorizontalAmplitude = 2.6f;
    [Tooltip("Secondary horizontal wobble (different frequency).")]
    [SerializeField] private float hoverHorizontalSecondaryAmplitude = 1.15f;
    [SerializeField] private float hoverPrimaryFrequency = 1.15f;
    [SerializeField] private float hoverSecondaryFrequency = 2.07f;
    [Tooltip("Vertical bob on top of hover band (keeps path from looking flat).")]
    [SerializeField] private float hoverVerticalBobAmplitude = 0.45f;
    [SerializeField] private float hoverVerticalBobFrequency = 1.6f;
    [Tooltip("Perlin drift strength in world units (applied around the sine path).")]
    [SerializeField] private float hoverPerlinAmplitude = 0.85f;
    [SerializeField] private float hoverPerlinScrollSpeed = 0.42f;
    [SerializeField] private float hoverPerlinScale = 1.8f;
    [SerializeField] private float hoverMoveSpeed = 5.2f;

    [Header("Attack cadence")]
    [Tooltip("Time between the end of one full cycle (back on hover) and the next telegraph.")]
    [SerializeField] private float swoopCooldown = 2.8f;
    [Tooltip("After taking damage, wait this long before a new telegraph can start.")]
    [SerializeField] private float damageInterruptSwoopDelay = 1.05f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphDuration = 0.75f;
    [Tooltip("Optional: enable while telegraphing (auto-disabled after).")]
    [SerializeField] private GameObject telegraphParticleRoot;
    [Header("Telegraph — color flash")]
    [SerializeField] private bool useTelegraphColorFlash = true;
    [SerializeField] private Color telegraphFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float telegraphFlashFrequency = 14f;

    [Header("Swoop")]
    [Tooltip("Travel speed toward the locked world point (no homing while swooping).")]
    [SerializeField] private float swoopSpeed = 22f;
    [Tooltip("Stop swoop when within this distance of the strike point.")]
    [SerializeField] private float swoopArriveEpsilon = 0.22f;

    [Tooltip("Raise the swoop target this far above the ground hit so the boss does not embed in tiles.")]
    [SerializeField] private float strikeGroundClearance = 0.55f;

    [Header("Recovery — strike & vulnerability")]
    [Tooltip("Boss attack hitboxes stay on for this long right after landing.")]
    [SerializeField] private float strikeHitboxDuration = 0.22f;
    [Tooltip("After the strike window, boss accepts melee damage for this duration.")]
    [SerializeField] private float vulnerabilityDuration = 1.25f;
    [Tooltip("How fast the boss returns to hover altitude after vulnerability.")]
    [SerializeField] private float returnToHoverSpeed = 12f;
    [Tooltip("Within this distance of the hover target, the cycle restarts.")]
    [SerializeField] private float hoverArriveEpsilon = 0.35f;
    [Tooltip("Hard cap on grounded recovery (strike + vulnerable + ascend). When exceeded, boss snaps to hover and resumes — fixes stuck-on-ground if ascend target drifts.")]
    [SerializeField] private float maxGroundedRecoveryDuration = 2.75f;

    [Tooltip("Nudge per physics step while the ground check overlaps solid ground during recovery (unstick).")]
    [SerializeField] private float groundUnstickStep = 0.14f;

    [Header("Hitboxes — receiving damage")]
    [Tooltip("Colliders the player/bullets use to damage this boss. Never disabled by strike hitbox toggling. Leave empty only if attack hitboxes are separate child triggers.")]
    [SerializeField] private Collider2D[] receivingDamageColliders;

    [Header("Legacy animator (optional)")]
    [Tooltip("If assigned, sets float speed / bool isGrounded for blend trees.")]
    [SerializeField] private bool driveAnimatorFromMotion = true;

    [Header("Animator — strike (landing)")]
    [Tooltip("Trigger fired when the swoop lands and melee hitboxes open. Boss 1 controller uses att1 / att2 (Any State → attack). Leave empty to skip.")]
    [SerializeField] private string strikeAnimatorTrigger = "att1";

    [Header("Recovery — after landing")]
    [Tooltip("If true, skip the dedicated Ascend sub-phase after vulnerability and return to Hover Evade immediately. The boss still reclimbs to normal hover height during Hover Evade (MoveTowards aerial target). If false, uses Ascend to reach a cached hover point first.")]
    [SerializeField] private bool skipAscendAfterSwoop = true;

    [Header("Attack Hitboxes")]
    [SerializeField] private Collider2D[] attackHitboxes;
    [SerializeField] private int meleeDamage = 1;

    public CombatState CurrentCombatState { get; private set; } = CombatState.Dormant;

    private SpriteRenderer[] sprites;
    private Color[] originalSpriteColors;
    private Collider2D[] allColliders;
    private Vector3 startPosition;
    private Vector3 startScale;
    private float startGravityScale = 1f;

    private float hoverTime;
    private float perlinSeed;
    private Vector2 lockedStrikePoint;
    private float stateTimer;
    private float nextSwoopAllowedTime;

    private bool meleeVulnerabilityActive;
    private RecoverySubPhase recoveryPhase;
    private float recoveryPhaseTimer;
    private float groundedRecoveryElapsed;
    private Vector2 cachedAscendHoverTarget;

    private enum RecoverySubPhase
    {
        Strike,
        Vulnerable,
        Ascending
    }

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        perlinSeed = Random.Range(0f, 10000f);

        if (receivingDamageColliders == null || receivingDamageColliders.Length == 0)
        {
            Collider2D rootCol = GetComponent<Collider2D>();
            if (rootCol != null)
                receivingDamageColliders = new[] { rootCol };
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
        EnsureHitboxesCanDamagePlayer();
        SetHitboxesEnabled(false);
        nextSwoopAllowedTime = Time.time + swoopCooldown * 0.35f;
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
            ApplyDormantAnimatorIdle();
            return;
        }

        if (CurrentCombatState == CombatState.Telegraph && useTelegraphColorFlash && sprites.Length > 0)
        {
            float t = Mathf.PingPong(Time.time * telegraphFlashFrequency, 1f);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                sprites[i].color = Color.Lerp(originalSpriteColors[i], telegraphFlashColor, t);
            }
        }

        UpdateAnimatorParams();
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null) return;

        if (!isActive || player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        hoverTime += Time.fixedDeltaTime;

        switch (CurrentCombatState)
        {
            case CombatState.Dormant:
                break;

            case CombatState.HoverEvade:
                StepHoverEvade();
                if (Time.time >= nextSwoopAllowedTime)
                    BeginTelegraph();
                break;

            case CombatState.Telegraph:
                StepTelegraph();
                break;

            case CombatState.Swoop:
                StepSwoop();
                break;

            case CombatState.RecoveryAscend:
                StepRecoveryAscend();
                break;
        }

        rb.linearVelocity = Vector2.zero;
    }

    private void BeginTelegraph()
    {
        CurrentCombatState = CombatState.Telegraph;
        stateTimer = telegraphDuration;
        lockedStrikePoint = ComputeLockedStrikePoint();
        if (telegraphParticleRoot != null)
            telegraphParticleRoot.SetActive(true);
    }

    private Vector2 ComputeLockedStrikePoint()
    {
        float x = player.position.x;
        Vector2 origin = new Vector2(x, player.position.y + groundProbeYOffset);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundProbeDistance, groundLayer);
        Vector2 strike = hit.collider != null
            ? hit.point + Vector2.up * strikeGroundClearance
            : new Vector2(x, player.position.y + strikeGroundClearance);
        return ClampPositionAboveGround(strike);
    }

    private Vector2 GetIdealHoverPosition()
    {
        float t = hoverTime;
        float ox = Mathf.Sin(t * hoverPrimaryFrequency) * hoverHorizontalAmplitude
                   + Mathf.Sin(t * hoverSecondaryFrequency + 1.7f) * hoverHorizontalSecondaryAmplitude;
        float px = (Mathf.PerlinNoise(perlinSeed + t * hoverPerlinScrollSpeed, perlinSeed * 0.31f) - 0.5f) * 2f * hoverPerlinAmplitude;

        float x = player.position.x + ox + px;
        float y;
        if (useFixedHoverWorldHeight)
            y = fixedHoverWorldY;
        else
        {
            float baseY = player.position.y + hoverHeightAbovePlayer;
            float minY = player.position.y + minVerticalClearanceAbovePlayer;
            float yBand = Mathf.Max(baseY, minY);
            float py = (Mathf.PerlinNoise(perlinSeed * 0.17f, t * hoverPerlinScrollSpeed * hoverPerlinScale) - 0.5f) * 2f * (hoverPerlinAmplitude * 0.55f);
            y = yBand + Mathf.Sin(t * hoverVerticalBobFrequency) * hoverVerticalBobAmplitude + py;
        }

        return ClampPositionAboveGround(new Vector2(x, y));
    }

    /// <summary>
    /// Keeps the boss root high enough that the ground check does not intersect solid ground below.
    /// </summary>
    private Vector2 ClampPositionAboveGround(Vector2 worldPosition)
    {
        if (groundLayer.value == 0 || rb == null)
            return worldPosition;

        float originY = worldPosition.y + groundProbeYOffset;
        if (player != null)
            originY = Mathf.Max(originY, player.position.y + groundProbeYOffset);

        float rayLen = groundProbeDistance + groundClampRayExtraDistance;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldPosition.x, originY), Vector2.down, rayLen, groundLayer);
        if (hit.collider == null)
            return worldPosition;

        float groundTop = hit.point.y;
        float minRootY;
        if (groundCheck != null)
        {
            float groundCheckOffsetY = groundCheck.position.y - rb.position.y;
            minRootY = groundTop + minimumClearanceAboveGround - groundCheckOffsetY + groundCheckRadius;
        }
        else
            minRootY = groundTop + minimumClearanceAboveGround + 0.5f;

        if (worldPosition.y < minRootY)
            worldPosition.y = minRootY;
        return worldPosition;
    }

    private void MoveBossTo(Vector2 worldPosition)
    {
        if (rb == null) return;
        rb.MovePosition(ClampPositionAboveGround(worldPosition));
    }

    private void StepHoverEvade()
    {
        meleeVulnerabilityActive = false;
        Vector2 target = GetIdealHoverPosition();
        Vector2 pos = rb.position;
        Vector2 next = Vector2.MoveTowards(pos, target, hoverMoveSpeed * Time.fixedDeltaTime);
        MoveBossTo(next);
        FaceToward(target.x);
    }

    private void StepTelegraph()
    {
        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
        {
            EndTelegraphVisuals();
            CurrentCombatState = CombatState.Swoop;
            FaceToward(lockedStrikePoint.x);
            return;
        }

        FacePlayer();
    }

    private void EndTelegraphVisuals()
    {
        if (telegraphParticleRoot != null)
            telegraphParticleRoot.SetActive(false);
        RestoreSpriteColors();
    }

    private void StepSwoop()
    {
        meleeVulnerabilityActive = false;
        Vector2 pos = rb.position;
        Vector2 next = Vector2.MoveTowards(pos, lockedStrikePoint, swoopSpeed * Time.fixedDeltaTime);
        MoveBossTo(next);
        FaceToward(lockedStrikePoint.x);

        if (Vector2.Distance(next, lockedStrikePoint) <= swoopArriveEpsilon)
        {
            MoveBossTo(lockedStrikePoint);
            BeginRecovery();
        }
    }

    private void BeginRecovery()
    {
        CurrentCombatState = CombatState.RecoveryAscend;
        recoveryPhase = RecoverySubPhase.Strike;
        recoveryPhaseTimer = strikeHitboxDuration;
        groundedRecoveryElapsed = 0f;
        meleeVulnerabilityActive = false;
        SetHitboxesEnabled(true);

        if (anim != null && !string.IsNullOrEmpty(strikeAnimatorTrigger))
            anim.SetTrigger(strikeAnimatorTrigger);

        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null)
            camShake.ShakeHit();

        TryUnstickBossFromGround();
    }

    private void StepRecoveryAscend()
    {
        groundedRecoveryElapsed += Time.fixedDeltaTime;
        if (groundedRecoveryElapsed >= maxGroundedRecoveryDuration)
        {
            ForceExitGroundedRecovery();
            return;
        }

        TryUnstickBossFromGround();

        switch (recoveryPhase)
        {
            case RecoverySubPhase.Strike:
                recoveryPhaseTimer -= Time.fixedDeltaTime;
                if (recoveryPhaseTimer <= 0f)
                {
                    SetHitboxesEnabled(false);
                    recoveryPhase = RecoverySubPhase.Vulnerable;
                    recoveryPhaseTimer = vulnerabilityDuration;
                    meleeVulnerabilityActive = true;
                }
                FaceToward(lockedStrikePoint.x);
                break;

            case RecoverySubPhase.Vulnerable:
                recoveryPhaseTimer -= Time.fixedDeltaTime;
                if (recoveryPhaseTimer <= 0f)
                {
                    meleeVulnerabilityActive = false;
                    if (skipAscendAfterSwoop)
                        CompleteRecoveryCycle();
                    else
                    {
                        recoveryPhase = RecoverySubPhase.Ascending;
                        cachedAscendHoverTarget = GetIdealHoverPosition();
                    }
                }
                FacePlayer();
                break;

            case RecoverySubPhase.Ascending:
                {
                    Vector2 pos = rb.position;
                    Vector2 next = Vector2.MoveTowards(pos, cachedAscendHoverTarget, returnToHoverSpeed * Time.fixedDeltaTime);
                    MoveBossTo(next);
                    FaceToward(cachedAscendHoverTarget.x);

                    if (Vector2.Distance(next, cachedAscendHoverTarget) <= hoverArriveEpsilon)
                    {
                        CompleteRecoveryCycle();
                    }
                }
                break;
        }
    }

    private void CompleteRecoveryCycle()
    {
        CurrentCombatState = CombatState.HoverEvade;
        nextSwoopAllowedTime = Time.time + swoopCooldown;
        hoverTime = Random.Range(0f, Mathf.PI * 2f);
        groundedRecoveryElapsed = 0f;
        meleeVulnerabilityActive = false;
        SetHitboxesEnabled(false);
    }

    private void TryUnstickBossFromGround()
    {
        if (rb == null || groundCheck == null || groundUnstickStep <= 0f) return;
        if (groundLayer.value == 0) return;

        Collider2D embedded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius * 1.4f, groundLayer);
        if (embedded != null)
            MoveBossTo(rb.position + Vector2.up * groundUnstickStep);
    }

    private void ForceExitGroundedRecovery()
    {
        SetHitboxesEnabled(false);
        meleeVulnerabilityActive = false;
        EndTelegraphVisuals();
        if (rb != null)
        {
            if (!skipAscendAfterSwoop && player != null)
                MoveBossTo(GetIdealHoverPosition());
            // If skipAscendAfterSwoop: stay at current Y; Hover Evade moves toward aerial hover target.
        }
        CurrentCombatState = CombatState.HoverEvade;
        nextSwoopAllowedTime = Time.time + swoopCooldown;
        hoverTime = Random.Range(0f, Mathf.PI * 2f);
        groundedRecoveryElapsed = 0f;
        recoveryPhase = RecoverySubPhase.Strike;
        recoveryPhaseTimer = 0f;
    }

    private void FacePlayer()
    {
        if (player == null) return;
        FaceToward(player.position.x);
    }

    private void FaceToward(float worldX)
    {
        Vector3 scale = transform.localScale;
        float dir = worldX - transform.position.x;
        if (Mathf.Abs(dir) < 0.02f) return;
        scale.x = Mathf.Abs(scale.x) * (dir > 0f ? 1f : -1f);
        transform.localScale = scale;
    }

    private void RestoreSpriteColors()
    {
        if (sprites == null || originalSpriteColors == null) return;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = originalSpriteColors[i];
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Boss2DamageSource.Ranged);
    }

    /// <returns>False if damage was blocked (e.g. melee during hover) or the boss was inactive/dead.</returns>
    public bool TakeDamage(int amount, Boss2DamageSource source)
    {
        if (isDead || !isActive) return false;

        if (source == Boss2DamageSource.Melee && !meleeVulnerabilityActive)
            return false;

        currentHealth -= amount;
        if (anim != null) anim.SetTrigger("takeHit");

        bool inGroundedRecovery = CurrentCombatState == CombatState.RecoveryAscend;
        if (inGroundedRecovery)
        {
            // Keep strike / vulnerable / ascend timers running; do not cancel recovery or fly away early.
        }
        else
        {
            SetHitboxesEnabled(false);
            meleeVulnerabilityActive = false;
            EndTelegraphVisuals();
            CurrentCombatState = CombatState.HoverEvade;
            nextSwoopAllowedTime = Time.time + damageInterruptSwoopDelay;
        }

        if (currentHealth <= 0)
            Die();

        return true;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        IsDefeated = true;
        SamuraiDeathVFX deathVfx = GetComponent<SamuraiDeathVFX>();
        if (deathVfx == null)
        {
            Debug.LogWarning("boss2Script: SamuraiDeathVFX not found on boss, adding fallback component.", this);
            deathVfx = gameObject.AddComponent<SamuraiDeathVFX>();
        }
        deathVfx.TriggerDeathFlash();

        SetHitboxesEnabled(false);
        meleeVulnerabilityActive = false;
        EndTelegraphVisuals();

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
        CurrentCombatState = CombatState.HoverEvade;
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        nextSwoopAllowedTime = Time.time + swoopCooldown * 0.25f;
    }

    public void DeactivateBoss()
    {
        isActive = false;
        CurrentCombatState = CombatState.Dormant;
        SetHitboxesEnabled(false);
        meleeVulnerabilityActive = false;
        EndTelegraphVisuals();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = startGravityScale;
        }
    }

    public void ResetBoss()
    {
        StopAllCoroutines();

        currentHealth = maxHealth;
        isDead = false;
        IsDefeated = false;
        isActive = false;
        CurrentCombatState = CombatState.Dormant;
        hoverTime = 0f;
        stateTimer = 0f;
        recoveryPhaseTimer = 0f;
        groundedRecoveryElapsed = 0f;
        meleeVulnerabilityActive = false;
        nextSwoopAllowedTime = 0f;

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
        EndTelegraphVisuals();

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        gameObject.SetActive(true);
    }

    /// <summary>Animation events on attack clips are ignored — hitbox timing is driven by <see cref="strikeHitboxDuration"/>.</summary>
    public void OpenHitbox() { }

    public void CloseHitbox() { }

    public void ResetAttack() { }

    private bool IsReceivingDamageCollider(Collider2D c)
    {
        if (c == null || receivingDamageColliders == null) return false;
        for (int i = 0; i < receivingDamageColliders.Length; i++)
        {
            if (receivingDamageColliders[i] == c)
                return true;
        }
        return false;
    }

    private void SetHitboxesEnabled(bool enabled)
    {
        if (attackHitboxes == null) return;
        for (int i = 0; i < attackHitboxes.Length; i++)
        {
            Collider2D c = attackHitboxes[i];
            if (c == null || IsReceivingDamageCollider(c))
                continue;
            c.enabled = enabled;
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

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void ApplyDormantAnimatorIdle()
    {
        if (!driveAnimatorFromMotion || anim == null || rb == null) return;
        anim.SetFloat("speed", 0f);
        anim.SetFloat("yVelocity", 0f);
        anim.SetBool("isGrounded", IsGrounded());
    }

    private void UpdateAnimatorParams()
    {
        if (!driveAnimatorFromMotion || anim == null || rb == null) return;

        if (!isActive || CurrentCombatState == CombatState.Dormant)
        {
            ApplyDormantAnimatorIdle();
            return;
        }

        float displaySpeed = 0f;
        switch (CurrentCombatState)
        {
            case CombatState.Dormant:
                displaySpeed = 0f;
                break;
            case CombatState.HoverEvade:
                displaySpeed = hoverMoveSpeed;
                break;
            case CombatState.Telegraph:
                displaySpeed = 0f;
                break;
            case CombatState.Swoop:
                displaySpeed = swoopSpeed;
                break;
            case CombatState.RecoveryAscend:
                displaySpeed = recoveryPhase == RecoverySubPhase.Ascending ? returnToHoverSpeed : 0f;
                break;
        }

        anim.SetFloat("speed", displaySpeed);
        float yVel = 0f;
        if (CurrentCombatState == CombatState.Swoop)
            yVel = -Mathf.Abs(swoopSpeed);
        else if (CurrentCombatState == CombatState.RecoveryAscend && recoveryPhase == RecoverySubPhase.Ascending)
            yVel = Mathf.Abs(returnToHoverSpeed);
        anim.SetFloat("yVelocity", yVel);
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
        RestoreSpriteColors();
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
