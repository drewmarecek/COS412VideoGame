using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;    // Drag your Enemy Prefab here
    public Transform spawnPoint;      // Drag the EnemySpawnPoint here
    public bool spawnOnlyOnce = true; // Prevents infinite enemies
    
    private bool hasSpawned = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the thing that touched the trigger is the Player
        if (other.CompareTag("Player") && !hasSpawned)
        {
            SpawnEnemy();
            
            if (spawnOnlyOnce)
            {
                hasSpawned = true;
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab != null && spawnPoint != null)
        {
            // Create the enemy at the spawn point's position and rotation
            Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // Optional: Add a small puff of smoke or sound here later
            Debug.Log("Enemy spawned");
        }
    }
}