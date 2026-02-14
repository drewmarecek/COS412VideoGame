using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    public int attackDamage = 1; 
    public float attackRate = 10f; // Attacks per second
    float nextAttackTime = 0f;

    private WeaponManager weaponManager;
    private Animator anim;

    void Start()
    {
        weaponManager = GetComponent<WeaponManager>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Check for cooldown timer
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Only swing if the gun is NOT active
                if (weaponManager.gunObject != null && !weaponManager.gunObject.activeSelf)
                {
                    SwingSword();
                    // Set cooldown
                    nextAttackTime = Time.time + 1f / attackRate;
                }
            }
        }
    }

    void SwingSword()
    {
        // 1. Play animation
        anim.SetTrigger("Attack");

        // 2. Detect enemies in range of attack
        // This creates an invisible circle at your attack point
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // 3. Damage them
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    // This helps you see the attack circle in the Unity Editor
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}