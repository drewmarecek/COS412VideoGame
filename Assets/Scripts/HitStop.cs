using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    private bool isWaiting = false;

    public void Stop(float duration, int delayFrames = 20)
    {
        if (isWaiting) return;
        StartCoroutine(Wait(duration, delayFrames));
    }

    IEnumerator Wait(float duration, int delayFrames)
    {
        isWaiting = true;

        // 1. Wait for a specific number of frames
        // This allows the animation/knockback to move slightly before the freeze
        for (int i = 0; i < delayFrames; i++)
        {
            yield return null;
        }

        // 2. Store and Freeze
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 3. Wait for real-world seconds
        yield return new WaitForSecondsRealtime(duration);

        // 4. Resume
        Time.timeScale = originalTimeScale;
        isWaiting = false;
    }
}