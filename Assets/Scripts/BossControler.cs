using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float moveSpeed = 2.5f;
    public float attackRange = 2.2f;
    public int damageValue = 20;
    public float attackCooldown = 2.0f;
    public float nextAttackTime;

    [Header("References")]
    [Tooltip("Treasure chest that drops when boss dies. Assign a prefab with TreasureChest script + chest sprite/animator.")]
    public GameObject treasureChestPrefab;
    [Tooltip("Gun pickup that appears when the chest opens. Assign the same prefab you used before.")]
    public GameObject gunPickupPrefab;
    [Tooltip("How long the death animation plays before the boss disappears")]
    public float deathDisappearDelay = 1.5f;
    public Transform attackPoint; // Create an empty child on the boss's hand and drag it here
    public float attackRadius = 1.5f;
    public LayerMask playerLayer;

    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;
    
    private bool isDead = false;
    private bool isAttacking = false;
    public bool isActive = false; // The Trigger will set this to true

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        if (!isActive || isDead || player == null) return;

        // Always face the player when active
        FacePlayer();

        if (isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange) {
            // Only attack if we are NOT currently attacking and cooldown is up
            if (Time.time >= nextAttackTime) {
                StartAttack();
            }
        } else {
            Move();
        }
    }

    void FacePlayer() {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (player.position.x > transform.position.x ? 1 : -1);
        transform.localScale = scale;
    }

    void Move() {
        anim.SetBool("isRunning", true);
        Vector2 target = new Vector2(player.position.x, rb.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void StartAttack() {
        isAttacking = true;
        anim.SetBool("isRunning", false);

        anim.SetTrigger("Attack" + Random.Range(1, 3));
        nextAttackTime = Time.time + attackCooldown;
    }

    // CALLED BY ANIMATION EVENT
    public void DealDamage() {
        // This acts as the "Hitbox"
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null) {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null) {
                TriggerPlayerHitEffects(playerHealth, damageValue);
                playerHealth.TakeDamage(damageValue);
            }
        }
    }

    public void ResetAttack() { isAttacking = false; }

    // Contact damage - player takes damage when touching the boss
    private void OnCollisionEnter2D(Collision2D collision) {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        TryDamagePlayer(other.gameObject);
    }

    private void TryDamagePlayer(GameObject other) {
        if (isDead || !isActive) return;
        if (other.CompareTag("Player")) {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null) {
                TriggerPlayerHitEffects(playerHealth, damageValue);
                playerHealth.TakeDamage(damageValue);
            }
        }
    }

    void TriggerPlayerHitEffects(PlayerHealth playerHealth, int damage) {
        if (playerHealth.IsInvincible) return;
        bool willDie = playerHealth.health <= damage;
        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null) {
            if (willDie) camShake.ShakeKill();
            else camShake.ShakeHit();
        }
        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null) {
            if (willDie) hitStop.Stop(0.25f, 15);
            else hitStop.Stop(0.07f, 45);
        }
    }

    public void TakeDamage(float damage) {
        if (isDead || !isActive) return;
        health -= damage;
        anim.SetTrigger("TakeHit");
        if (health <= 0) Die();
    }

    void Die() {
        isDead = true;
        anim.SetTrigger("Death");

        // Spawn treasure chest where the boss died (it will fall with gravity)
        if (treasureChestPrefab != null) {
            Vector3 spawnPos = transform.position;
            GameObject chest = Instantiate(treasureChestPrefab, spawnPos, Quaternion.identity);
            TreasureChest tc = chest.GetComponent<TreasureChest>();
            if (tc != null && gunPickupPrefab != null) {
                tc.gunPickupPrefab = gunPickupPrefab;
            }
        } else {
            Debug.LogWarning("BossController: treasureChestPrefab is not assigned! Assign it in the Inspector for the chest to drop.");
        }

        rb.simulated = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) {
            col.enabled = false;
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy() {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        Color[] originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) {
            originalColors[i] = sprites[i].color;
        }

        float elapsed = 0f;
        while (elapsed < deathDisappearDelay) {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / deathDisappearDelay);
            for (int i = 0; i < sprites.Length; i++) {
                if (sprites[i] != null) {
                    Color c = originalColors[i];
                    c.a = originalColors[i].a * alpha;
                    sprites[i].color = c;
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    // Visualizes the hitbox in the editor
    private void OnDrawGizmosSelected() {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}