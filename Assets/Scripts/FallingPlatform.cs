using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kinematic bridge segment: when the player lands on top, waits <see cref="fallDelay"/>, then drops.
///
/// BRIDGE GROUPS: give every block in a bridge the same <see cref="bridgeGroupId"/> and a unique
/// <see cref="blockIndex"/> (1, 2, 3 …). When the player lands on block 5, blocks 1-4 all start
/// falling automatically so the player can't backpedal.
///
/// RESPAWN: call <see cref="ResetAll"/> (wired into PlayerHealth.RespawnImmediate) to restore every
/// block in the scene.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How long the player can stand on the block before it drops.")]
    [SerializeField] public float fallDelay = 0.2f;

    [Tooltip("Seconds after the block starts falling before it is removed.")]
    [SerializeField] float destroyDelay = 2f;

    [Header("Bridge Group (optional)")]
    [Tooltip("Give every block in the same bridge the same ID (e.g. 'Bridge_1').")]
    [SerializeField] public string bridgeGroupId = "";

    [Tooltip("Position of this block in the bridge (1 = closest to the player start). " +
             "When the player lands on block N, all blocks with a lower index in the same group also fall.")]
    [SerializeField] public int blockIndex = 0;

    [Tooltip("Cascade delay per step: earlier blocks start their shake slightly later so the " +
             "collapse feels like a wave toward the player. Set 0 to have all drop instantly.")]
    [SerializeField] float cascadeStepDelay = 0.04f;

    [Header("Top collision")]
    [Tooltip("Contact point must be this close to the top of the BoxCollider2D (world Y) to count as a landing.")]
    [SerializeField] float topSurfaceSlop = 0.12f;

    [Header("Shake (optional)")]
    [Tooltip("Max random offset in world units during fallDelay. 0 = no shake.")]
    [SerializeField] float shakeIntensity = 0.025f;

    [Header("Removal")]
    [Tooltip("If off (default), the block is SetActive(false) so ResetAll can reuse it without a prefab.")]
    [SerializeField] bool destroyGameObject = false;

    [Tooltip("Instantiated on ResetAll if Destroy Game Object is on.")]
    [SerializeField] GameObject respawnPrefab;

    // -----------------------------------------------------------------------
    static readonly List<FallingPlatform> instances = new List<FallingPlatform>();

    Rigidbody2D rb;
    BoxCollider2D box;

    Vector3 initialWorldPosition;
    Quaternion initialWorldRotation;
    Vector3 initialLocalScale;
    Transform initialParent;
    RigidbodyType2D initialBodyType;

    bool sequenceRunning;
    Coroutine fallRoutine;

    // Prevents scene-startup physics from firing the collapse before the player touches anything.
    bool readyForCollision;

    // -----------------------------------------------------------------------
    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        CaptureInitialState();
        if (!instances.Contains(this))
            instances.Add(this);
    }

    void FixedUpdate()
    {
        // Allow collisions only after at least one physics step has passed so
        // overlapping-at-spawn contacts don't immediately trigger a fall.
        readyForCollision = true;
    }

    void OnDestroy()
    {
        instances.Remove(this);
    }

    // -----------------------------------------------------------------------
    void CaptureInitialState()
    {
        initialWorldPosition = transform.position;
        initialWorldRotation = transform.rotation;
        initialLocalScale    = transform.localScale;
        initialParent        = transform.parent;
        if (rb != null) initialBodyType = rb.bodyType;
    }

    // -----------------------------------------------------------------------
    public static void ResetAll()
    {
        for (int i = 0; i < instances.Count; i++)
        {
            FallingPlatform p = instances[i];
            if (p != null) p.Restore();
        }
    }

    public void Restore()
    {
        if (fallRoutine != null) { StopCoroutine(fallRoutine); fallRoutine = null; }

        sequenceRunning  = false;
        readyForCollision = false;   // re-arm the startup guard on respawn
        gameObject.SetActive(true);
        transform.SetParent(initialParent, true);
        transform.position   = initialWorldPosition;
        transform.rotation   = initialWorldRotation;
        transform.localScale = initialLocalScale;

        if (box != null) box.enabled = true;
        if (rb  != null)
        {
            rb.bodyType         = initialBodyType;
            rb.linearVelocity   = Vector2.zero;
            rb.angularVelocity  = 0f;
            rb.simulated        = true;
        }
    }

    // -----------------------------------------------------------------------
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!readyForCollision) return;
        if (sequenceRunning) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (!IsLandingOnTop(collision)) return;

        TriggerFall(startDelay: 0f);

        // Cascade: drop all earlier-numbered blocks in the same bridge.
        if (!string.IsNullOrEmpty(bridgeGroupId) && blockIndex > 0)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                FallingPlatform other = instances[i];
                if (other == null || other == this) continue;
                if (other.bridgeGroupId != bridgeGroupId) continue;
                if (other.blockIndex >= blockIndex) continue;
                if (other.sequenceRunning) continue;

                // Earlier blocks get a tiny extra delay so the wave travels toward the player start.
                float extra = (blockIndex - other.blockIndex) * cascadeStepDelay;
                other.TriggerFall(startDelay: extra);
            }
        }
    }

    /// <summary>Starts the fall sequence, optionally after an initial pause (for cascade use).</summary>
    public void TriggerFall(float startDelay = 0f)
    {
        if (sequenceRunning) return;
        sequenceRunning = true;
        fallRoutine = StartCoroutine(FallSequence(startDelay));
    }

    // -----------------------------------------------------------------------
    bool IsLandingOnTop(Collision2D collision)
    {
        Collider2D playerCol = collision.collider;
        if (playerCol == null || box == null) return false;

        Bounds platformBounds = box.bounds;
        float  platformTop    = platformBounds.max.y;
        Bounds playerBounds   = playerCol.bounds;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D c = collision.GetContact(i);

            // Contact must be near the top surface.
            if (Mathf.Abs(c.point.y - platformTop) > topSurfaceSlop) continue;

            // Player's bottom must be at or above the surface (not punching through from below).
            if (playerBounds.min.y < platformTop - topSurfaceSlop * 2f) continue;

            return true;
        }

        return false;
    }

    // -----------------------------------------------------------------------
    IEnumerator FallSequence(float startDelay)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // Shake phase.
        float    elapsed = 0f;
        Vector3  basePos = transform.position;

        while (elapsed < fallDelay)
        {
            elapsed += Time.deltaTime;
            if (shakeIntensity > 0f)
            {
                Vector2 r = Random.insideUnitCircle * shakeIntensity;
                transform.position = basePos + new Vector3(r.x, r.y, 0f);
            }
            yield return null;
        }

        transform.position = basePos;

        // Drop.
        if (box != null) box.enabled = false;
        if (rb  != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.WakeUp(); }

        yield return new WaitForSeconds(destroyDelay);

        if (destroyGameObject)
        {
            if (respawnPrefab != null)
            {
                GameObject spawned = Instantiate(respawnPrefab, initialWorldPosition, initialWorldRotation, initialParent);
                spawned.transform.localScale = initialLocalScale;
            }
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        fallRoutine     = null;
        sequenceRunning = false;
    }

    // -----------------------------------------------------------------------
#if UNITY_EDITOR
    void OnValidate()
    {
        fallDelay    = Mathf.Max(0f, fallDelay);
        destroyDelay = Mathf.Max(0f, destroyDelay);
        blockIndex   = Mathf.Max(0,  blockIndex);
    }
#endif
}
