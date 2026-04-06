using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When the player enters this trigger, drops a barrier behind them.
///
/// Put all block objects as <b>direct children</b> of bossGate, assign that parent to
/// <see cref="existingBlocksParent"/>. First visit: blocks move up at start, then fall when triggered.
/// PlayerPrefs remembers the seal in builds. In the Editor, persistence is off by default so every
/// Play Mode run lifts the wall again (enable <see cref="persistSealInEditor"/> to test saves in Editor).
/// </summary>
public class BossArenaCheckpointSeal : MonoBehaviour
{
    [Header("Persistence")]
    [Tooltip("Unique key per barrier. Empty = no save anywhere.")]
    [SerializeField] string playerPrefsKey = "Level2_PreBossBarrierSealed";

    [Tooltip("If off (default), the Editor ignores PlayerPrefs — wall lifts every time you press Play. Builds always save/load the seal.")]
    [SerializeField] bool persistSealInEditor = false;

    [Header("Blocks — easy mode")]
    [Tooltip("Drag bossGate. Each direct child = one block.")]
    [SerializeField] Transform existingBlocksParent;

    [Header("Blocks — prefab mode (optional)")]
    [Tooltip("Leave empty if you use Existing Blocks Parent above.")]
    [SerializeField] GameObject blockPrefab;
    [SerializeField] Transform[] blockAnchors;

    [Header("Fall")]
    [SerializeField] float spawnHeightAboveAnchor = 9f;
    [SerializeField] float fallDuration = 0.55f;
    [SerializeField] AnimationCurve fallEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Optional (prefab mode only)")]
    [SerializeField] Transform blocksParent;

    readonly List<GameObject> spawnedInstances = new List<GameObject>();

    List<Transform> gateBlockTransforms;
    Vector3[] gateBlockEndWorldPos;
    Rigidbody2D[] gateBlockRigidbodies;
    RigidbodyType2D[] gateBlockOriginalBodyTypes;
    float[] gateBlockOriginalGravity;
    bool gateFallFinished;
    bool gateReady;

    bool UseGateMode => existingBlocksParent != null;

    void Awake()
    {
        if (UseGateMode)
        {
            CacheGateChildren();
            if (gateBlockTransforms.Count == 0)
            {
                Debug.LogWarning($"BossArenaCheckpointSeal on '{name}': bossGate has no direct children, or parent not assigned.", this);
                return;
            }

            if (IsPersistedSealed())
            {
                gateFallFinished = true;
                return;
            }

            CacheRigidbodyStates();
            gateReady = true;
            return;
        }

        if (blocksParent == null)
        {
            var go = new GameObject("BossArenaBarrierBlocks");
            go.transform.SetParent(transform, false);
            blocksParent = go.transform;
        }

        if (IsPersistedSealed())
            SpawnBarrierAtRest();
    }

    void Start()
    {
        if (!UseGateMode || !gateReady || gateFallFinished) return;

        LiftGateBlocksToSky();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (UseGateMode)
        {
            if (IsPersistedSealed() || gateFallFinished) return;
            if (gateBlockTransforms == null || gateBlockTransforms.Count == 0) return;

            PersistSeal();
            StartCoroutine(FallGateBlocksIn());
            return;
        }

        if (IsPersistedSealed() || spawnedInstances.Count > 0) return;

        PersistSeal();
        StartCoroutine(FallBlocksIn());
    }

    bool UseDiskPersistence()
    {
        if (string.IsNullOrEmpty(playerPrefsKey)) return false;
        if (!Application.isEditor) return true;
        return persistSealInEditor;
    }

    bool IsPersistedSealed()
    {
        if (!UseDiskPersistence()) return false;
        return PlayerPrefs.GetInt(playerPrefsKey, 0) == 1;
    }

    void PersistSeal()
    {
        if (!UseDiskPersistence()) return;
        PlayerPrefs.SetInt(playerPrefsKey, 1);
        PlayerPrefs.Save();
    }

    [ContextMenu("Clear Seal Save For Testing")]
    void ClearSealSaveForTesting()
    {
        if (string.IsNullOrEmpty(playerPrefsKey))
        {
            Debug.Log("No playerPrefsKey set.", this);
            return;
        }
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();
        Debug.Log($"Deleted PlayerPrefs key '{playerPrefsKey}'. Enter Play again to see blocks lift.", this);
    }

    void CacheGateChildren()
    {
        gateBlockTransforms = new List<Transform>();
        for (int i = 0; i < existingBlocksParent.childCount; i++)
        {
            Transform child = existingBlocksParent.GetChild(i);
            if (child != null)
                gateBlockTransforms.Add(child);
        }

        gateBlockEndWorldPos = new Vector3[gateBlockTransforms.Count];
        for (int i = 0; i < gateBlockTransforms.Count; i++)
            gateBlockEndWorldPos[i] = gateBlockTransforms[i].position;
    }

