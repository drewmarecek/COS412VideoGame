using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    public Color activeColor = Color.green;
    public bool isReached = false;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player touched the checkpoint
        if (other.CompareTag("Player") && !isReached)
        {
            ActivateCheckpoint(other.gameObject);
        }
    }

    void ActivateCheckpoint(GameObject player)
    {
        isReached = true;

        // 1. Change color to show it's active
        if (sr != null) sr.color = activeColor;

        // 2. Update the Player's Spawn Point to THIS checkpoint
        PlayerHealth healthScript = player.GetComponent<PlayerHealth>();
        if (healthScript != null)
        {
            // We tell the health script to use this checkpoint's position now
            healthScript.SetNewRespawnPoint(transform.position);
            // 3. Replenish Health
            healthScript.health = healthScript.maxHealth;
            healthScript.UpdateUI();
        }
    }
}