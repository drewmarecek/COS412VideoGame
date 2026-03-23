using UnityEngine;
using System.Collections;

/// <summary>
/// Treasure chest that drops from the boss. Falls with gravity, then when the player touches it,
/// it opens and spawns the gun for the player to pick up.
/// Requires: Rigidbody2D (added automatically if missing), Collider2D (non-trigger so it lands on ground).
/// </summary>
public class TreasureChest : MonoBehaviour
{
    [Tooltip("The gun pickup prefab to spawn when the chest opens")]
    public GameObject gunPickupPrefab;

    [Tooltip("How far the gun pops up from the chest")]
    public float gunPopHeight = 0.8f;

    [Tooltip("How long the pop-out animation takes")]
    public float gunPopDuration = 0.35f;

    [Tooltip("Delay after opening animation starts before spawning the gun")]
    public float gunSpawnDelay = 0.4f;

    [Tooltip("Animator trigger name for opening (default: Open)")]
    public string openTriggerName = "Open";

    private Animator anim;
    private Collider2D col;
    private bool isOpened;

    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        // Ensure the chest starts in its closed state
        if (anim != null)
        {
            anim.SetBool(openTriggerName, false);
        }

        // Add Rigidbody2D if missing so the chest falls with gravity
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        // Ensure collider is NOT a trigger so the chest lands on the ground
        if (col != null && col.isTrigger)
        {
            col.isTrigger = false;
        }
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

        Open();
    }

    void Open()
    {
        if (isOpened) return;
        isOpened = true;

        // Stop physics so chest stays in place (doesn't fall or disappear)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (col != null) col.enabled = false; // Prevent re-triggering (safe now - physics stopped)

        // Play open animation
        if (anim != null)
        {
            anim.SetBool(openTriggerName, true);
        }

        // Spawn gun after a short delay so it comes out of the chest
        StartCoroutine(SpawnGunAfterDelay());
    }

    IEnumerator SpawnGunAfterDelay()
    {
        yield return new WaitForSeconds(gunSpawnDelay);

        if (gunPickupPrefab != null)
        {
            Vector3 chestPos = transform.position;
            Vector3 spawnPos = chestPos;
            GameObject gun = Instantiate(gunPickupPrefab, spawnPos, Quaternion.identity);

            GunPickup gunPickup = gun.GetComponent<GunPickup>();
            if (gunPickup != null)
            {
                gunPickup.enabled = false;
            }

            StartCoroutine(PopGunOut(gun, chestPos, gunPickup));
        }
        else
        {
            Debug.LogWarning("TreasureChest: gunPickupPrefab is not assigned! Assign it in the Inspector.");
        }
    }

    IEnumerator PopGunOut(GameObject gun, Vector3 chestPos, GunPickup gunPickup)
    {
        Vector3 startPos = chestPos;
        Vector3 endPos = chestPos + Vector3.up * gunPopHeight;
        float elapsed = 0f;

        while (elapsed < gunPopDuration)
        {
            if (gun == null) yield break; // Player picked it up during pop
            elapsed += Time.deltaTime;
            float t = elapsed / gunPopDuration;
            t = 1f - (1f - t) * (1f - t); // Ease-out quad - fast start, slow at top
            gun.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        if (gun == null) yield break;
        gun.transform.position = endPos;

        if (gunPickup != null)
        {
            gunPickup.SetHoverPosition(endPos);
            gunPickup.enabled = true;
        }
    }
}
