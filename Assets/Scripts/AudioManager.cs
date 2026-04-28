using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    private class SoundEntry
    {
        public AudioClip clip;
        [Range(0f, 2f)] public float volume = 1f;
        public bool muted = false;
    }

    public static AudioManager Instance { get; private set; }

    [Header("Assign SFX clips here (clip + per-sfx volume/mute)")]
    [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

    [Header("Single AudioSource on this GameObject")]
    [SerializeField] private AudioSource audioSource;
    private Coroutine exclusivePlaybackRoutine;
    private readonly List<AudioSource> pausedSources = new List<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlaySound(string clipName)
    {
        if (!TryGetSoundEntry(clipName, out SoundEntry entry))
            return;

        if (entry.muted)
            return;

        // PlayOneShot layers SFX so currently playing sounds are not interrupted.
        audioSource.PlayOneShot(entry.clip, Mathf.Max(0f, entry.volume));
    }

    public void StopAllAudioAndPlayExclusive(string clipName)
    {
        if (!TryGetSoundEntry(clipName, out SoundEntry entry))
            return;
        if (exclusivePlaybackRoutine != null)
            StopCoroutine(exclusivePlaybackRoutine);
        RestorePausedSources();
        exclusivePlaybackRoutine = StartCoroutine(PlayExclusiveThenResumeRoutine(entry));
    }

    private bool TryGetSoundEntry(string clipName, out SoundEntry entry)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            Debug.LogWarning("AudioManager called with an empty clip name.");
            entry = null;
            return false;
        }

        entry = sounds.Find(s => s != null && s.clip != null && s.clip.name == clipName);
        if (entry == null)
        {
            Debug.LogWarning($"AudioManager could not find clip named '{clipName}'.");
            return false;
        }

        return true;
    }

    private System.Collections.IEnumerator PlayExclusiveThenResumeRoutine(SoundEntry entry)
    {
        pausedSources.Clear();

        AudioSource[] allSources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < allSources.Length; i++)
        {
            AudioSource src = allSources[i];
            if (src == null || src == audioSource) continue;
            if (!src.isPlaying) continue;
            src.Pause();
            pausedSources.Add(src);
        }

        if (audioSource != null)
            audioSource.Stop();

        float waitDuration = 0f;
        if (!entry.muted)
        {
            audioSource.PlayOneShot(entry.clip, Mathf.Max(0f, entry.volume));
            waitDuration = entry.clip != null ? entry.clip.length : 0f;
        }

        if (waitDuration > 0f)
            yield return new WaitForSecondsRealtime(waitDuration);

        RestorePausedSources();
        exclusivePlaybackRoutine = null;
    }

    private void RestorePausedSources()
    {
        for (int i = 0; i < pausedSources.Count; i++)
        {
            AudioSource src = pausedSources[i];
            if (src != null)
                src.UnPause();
        }
        pausedSources.Clear();
    }
}
