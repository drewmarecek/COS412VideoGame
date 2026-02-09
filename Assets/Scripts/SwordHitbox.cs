using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Try to find the health script and damage it
            EnemyHealth eHealth = other.GetComponent<EnemyHealth>();
            if (eHealth != null)
            {
                eHealth.TakeDamage(damage);
            }
        }
    }
}