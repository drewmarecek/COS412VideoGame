using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;    
    public Transform spawnPoint;      
    public bool spawnOnlyOnce = true; 
    private bool hasSpawned = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasSpawned)
        {
            if (enemyPrefab != null)
            {
                Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            if (spawnOnlyOnce) hasSpawned = true;
        }
    }
}