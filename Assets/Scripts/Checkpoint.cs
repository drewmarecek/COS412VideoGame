using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    public Color activeColor = Color.green;
    public bool isReached = false;

    [Header("Grouping")]
    [Tooltip("Optional. Checkpoints sharing the same non-empty groupId activate together. " +
             "If left empty, every Checkpoint sharing the same parent transform is treated as one group.")]
    public string groupId = "";

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isReached)
        {
            ActivateCheckpoint(other.gameObject);
        }
    }

    void ActivateCheckpoint(GameObject player)
    {
        MarkReachedVisual();

        PlayerHealth healthScript = player.GetComponent<PlayerHealth>();
        if (healthScript == null)
            healthScript = player.GetComponentInParent<PlayerHealth>();
        if (healthScript != null)
        {
            healthScript.SetNewRespawnPoint(transform.position);
            healthScript.health = healthScript.maxHealth;
            healthScript.UpdateUI();
        }

        // Propagate the visual change to every other block in the same checkpoint group.
        Checkpoint[] all = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Checkpoint other = all[i];
            if (other == null || other == this || other.isReached) continue;
            if (!IsSameGroup(other)) continue;
            other.MarkReachedVisual();
        }
    }

    bool IsSameGroup(Checkpoint other)
    {
        // Explicit groupId always wins.
        if (!string.IsNullOrEmpty(groupId) || !string.IsNullOrEmpty(other.groupId))
            return groupId == other.groupId;

        // Fallback: siblings under the same parent are treated as one checkpoint.
        return transform.parent != null && transform.parent == other.transform.parent;
    }

    /// <summary>Flip this block to its reached state without re-binding the spawn point.</summary>
    public void MarkReachedVisual()
    {
        isReached = true;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = activeColor;
    }
}
