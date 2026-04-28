using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 1;
    public float hitEffectsDelay = 0.1f;

    [Header("Player Hit Shake Scaling (Boss Fights)")]
    [Tooltip("Shake intensity for player hit when player is at full health.")]
    public float playerHitShakeMinIntensity = 0.08f;
    [Tooltip("Shake intensity for player hit when player is near death (non-fatal).")]
    public float playerHitShakeMaxIntensity = 0.28f;
    [Tooltip("Shake duration for player hit when player is at full health.")]
    public float playerHitShakeMinDuration = 0.12f;
    [Tooltip("Shake duration for player hit when player is near death (non-fatal).")]
    public float playerHitShakeMaxDuration = 0.3f;

    [Header("Player Hit Pause (Boss Fights)")]
    [Tooltip("Hit-stop duration when the player is hit by a boss (non-fatal).")]
    public float playerHitPauseDuration = 0.12f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Covers cases where a hitbox is enabled while already overlapping the player.
        TryDamagePlayer(other.gameObject);
    }

    void TryDamagePlayer(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (!playerHealth.IsInvincible)
                {
                    bool willDie = playerHealth.health <= damage;
                    StartCoroutine(PlayHitEffectsDelayed(willDie));
                }
                playerHealth.TakeDamage(damage);
            }
        }
    }

    private IEnumerator PlayHitEffectsDelayed(bool willDie)
    {
        if (hitEffectsDelay > 0f)
            yield return new WaitForSeconds(hitEffectsDelay);

        bool isBossAttacker = IsBossAttacker();

        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null)
        {
            if (willDie)
            {
                camShake.ShakeKill();
            }
            else if (isBossAttacker)
            {
                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                float hpRatio = (ph != null && ph.maxHealth > 0) ? Mathf.Clamp01((float)ph.health / ph.maxHealth) : 1f;
                float lowFactor = 1f - hpRatio;
                float intensity = Mathf.Lerp(playerHitShakeMinIntensity, playerHitShakeMaxIntensity, lowFactor);
                float duration = Mathf.Lerp(playerHitShakeMinDuration, playerHitShakeMaxDuration, lowFactor);
                camShake.Shake(intensity, duration);
            }
            else
            {
                camShake.ShakeHit();
            }
        }

        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null)
        {
            if (willDie) hitStop.Stop(0.25f, 15);
            else if (isBossAttacker) hitStop.Stop(playerHitPauseDuration, 45);
            else hitStop.Stop(0.07f, 45);
        }

        if (willDie)
        {
            SamuraiDeathVFX deathVfx = GetComponentInParent<SamuraiDeathVFX>();
            if (deathVfx == null) yield break;
            deathVfx.TriggerDeathFlash(true);
        }
    }

    private bool IsBossAttacker()
    {
        if (GetComponentInParent<BossController>() != null) return true;
        if (GetComponentInParent<boss2Script>() != null) return true;
        if (GetComponentInParent<SamuraiDeathVFX>() != null) return true;
        return false;
    }
}
