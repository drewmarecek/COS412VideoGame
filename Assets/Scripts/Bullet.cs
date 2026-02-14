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
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
            Destroy(gameObject); // Destroy immediately on hit
        }
        else if (!other.CompareTag("Player"))
        {
            Destroy(gameObject); // Destroy if it hits a wall/floor
        }
    }
}