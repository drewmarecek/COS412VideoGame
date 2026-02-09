using UnityEngine;
using UnityEngine.UI;

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
            Respawn();
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

    public void Respawn()
    {
        // 1. Move the player
        transform.position = currentRespawnPoint;

        // 2. Stop any falling/sliding movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 3. Refill health to max
        health = maxHealth;
        
        // 4. THE FIX: Give the player 1 full second of invincibility on respawn
        // This prevents the enemy from hitting you before you can move!
        iFrameTimer = 1.0f; 

        UpdateUI();
        Debug.Log("Player Respawned with full health and grace period.");
    }
}