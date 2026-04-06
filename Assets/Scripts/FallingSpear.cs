using UnityEngine;

/// <summary>
/// Spear hangs from the ceiling until the player enters a detection box below it, then falls.
/// After release, a hit on the player deals damage once per contact window (PlayerHealth i-frames apply).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingSpear : MonoBehaviour
{
    [Header("Detection (world-space box under the spear)")]
    [Tooltip("Center of the box in world space. If null, uses this transform's position.")]
    public Transform detectionCenter;

    [Tooltip("Full width and height of the detection area.")]
    public Vector2 detectionSize = new Vector2(4f, 0.75f);

    [Tooltip("Which layers the detection box tests. Must include the layer your Player collider is on. Default Everything is fine.")]
    public LayerMask detectionLayers = ~0;

    [Header("Fall")]
    [Tooltip("Rigidbody2D gravity scale after the spear is released.")]
    public float fallGravityScale = 1f;

    [Header("Damage")]
    public int damage = 1;

    Rigidbody2D rb;
    bool released;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void FixedUpdate()
    {
        if (released) return;

        Vector2 center = detectionCenter != null ? (Vector2)detectionCenter.position : (Vector2)transform.position;
        // OverlapBox returns only ONE collider — often ground/tiles, not the player. Check all overlaps.
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, detectionSize, 0f, detectionLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag("Player"))
            {
                Release();
                break;
            }
        }
    }

    void Release()
    {
        if (released) return;
        released = true;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    void TryDamage(GameObject other)
    {
        if (!released) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Vector3 center = detectionCenter != null ? detectionCenter.position : transform.position;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(center, new Vector3(detectionSize.x, detectionSize.y, 0f));
    }
}
