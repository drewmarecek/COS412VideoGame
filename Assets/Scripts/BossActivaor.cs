using UnityEngine;

public class BossActivator : MonoBehaviour
{
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            BossController boss = FindFirstObjectByType<BossController>();

            if (boss != null)
                boss.isActive = true;

            if (triggerOnlyOnce) hasTriggered = true;
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
