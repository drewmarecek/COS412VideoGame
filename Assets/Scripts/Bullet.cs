using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public GameObject hitEffect; // Optional: Explosion particle

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Player")) return;

        // Change "Enemy" to "EnemyHealth" to match your script name
        EnemyHealth enemy = hitInfo.GetComponent<EnemyHealth>();
        
        if (enemy != null)
        {
            enemy.TakeDamage(1); // Deals 1 damage per bullet
        }

        Destroy(gameObject);
    }
}