    void CacheRigidbodyStates()
    {
        int n = gateBlockTransforms.Count;
        gateBlockRigidbodies = new Rigidbody2D[n];
        gateBlockOriginalBodyTypes = new RigidbodyType2D[n];
        gateBlockOriginalGravity = new float[n];
        for (int i = 0; i < n; i++)
        {
            Rigidbody2D rb = gateBlockTransforms[i] != null ? gateBlockTransforms[i].GetComponent<Rigidbody2D>() : null;
            gateBlockRigidbodies[i] = rb;
            if (rb != null)
            {
                gateBlockOriginalBodyTypes[i] = rb.bodyType;
                gateBlockOriginalGravity[i] = rb.gravityScale;
            }
        }
    }

    void MakeGateBlocksKinematicForSeal()
    {
        for (int i = 0; i < gateBlockRigidbodies.Length; i++)
        {
            Rigidbody2D rb = gateBlockRigidbodies[i];
            if (rb == null) continue;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void RestoreGateBlockPhysics()
    {
        for (int i = 0; i < gateBlockRigidbodies.Length; i++)
        {
            Rigidbody2D rb = gateBlockRigidbodies[i];
            if (rb == null) continue;
            rb.bodyType = gateBlockOriginalBodyTypes[i];
            rb.gravityScale = gateBlockOriginalGravity[i];
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void ApplyBlockWorldPosition(int index, Vector3 worldPos)
    {
        Transform t = gateBlockTransforms[index];
        if (t == null) return;

        t.position = worldPos;
        Rigidbody2D rb = gateBlockRigidbodies[index];
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = worldPos;
        }
    }

    void LiftGateBlocksToSky()
    {
        MakeGateBlocksKinematicForSeal();

        for (int i = 0; i < gateBlockTransforms.Count; i++)
        {
            if (gateBlockTransforms[i] == null) continue;
            Vector3 high = gateBlockEndWorldPos[i] + Vector3.up * spawnHeightAboveAnchor;
            ApplyBlockWorldPosition(i, high);
        }
    }

    IEnumerator FallGateBlocksIn()
    {
        MakeGateBlocksKinematicForSeal();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fallDuration);
            float e = fallEase.Evaluate(Mathf.Clamp01(t));
            for (int i = 0; i < gateBlockTransforms.Count; i++)
            {
                if (gateBlockTransforms[i] == null) continue;
                Vector3 start = gateBlockEndWorldPos[i] + Vector3.up * spawnHeightAboveAnchor;
                Vector3 pos = Vector3.Lerp(start, gateBlockEndWorldPos[i], e);
                ApplyBlockWorldPosition(i, pos);
            }
            yield return null;
        }

        for (int i = 0; i < gateBlockTransforms.Count; i++)
        {
            if (gateBlockTransforms[i] != null)
                ApplyBlockWorldPosition(i, gateBlockEndWorldPos[i]);
        }

        RestoreGateBlockPhysics();
        gateFallFinished = true;
    }

    void SpawnBarrierAtRest()
    {
        if (blockPrefab == null || blockAnchors == null) return;

        for (int i = 0; i < blockAnchors.Length; i++)
        {
            Transform anchor = blockAnchors[i];
            if (anchor == null) continue;

            GameObject block = Instantiate(blockPrefab, anchor.position, anchor.rotation, blocksParent);
            spawnedInstances.Add(block);
        }
    }

    IEnumerator FallBlocksIn()
    {
        if (blockPrefab == null || blockAnchors == null) yield break;

        var jobs = new List<(GameObject go, Vector3 end)>();

        for (int i = 0; i < blockAnchors.Length; i++)
        {
            Transform anchor = blockAnchors[i];
            if (anchor == null) continue;

            Vector3 end = anchor.position;
            GameObject block = Instantiate(blockPrefab, end + Vector3.up * spawnHeightAboveAnchor, anchor.rotation, blocksParent);
            spawnedInstances.Add(block);
            jobs.Add((block, end));
        }

        if (jobs.Count == 0) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fallDuration);
            float e = fallEase.Evaluate(Mathf.Clamp01(t));
            for (int j = 0; j < jobs.Count; j++)
            {
                if (jobs[j].go == null) continue;
                jobs[j].go.transform.position = Vector3.Lerp(
                    jobs[j].end + Vector3.up * spawnHeightAboveAnchor,
                    jobs[j].end,
                    e);
            }
            yield return null;
        }

        for (int j = 0; j < jobs.Count; j++)
        {
            if (jobs[j].go != null)
                jobs[j].go.transform.position = jobs[j].end;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (UseGateMode && existingBlocksParent != null)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.85f);
            for (int i = 0; i < existingBlocksParent.childCount; i++)
            {
                Transform child = existingBlocksParent.GetChild(i);
                if (child == null) continue;
                Vector3 a = child.position;
                Gizmos.DrawLine(a, a + Vector3.up * spawnHeightAboveAnchor);
            }
            return;
        }

        if (blockAnchors == null) return;
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.85f);
        for (int i = 0; i < blockAnchors.Length; i++)
        {
            if (blockAnchors[i] == null) continue;
            Vector3 a = blockAnchors[i].position;
            Gizmos.DrawLine(a, a + Vector3.up * spawnHeightAboveAnchor);
        }
    }
}
