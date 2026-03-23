using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 25; // Boosted for testing the Boss!

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            bool didHit = false;

            // 1. Try to damage the Boss first
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                didHit = true;
            }
            else
            {
                // 2. If it's not a boss, try the regular EnemyHealth script
                EnemyHealth eHealth = other.GetComponent<EnemyHealth>();
                if (eHealth != null)
                {
                    eHealth.TakeDamage(damage);
                    didHit = true;
                }
            }

            if (didHit)
            {
                CameraShake camShake = FindFirstObjectByType<CameraShake>();
                if (camShake != null) camShake.ShakeHit();
                HitStop hitStop = FindFirstObjectByType<HitStop>();
                if (hitStop != null) hitStop.Stop(0.07f, 45);
            }
        }
    }
}