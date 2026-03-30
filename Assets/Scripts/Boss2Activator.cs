using UnityEngine;

public class Boss2Activator : MonoBehaviour
{
    [SerializeField] private boss2Script boss2;
    [SerializeField] private bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    private void Awake()
    {
        if (boss2 == null)
            boss2 = FindFirstObjectByType<boss2Script>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;
        if (boss2 != null)
            boss2.ActivateBoss();

        if (triggerOnlyOnce)
            hasTriggered = true;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
