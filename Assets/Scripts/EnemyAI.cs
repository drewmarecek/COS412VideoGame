using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float retreatSpeed = 2f;
    public float retreatTime = 1.0f; // How long to back up after hitting

    private Transform player;
    private bool isRetreating = false;
    private float retreatTimer = 0f;
    private bool isFacingRight = true;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        if (isRetreating)
        {
            // Move AWAY from player
            transform.position = Vector2.MoveTowards(transform.position, player.position, -retreatSpeed * Time.deltaTime);
            
            retreatTimer -= Time.deltaTime;
            if (retreatTimer <= 0)
            {
                isRetreating = false;
            }
        }
        else
        {
            // Move TOWARDS player
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }

        // Face the player (visuals)
        if (transform.position.x < player.position.x && !isFacingRight) Flip();
        else if (transform.position.x > player.position.x && isFacingRight) Flip();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // Change this from 2 to 1
                health.TakeDamage(1); 
            }

            // Ensure this function is called so the enemy bounces back!
            StartRetreat();
        }
    }

    void StartRetreat()
    {
        isRetreating = true;
        retreatTimer = retreatTime;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }
}