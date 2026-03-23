using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void TryDamagePlayer(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (!playerHealth.IsInvincible)
                {
                    bool willDie = playerHealth.health <= damage;
                    CameraShake camShake = FindFirstObjectByType<CameraShake>();
                    if (camShake != null)
                    {
                        if (willDie) camShake.ShakeKill();
                        else camShake.ShakeHit();
                    }
                    HitStop hitStop = FindFirstObjectByType<HitStop>();
                    if (hitStop != null)
                    {
                        if (willDie) hitStop.Stop(0.25f, 15);
                        else hitStop.Stop(0.07f, 45);
                    }
                }
                playerHealth.TakeDamage(damage);
            }
        }
    }
}
