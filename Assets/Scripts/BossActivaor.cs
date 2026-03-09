using UnityEngine;

public class BossActivator : MonoBehaviour
{
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            // Use the faster method we discussed earlier
            BossController boss = Object.FindFirstObjectByType<BossController>();
            
            if (boss != null)
            {
                boss.isActive = true;
                Debug.Log("Boss Aggro Triggered!");
            }

            if (triggerOnlyOnce) hasTriggered = true;
        }
    }
}