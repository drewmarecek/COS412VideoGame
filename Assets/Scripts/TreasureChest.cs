using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Tooltip("Delay after opening animation before giving the gun")]
    public float gunUnlockDelay = 0.5f;

    [Tooltip("Animator bool name for opening (default: Open)")]
    public string openTriggerName = "Open";

    // Not used anymore but kept so the BossController inspector reference doesn't break
    [HideInInspector] public GameObject gunPickupPrefab;

    private Animator anim;
    private Collider2D col;
    private bool isOpened;

    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (anim != null)
            anim.SetBool(openTriggerName, false);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        if (col != null && col.isTrigger)
            col.isTrigger = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryOpen(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryOpen(collision.gameObject);
    }

    void TryOpen(GameObject other)
    {
        if (isOpened) return;
        if (!other.CompareTag("Player")) return;

        Open(other);
    }

    void Open(GameObject player)
    {
        if (isOpened) return;
        isOpened = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (col != null) col.enabled = false;

        if (anim != null)
            anim.SetBool(openTriggerName, true);

        WeaponManager manager = player.GetComponent<WeaponManager>();
        if (manager != null)
        {
            StartCoroutine(UnlockGunAfterDelay(manager));
        }
    }

    System.Collections.IEnumerator UnlockGunAfterDelay(WeaponManager manager)
    {
        yield return new WaitForSeconds(gunUnlockDelay);
        manager.UnlockGun();
    }
}
