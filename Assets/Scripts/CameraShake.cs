using UnityEngine;

/// <summary>
/// Handles screen shake for hits and kills. Add to the Main Camera.
/// Light shake = hit. Heavy shake = kill.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Presets")]
    [Tooltip("Intensity and duration for a normal hit")]
    public float hitIntensity = 0.08f;
    public float hitDuration = 0.12f;

    [Tooltip("Intensity and duration for a kill (noticeably bigger than hits)")]
    public float killIntensity = 0.32f;
    public float killDuration = 0.4f;

    private float shakeRemaining;
    private float shakeIntensity;
    private float shakeDuration;
    private Vector3 shakeOffset;

    /// <summary>Light shake when the player hits something.</summary>
    public void ShakeHit()
    {
        Shake(hitIntensity, hitDuration);
    }

    /// <summary>Heavier shake when something is killed.</summary>
    public void ShakeKill()
    {
        Shake(killIntensity, killDuration);
    }

    /// <summary>Custom shake with intensity and duration.</summary>
    public void Shake(float intensity, float duration)
    {
        // If a stronger shake is requested, override
        if (intensity > shakeIntensity || shakeRemaining <= 0f)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeRemaining = duration;
        }
    }

    void Update()
    {
        if (shakeRemaining <= 0f)
        {
            shakeOffset = Vector3.zero;
            return;
        }

        float progress = 1f - (shakeRemaining / shakeDuration);
        float decay = 1f - progress * progress; // Quadratic decay
        float currentMagnitude = shakeIntensity * decay;

        shakeOffset = new Vector3(
            (Mathf.PerlinNoise(Time.time * 50f, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0f, Time.time * 50f) - 0.5f) * 2f,
            0f
        ).normalized * currentMagnitude;

        shakeRemaining -= Time.deltaTime;
    }

    /// <summary>Returns the current shake offset to add to camera position.</summary>
    public Vector3 GetShakeOffset()
    {
        return shakeOffset;
    }
}
