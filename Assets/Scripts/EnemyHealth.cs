using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    // We need the AI script to tell it to stop moving when hit
    private EnemyAI enemyAI;

    void Start()
    {
        currentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // 1. Play Hurt Animation (Optional)
        // GetComponent<Animator>().SetTrigger("Hit");

        // 2. Apply Knockback (We will add this function to EnemyAI next)
        if (enemyAI != null)
        {
            enemyAI.ApplyKnockback();
        }

        // 3. Check for Death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Disable the enemy so it stops moving/attacking
        // (You can also play a death anim here before destroying)
        Destroy(gameObject);
    }
}