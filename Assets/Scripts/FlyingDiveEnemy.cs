using UnityEngine;

/// <summary>
/// Before aggro: flies a horizontal figure-8 (lemniscate) around its spawn point.
/// After detection: dive toward the player, climb above them, hold, repeat.
/// Works with EnemyHealth (knockback) and EnemyAttack (contact damage). Do not add EnemyAI on the same object.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FlyingDiveEnemy : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        Diving,
        Climbing,
        Holding,
        ReturningHome
    }

    [Header("Detection")]
    [Tooltip("Player must be this close (or closer) to start attacking.")]
    public float detectionRadius = 8f;

    [Tooltip("Larger than detection — keeps combat from flickering when player moves in and out.")]
    public float loseAggroRadius = 12f;

    [Header("Patrol — figure 8 (before player is detected)")]
    [Tooltip("Horizontal size of the loop (half-width of the figure).")]
    public float figureEightWidth = 2.5f;

    [Tooltip("Vertical size of the lobes (scales sin(t)cos(t)).")]
    public float figureEightHeight = 1.75f;

    [Tooltip("How fast the enemy runs around the path (radians per second on the lemniscate parameter).")]
    public float figureEightAngularSpeed = 1.1f;

    [Header("Patrol / home")]
    [Tooltip("How fast the enemy flies back to spawn after losing aggro.")]
    public float returnHomeSpeed = 4f;

    [Tooltip("Stop returning when this close to spawn.")]
    public float idleArriveEpsilon = 0.15f;

    [Header("Dive")]
    [Tooltip("Homing dive speed while chasing the player.")]
    public float diveSpeed = 11f;

    [Tooltip("Stop dive when this close to the player (EnemyAttack still handles actual hits).")]
    public float diveEndDistance = 1.1f;

    [Tooltip("Failsafe if the player dodges — end dive after this many seconds.")]
    public float maxDiveTime = 2.5f;

    [Header("Climb / hover")]
    [Tooltip("Target height above the player after each dive.")]
    public float heightAbovePlayer = 5f;

    [Tooltip("Horizontal offset from player X when hovering above (0 = straight up).")]
    public float hoverOffsetX = 0f;

    public float climbSpeed = 5f;

    [Tooltip("How close to the hover point before the hold phase starts.")]
    public float climbArriveEpsilon = 0.35f;

    [Tooltip("Pause at the top before the next dive.")]
    public float holdDuration = 0.4f;

    [Tooltip("If a dive is aborted by hitting geometry first, hold this long before retrying.")]
    public float wallAbortHoldDuration = 1f;

    [Tooltip("After being hit (knockback), wait this long before diving again.")]
    public float postHitAttackDelay = 0.45f;

    [Header("Knockback (called by EnemyHealth)")]
    public float knockbackForce = 9f;
    public float knockbackDuration = 0.22f;

    [Header("Facing (optional sprite flip)")]
    public bool flipSpriteToFacePlayer = true;

    [Header("Unstick (walls / ground)")]
    [Tooltip("Ground + wall layers. Player is ignored by tag. Set to Nothing to disable.")]
    public LayerMask geometryLayers = -1;

    [Tooltip("How fast to nudge upward per physics step while overlapping geometry.")]
    public float unstickLiftSpeed = 10f;

    [Header("Obstacle Recovery")]
    [Tooltip("Probe distance used to detect blocking geometry when climbing.")]
    public float obstacleProbeDistance = 0.12f;
    [Tooltip("How fast to move during obstacle detour (down + sideways).")]
    public float obstacleDetourSpeed = 3.25f;

    Rigidbody2D rb;
    Animator animator;
    Transform player;
    Vector2 spawnPosition;
    Phase phase = Phase.Idle;

    float diveTimer;
    float holdTimer;
    float knockbackTimer;
    float attackCooldownTimer;
    float figureEightPhase;
    bool aggro;
    bool isDead;
    bool useWallAbortHold;
    int forcedDetourDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
    }

    void Start()
    {
        TryAcquirePlayer();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
    }

    void TryAcquirePlayer()
    {
        if (player != null && player.gameObject != null && player.gameObject.activeInHierarchy)
            return;

        player = null;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null && p.activeInHierarchy)
            player = p.transform;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        TryAcquirePlayer();

        if (player == null)
        {
            StepFigureEightPatrol();
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.fixedDeltaTime;

        float dist = Vector2.Distance(rb.position, player.position);
        if (!aggro && dist <= detectionRadius)
            aggro = true;
        else if (aggro && dist > loseAggroRadius)
        {
            aggro = false;
            phase = Phase.ReturningHome;
        }

        if (flipSpriteToFacePlayer)
            UpdateFacing();

        switch (phase)
        {
            case Phase.Idle:
                if (aggro && attackCooldownTimer <= 0f)
                    BeginDive();
                else if (aggro)
                    rb.linearVelocity = Vector2.zero;
                else
                    StepFigureEightPatrol();
                break;

            case Phase.Diving:
                diveTimer += Time.fixedDeltaTime;
                {
                    Vector2 to = (Vector2)player.position - rb.position;
                    float d = to.magnitude;
                    if (d > 0.01f)
                        rb.linearVelocity = (to / d) * diveSpeed;
                    else
                        rb.linearVelocity = Vector2.zero;

                    if (IsTouchingGeometry())
                    {
                        useWallAbortHold = true;
                        float vx = rb.linearVelocity.x;
                        if (Mathf.Abs(vx) > 0.05f)
                            forcedDetourDirection = vx > 0f ? -1 : 1;
                        phase = Phase.Climbing;
                    }
                    else if (d <= diveEndDistance || diveTimer >= maxDiveTime)
                        phase = Phase.Climbing;
                }
                break;

            case Phase.Climbing:
                {
                    Vector2 hover = GetHoverPoint();
                    Vector2 to = hover - rb.position;
                    float d = to.magnitude;
                    if (d > climbArriveEpsilon)
                    {
                        float horizontalIntent = to.x;
                        bool wantsToGoUp = to.y > 0.05f;
                        if (forcedDetourDirection != 0)
                        {
                            ApplyObstacleDetour(forcedDetourDirection);
                            forcedDetourDirection = 0;
                            break;
                        }

                        if (wantsToGoUp && IsBlockedInDirection(Vector2.up))
                        {
                            int sideDir = Mathf.Abs(horizontalIntent) > 0.05f ? (horizontalIntent > 0f ? 1 : -1) : GetAlternatingSideDirection();
                            ApplyObstacleDetour(sideDir);
                            break;
                        }

                        if (Mathf.Abs(horizontalIntent) > 0.05f && IsBlockedInDirection(new Vector2(Mathf.Sign(horizontalIntent), 0f)))
                        {
                            int oppositeDir = horizontalIntent > 0f ? -1 : 1;
                            ApplyObstacleDetour(oppositeDir);
                            break;
                        }

                        rb.linearVelocity = (to / d) * climbSpeed;
                    }
                    else
                    {
                        rb.position = hover;
                        rb.linearVelocity = Vector2.zero;
                        holdTimer = useWallAbortHold ? wallAbortHoldDuration : holdDuration;
                        useWallAbortHold = false;
                        phase = Phase.Holding;
                    }
                }
                break;

            case Phase.Holding:
                rb.linearVelocity = Vector2.zero;
                holdTimer -= Time.fixedDeltaTime;
                if (holdTimer <= 0f)
                {
                    if (aggro && attackCooldownTimer <= 0f)
                        BeginDive();
                    else if (!aggro)
                        phase = Phase.ReturningHome;
                    else
                        holdTimer = holdDuration * 0.25f;
                }
                break;

            case Phase.ReturningHome:
                if (Vector2.Distance(rb.position, spawnPosition) <= idleArriveEpsilon)
                {
                    rb.position = spawnPosition;
                    rb.linearVelocity = Vector2.zero;
                    figureEightPhase = 0f;
                    phase = Phase.Idle;
                }
                else
                {
                    DriftTowards(spawnPosition, returnHomeSpeed);
                }
                break;
        }

        TryUnstickFromGeometry();
    }

    void TryUnstickFromGeometry()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector2 size = (Vector2)b.size * 0.95f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, size, 0f, geometryLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D h = hits[i];
            if (h == null) continue;
            if (h.CompareTag("Player")) continue;
            if (h.attachedRigidbody == rb) continue;
            if (h.transform.IsChildOf(transform)) continue;

            rb.MovePosition(rb.position + Vector2.up * unstickLiftSpeed * Time.fixedDeltaTime);
            return;
        }
    }

    bool IsTouchingGeometry()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Bounds b = col.bounds;
        Vector2 size = (Vector2)b.size * 0.98f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, size, 0f, geometryLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D h = hits[i];
            if (h == null) continue;
            if (h.CompareTag("Player")) continue;
            if (h.attachedRigidbody == rb) continue;
            if (h.transform.IsChildOf(transform)) continue;
            return true;
        }

        return false;
    }

    bool IsBlockedInDirection(Vector2 direction)
    {
        if (geometryLayers.value == 0) return false;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Vector2 dir = direction.normalized;
        if (dir.sqrMagnitude < 0.0001f) return false;

        Bounds b = col.bounds;
        Vector2 size = (Vector2)b.size * 0.9f;
        RaycastHit2D hit = Physics2D.BoxCast(b.center, size, 0f, dir, obstacleProbeDistance, geometryLayers);
        return IsBlockingGeometryHit(hit.collider);
    }

    bool IsBlockingGeometryHit(Collider2D hit)
    {
        if (hit == null) return false;
        if (hit.CompareTag("Player")) return false;
        if (hit.attachedRigidbody == rb) return false;
        if (hit.transform.IsChildOf(transform)) return false;
        return true;
    }

    void ApplyObstacleDetour(int sideDirection)
    {
        if (sideDirection == 0)
            sideDirection = GetAlternatingSideDirection();

        rb.linearVelocity = new Vector2(Mathf.Sign(sideDirection) * obstacleDetourSpeed, -obstacleDetourSpeed);
    }

    int GetAlternatingSideDirection()
    {
        // Alternate left/right on repeated retries to escape tight corners.
        forcedDetourDirection = forcedDetourDirection == 1 ? -1 : 1;
        return forcedDetourDirection;
    }

    Vector2 GetHoverPoint()
    {
        return new Vector2(player.position.x + hoverOffsetX, player.position.y + heightAbovePlayer);
    }

    void BeginDive()
    {
        diveTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        phase = Phase.Diving;
        if (animator != null)
            animator.SetTrigger("Attack");
    }

    /// <summary>Lemniscate of Gerono centered on spawn: ∞ shape in X/Y.</summary>
    Vector2 GetFigureEightOffset(float t)
    {
        float s = Mathf.Sin(t);
        return new Vector2(figureEightWidth * s, figureEightHeight * s * Mathf.Cos(t));
    }

    void StepFigureEightPatrol()
    {
        rb.linearVelocity = Vector2.zero;
        figureEightPhase += figureEightAngularSpeed * Time.fixedDeltaTime;
        if (figureEightPhase > Mathf.PI * 2f)
            figureEightPhase -= Mathf.PI * 2f;

        Vector2 target = spawnPosition + GetFigureEightOffset(figureEightPhase);
        rb.MovePosition(target);
    }

    void DriftTowards(Vector2 target, float speed)
    {
        Vector2 to = target - rb.position;
        float d = to.magnitude;
        if (d <= idleArriveEpsilon)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = (to / d) * speed;
    }

    void UpdateFacing()
    {
        float sx = transform.localScale.x;
        if (sx == 0f) return;

        float dirX;
        if (aggro && player != null)
            dirX = player.position.x - transform.position.x;
        else if (phase == Phase.Idle && !aggro)
            dirX = figureEightWidth * Mathf.Cos(figureEightPhase);
        else if (phase == Phase.ReturningHome)
        {
            Vector2 to = spawnPosition - rb.position;
            dirX = to.x;
        }
        else
            dirX = player != null ? player.position.x - transform.position.x : 0f;

        if (Mathf.Abs(dirX) < 0.05f) return;

        bool faceRight = dirX > 0f;
        bool scaleSaysRight = sx > 0f;
        if (faceRight != scaleSaysRight)
        {
            Vector3 s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }
    }

    /// <summary>Called by EnemyHealth when the player hits this enemy (same idea as EnemyAI).</summary>
    public void ApplyKnockback()
    {
        if (isDead) return;

        TryAcquirePlayer();

        knockbackTimer = knockbackDuration;
        attackCooldownTimer = postHitAttackDelay;
        phase = Phase.Idle;
        diveTimer = 0f;

        float dirX = 1f;
        if (player != null)
            dirX = Mathf.Sign(transform.position.x - player.position.x);
        if (dirX == 0f) dirX = 1f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dirX * knockbackForce, knockbackForce * 0.35f), ForceMode2D.Impulse);
    }

    public void PlayDeathAndDestroy()
    {
        if (isDead) return;
        isDead = true;
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? spawnPosition : (Vector2)transform.position;

        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.9f);
        Vector3 prev = center + GetFigureEightOffset(0f);
        const int segments = 48;
        for (int i = 1; i <= segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + GetFigureEightOffset(t);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, loseAggroRadius);
    }
}
