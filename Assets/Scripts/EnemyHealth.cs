using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    private EnemyAI enemyAI;
    private SkeletonAI skeletonAI;

    void Start()
    {
        currentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
        skeletonAI = GetComponent<SkeletonAI>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (enemyAI != null)
            enemyAI.ApplyKnockback();
        else if (skeletonAI != null)
            skeletonAI.ApplyKnockback();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("TakeHit");
    }

    void Die()
    {
        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null) hitStop.Stop(0.2f, 12);

        if (skeletonAI != null)
        {
            skeletonAI.Die();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}