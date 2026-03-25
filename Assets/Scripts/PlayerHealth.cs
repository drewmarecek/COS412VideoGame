using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public int health = 3;
    public int maxHealth = 3;

    [Header("UI References")]
    public GameObject[] hearts;

    [Header("Invincibility")]
    public float iFrameDuration = 0.5f;
    private float iFrameTimer;
    public bool IsInvincible => iFrameTimer > 0;

    [Header("Death")]
    [Tooltip("Pause before respawning when the player dies")]
    public float respawnDelay = 0.4f;

    private Vector3 currentRespawnPoint;
    private Rigidbody2D rb;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        health = maxHealth;
        currentRespawnPoint = transform.position;
        
        UpdateUI();
    }

    void Update()
    {
        // Countdown the invincibility timer
        if (iFrameTimer > 0)
        {
            iFrameTimer -= Time.deltaTime;
        }
    }

    // This is the ONLY TakeDamage function you need!
    public void TakeDamage(int amount)
    {
        // 1. Check if we are currently invincible
        if (iFrameTimer > 0) return;

        // 2. Subtract health
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateUI();

        // 3. Play the TakeHit animation
        if (anim != null)
        {
            anim.SetTrigger("TakeHit");
        }

        // 4. Start invincibility so we don't get double-hit
        iFrameTimer = iFrameDuration;

        // 5. Check for death
        if (health <= 0)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    public void UpdateUI()
    {
        if (hearts == null || hearts.Length == 0) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < health);
        }
    }

    public void SetNewRespawnPoint(Vector3 newPosition)
    {
        currentRespawnPoint = newPosition;
    }

    /// <summary>Call when the player dies (e.g. fell in KillZone) to respawn after a pause.</summary>
    public void RespawnWithDelay()
    {
        health = 0;
        UpdateUI();
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        // Disable physics so the player doesn't fall or get hit during the pause
        if (rb != null) rb.simulated = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        if (col != null) col.enabled = true;
        if (rb != null) rb.simulated = true;
        RespawnImmediate();
    }

    void RespawnImmediate()
    {
        // 1. Move the player
        transform.position = currentRespawnPoint;

        // 2. Stop any falling/sliding movement
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // 3. Refill health to max
        health = maxHealth;

        // 4. Give the player 1 full second of invincibility on respawn
        iFrameTimer = 1.0f;

        UpdateUI();
        Debug.Log("Player Respawned with full health and grace period.");
    }
}