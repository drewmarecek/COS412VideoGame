using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2.5f;
    public float attackRange = 2.2f;
    public int damageValue = 20;
    public float attackCooldown = 2.0f;
    public float deathDisappearDelay = 1.5f;

    [Header("References")]
    [Tooltip("Treasure chest that drops when boss dies.")]
    public GameObject treasureChestPrefab;
    public Transform attackPoint;
    public float attackRadius = 1.5f;
    public LayerMask playerLayer;

    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer[] sprites;

    private float health;
    private float nextAttackTime;
    private bool isDead = false;
    private bool isAttacking = false;
    public bool isActive = false;
    public bool IsDefeated { get; private set; }

    private Vector3 startPosition;
    private Vector3 startScale;
    private GameObject spawnedChest;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();

        startPosition = transform.position;
        startScale = transform.localScale;
        health = maxHealth;
    }

    void Update()
    {
        if (!isActive || isDead || player == null) return;

        FacePlayer();

        if (isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (Time.time >= nextAttackTime)
                StartAttack();
        }
        else
        {
            Move();
        }
    }

    void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (player.position.x > transform.position.x ? 1 : -1);
        transform.localScale = scale;
    }

    void Move()
    {
        anim.SetBool("isRunning", true);
        Vector2 target = new Vector2(player.position.x, rb.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void StartAttack()
    {
        isAttacking = true;
        anim.SetBool("isRunning", false);
        anim.SetTrigger("Attack" + Random.Range(1, 3));
        nextAttackTime = Time.time + attackCooldown;
    }

    // CALLED BY ANIMATION EVENT
    public void DealDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                TriggerPlayerHitEffects(playerHealth, damageValue);
                playerHealth.TakeDamage(damageValue);
            }
        }
    }

    public void ResetAttack() { isAttacking = false; }

    private void OnCollisionEnter2D(Collision2D collision) { TryDamagePlayer(collision.gameObject); }
    private void OnTriggerEnter2D(Collider2D other) { TryDamagePlayer(other.gameObject); }

    private void TryDamagePlayer(GameObject other)
    {
        if (isDead || !isActive) return;
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                TriggerPlayerHitEffects(playerHealth, damageValue);
                playerHealth.TakeDamage(damageValue);
            }
        }
    }

    void TriggerPlayerHitEffects(PlayerHealth playerHealth, int damage)
    {
        if (playerHealth.IsInvincible) return;
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

    public void TakeDamage(float damage)
    {
        if (isDead || !isActive) return;
        health -= damage;
        anim.SetTrigger("TakeHit");
        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        IsDefeated = true;
        anim.SetTrigger("Death");

        if (treasureChestPrefab != null)
            spawnedChest = Instantiate(treasureChestPrefab, transform.position, Quaternion.identity);

        rb.simulated = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        Color[] originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i].color;

        float elapsed = 0f;
        while (elapsed < deathDisappearDelay)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / deathDisappearDelay);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    Color c = originalColors[i];
                    c.a = originalColors[i].a * alpha;
                    sprites[i].color = c;
                }
            }
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void ResetBoss()
    {
        StopAllCoroutines();

        if (spawnedChest != null)
        {
            Destroy(spawnedChest);
            spawnedChest = null;
        }

        health = maxHealth;
        isDead = false;
        isAttacking = false;
        isActive = false;
        nextAttackTime = 0f;

        transform.position = startPosition;
        transform.localScale = startScale;

        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                Color c = sprites[i].color;
                c.a = 1f;
                sprites[i].color = c;
            }
        }

        anim.SetBool("isRunning", false);
        anim.Rebind();
        anim.Update(0f);

        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
