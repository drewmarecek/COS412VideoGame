using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 25; // Boosted for testing the Boss!

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 1. Try to damage the Boss first
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                return; // Exit if we hit the boss so we don't double-damage
            }

            // 2. If it's not a boss, try the regular EnemyHealth script
            EnemyHealth eHealth = other.GetComponent<EnemyHealth>();
            if (eHealth != null)
            {
                eHealth.TakeDamage(damage);
            }
        }
    }
}