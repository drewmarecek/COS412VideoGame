using UnityEngine;
using System.Collections;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 25; // Boosted for testing the Boss!
    [Header("Hit VFX")]
    [Tooltip("Assign your slash effect child object here.")]
    public GameObject slashEffect;
    public float slashEffectDuration = 0.08f;
    private Coroutine slashRoutine;

    private void Awake()
    {
        if (slashEffect != null)
            slashEffect.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            bool didHit = false;

            // 1. Try to damage the Boss first
            BossController boss = other.GetComponentInParent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                didHit = true;
            }
            else
            {
                // 1b. Boss2 support
                boss2Script boss2 = other.GetComponentInParent<boss2Script>();
                if (boss2 != null)
                {
                    boss2.TakeDamage(damage);
                    didHit = true;
                }
            }

            if (!didHit)
            {
                // 2. If it's not a boss, try the regular EnemyHealth script
                EnemyHealth eHealth = other.GetComponentInParent<EnemyHealth>();
                if (eHealth != null)
                {
                    eHealth.TakeDamage(damage);
                    didHit = true;
                }
            }

            if (didHit)
            {
                CameraShake camShake = FindFirstObjectByType<CameraShake>();
                if (camShake != null) camShake.ShakeHit();
                HitStop hitStop = FindFirstObjectByType<HitStop>();
                if (hitStop != null) hitStop.Stop(0.07f, 45);
                PlaySlashEffect();
            }
        }
    }

    private void PlaySlashEffect()
    {
        if (slashEffect == null) return;

        if (slashRoutine != null)
            StopCoroutine(slashRoutine);

        slashRoutine = StartCoroutine(ShowSlashEffectBriefly());
    }

    private IEnumerator ShowSlashEffectBriefly()
    {
        slashEffect.SetActive(true);
        yield return new WaitForSeconds(slashEffectDuration);
        if (slashEffect != null) slashEffect.SetActive(false);
        slashRoutine = null;
    }
}