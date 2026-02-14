using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damageAmount = 1;

    void Start()
    {
        // This tells Unity: "Wait 3 seconds, then delete this object."
        Destroy(gameObject, 3f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Ignore the Player so you don't shoot yourself
        if (other.CompareTag("Player")) return;

        // 2. Ignore Checkpoints so bullets pass through them
        if (other.CompareTag("Checkpoint")) return;

        // 3. Check for Enemy
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
            Destroy(gameObject); // Hit enemy, destroy bullet
            return;
        }

        // 4. If it hits anything else (Walls, Floor), destroy it
        Destroy(gameObject);
    }
}