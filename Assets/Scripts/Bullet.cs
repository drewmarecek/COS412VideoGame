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

        // 3. Check for bosses first (supports child colliders)
        BossController boss1 = other.GetComponentInParent<BossController>();
        if (boss1 != null)
        {
            boss1.TakeDamage(damageAmount);
            HitStop hitStop = FindFirstObjectByType<HitStop>();
            if (hitStop != null) hitStop.Stop(0.035f, 20);
            Destroy(gameObject);
            return;
        }

        boss2Script boss2 = other.GetComponentInParent<boss2Script>();
        if (boss2 != null)
        {
            boss2.TakeDamage(damageAmount);
            HitStop hitStop = FindFirstObjectByType<HitStop>();
            if (hitStop != null) hitStop.Stop(0.035f, 20);
            Destroy(gameObject);
            return;
        }

        // 4. Check regular enemies (supports child colliders)
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
            HitStop hitStop = FindFirstObjectByType<HitStop>();
            if (hitStop != null) hitStop.Stop(0.035f, 20);
            Destroy(gameObject); // Hit enemy, destroy bullet
            return;
        }

        // 5. If it hits anything else (Walls, Floor), destroy it
        Destroy(gameObject);
    }
}