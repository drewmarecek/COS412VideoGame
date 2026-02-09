using UnityEngine;

public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player fell into the zone
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // In The Glitch Protocol, falling into the void is an instant respawn!
                health.Respawn(); 
            }
        }
    }
}