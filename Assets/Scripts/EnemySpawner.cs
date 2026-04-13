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
                Transform t = spawnPoint != null ? spawnPoint : transform;
                Instantiate(enemyPrefab, t.position, t.rotation);
            }
            if (spawnOnlyOnce) hasSpawned = true;
        }
    }
}