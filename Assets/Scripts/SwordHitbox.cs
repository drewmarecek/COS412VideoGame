using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordHitbox : MonoBehaviour
{
    [Tooltip("Damage per hit. Tune in Inspector; bosses often use higher values than regular enemies.")]
    public int damage = 1;

    [Header("Hit VFX")]
    [Tooltip("Assign your slash effect child object here.")]
    public GameObject slashEffect;
    public float slashEffectDuration = 0.08f;
    [Tooltip("Random Z rotation each hit (degrees). Set max to 360 for full spin.")]
    public Vector2 randomSlashRotationZ = new Vector2(0f, 360f);

    private Coroutine slashRoutine;
    private Collider2D hitCollider;

    /// <summary>
    /// One swing can only damage each enemy root once. Cleared when hitbox turns on again.
    /// </summary>
    private readonly HashSet<int> hitRootsThisSwing = new HashSet<int>();

    private bool wasColliderEnabled;

    private void Awake()
    {
        hitCollider = GetComponent<Collider2D>();
        if (slashEffect != null)
            slashEffect.SetActive(false);
        if (hitCollider != null)
            wasColliderEnabled = hitCollider.enabled;
    }

    private void LateUpdate()
    {
        if (hitCollider == null) return;

        // New swing: collider was off, now on — allow hits again
        if (hitCollider.enabled && !wasColliderEnabled)
            hitRootsThisSwing.Clear();

        wasColliderEnabled = hitCollider.enabled;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryProcessHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Enter does not fire if hitbox turns on while already overlapping the enemy.
        // Stay catches that case; dedupe prevents multi-hit per swing.
        TryProcessHit(other);
    }

    private void TryProcessHit(Collider2D other)
    {
        if (hitCollider == null || !hitCollider.enabled) return;
        if (!other.CompareTag("Enemy")) return;

        Transform root = other.transform.root;
        int rootId = root.GetInstanceID();
        if (hitRootsThisSwing.Contains(rootId))
            return;

        bool didHit = false;

        BossController boss = other.GetComponentInParent<BossController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            didHit = true;
        }
        else
        {
            boss2Script boss2 = other.GetComponentInParent<boss2Script>();
            if (boss2 != null)
            {
                if (boss2.TakeDamage(damage, boss2Script.Boss2DamageSource.Melee))
                    didHit = true;
            }
        }

        if (!didHit)
        {
            EnemyHealth eHealth = other.GetComponentInParent<EnemyHealth>();
            if (eHealth != null)
            {
                eHealth.TakeDamage(damage);
                didHit = true;
            }
        }

        if (!didHit) return;

        hitRootsThisSwing.Add(rootId);

        CameraShake camShake = FindFirstObjectByType<CameraShake>();
        if (camShake != null) camShake.ShakeHit();
        HitStop hitStop = FindFirstObjectByType<HitStop>();
        if (hitStop != null) hitStop.Stop(0.07f, 45);
        PlaySlashEffect();
    }

    private void PlaySlashEffect()
    {
        if (slashEffect == null) return;

        float z = Random.Range(randomSlashRotationZ.x, randomSlashRotationZ.y);
        slashEffect.transform.localRotation = Quaternion.Euler(0f, 0f, z);

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
