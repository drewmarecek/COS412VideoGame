using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damageAmount = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Look for the EnemyHealth script on whatever we hit
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
            Debug.Log("Hit Enemy!");
        }

        // Destroy the bullet so it doesn't fly forever
        if (!other.CompareTag("Player")) // Don't destroy if hitting player
        {
            Destroy(gameObject);
        }
    }
}