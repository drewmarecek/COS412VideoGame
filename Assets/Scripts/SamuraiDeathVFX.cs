using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss death flash: black screen + white silhouettes for player and boss.
/// Attach to the boss. Zero inspector wiring required beyond the whiteMaterial slot.
/// </summary>
public class SamuraiDeathVFX : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Color flashColor = Color.black;

    private const string OverrideLayer = "Default";
    private const int BgSortingOrder  = 29999;
    private const int CharSortingOrder = 30000;

    private SpriteRenderer[] playerRenderers;
    private SpriteRenderer[] bossRenderers;

    private readonly Dictionary<SpriteRenderer, Material> originalMaterials     = new Dictionary<SpriteRenderer, Material>();
    private readonly Dictionary<SpriteRenderer, Color>    originalColors         = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<SpriteRenderer, string>   originalLayerNames     = new Dictionary<SpriteRenderer, string>();
    private readonly Dictionary<SpriteRenderer, int>      originalSortingOrders  = new Dictionary<SpriteRenderer, int>();

    private GameObject activeFlashBackground;
    private Coroutine  activeRoutine;

    // -------------------------------------------------------------------------

    private void Start()
    {
        AutoWireTargets();
    }

    public void TriggerDeathFlash()
    {
        AutoWireTargets();
        LogTargetCounts();

        if (whiteMaterial == null)
            Debug.LogWarning("SamuraiDeathVFX: whiteMaterial is not assigned. Using runtime fallback material.", this);

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayDeathFlashRoutine());
    }

    // -------------------------------------------------------------------------

    private void AutoWireTargets()
    {
        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerRenderers = playerObject.GetComponentsInChildren<SpriteRenderer>(true);
                Debug.LogWarning($"[VFX Check] Found Player Object: {playerObject.name}. Found {playerRenderers.Length} sprite renderers on it.");
            }
        }

        if (bossRenderers == null || bossRenderers.Length == 0)
            bossRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void LogTargetCounts()
    {
        int playerCount = CountValidRenderers(playerRenderers);
        int bossCount   = CountValidRenderers(bossRenderers);

        Debug.Log($"SamuraiDeathVFX: Found {playerCount} player SpriteRenderer(s), {bossCount} boss SpriteRenderer(s).", this);

        if (playerCount == 0)
            Debug.LogWarning("SamuraiDeathVFX: No player SpriteRenderers found. Check Player tag and child renderers.", this);
        if (bossCount == 0)
            Debug.LogWarning("SamuraiDeathVFX: No boss SpriteRenderers found. Check boss child renderers.", this);
    }

    // -------------------------------------------------------------------------

    private IEnumerator PlayDeathFlashRoutine()
    {
        bool timePaused = false;
        try
        {
            SpawnBlackBackground();
            Time.timeScale = 0f;
            timePaused = true;
            ApplyWhiteSilhouette();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SamuraiDeathVFX: Exception during flash setup — {e.Message}\n{e.StackTrace}", this);
            if (timePaused) Time.timeScale = 1f;
            DestroyBlackBackground();
            activeRoutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, flashDuration));

        RestoreOriginalVisuals();
        Time.timeScale = 1f;
        DestroyBlackBackground();

        activeRoutine = null;
    }

    // -------------------------------------------------------------------------

    private void SpawnBlackBackground()
    {
        DestroyBlackBackground();

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("SamuraiDeathVFX: Camera.main not found, black background will not spawn.", this);
            return;
        }

        activeFlashBackground = new GameObject("SamuraiDeathVFXBackground");
        activeFlashBackground.transform.SetParent(cam.transform, false);

        SpriteRenderer bgRenderer = activeFlashBackground.AddComponent<SpriteRenderer>();
        bgRenderer.sprite           = CreateSolidSquareSprite();
        bgRenderer.color            = flashColor;
        bgRenderer.sortingLayerName = OverrideLayer;
        bgRenderer.sortingOrder     = BgSortingOrder;

        activeFlashBackground.transform.localPosition = new Vector3(0f, 0f, 1f);
        activeFlashBackground.transform.localScale    = new Vector3(5000f, 5000f, 1f);
    }

    private void ApplyWhiteSilhouette()
    {
        Material flashMaterial = GetWhiteMaterial();

        // Clear any stale state from a previous flash that didn't finish restoring.
        originalMaterials.Clear();
        originalColors.Clear();
        originalLayerNames.Clear();
        originalSortingOrders.Clear();

        ApplyWhiteToSet(playerRenderers, flashMaterial);
        ApplyWhiteToSet(bossRenderers,   flashMaterial);
    }

    private void ApplyWhiteToSet(SpriteRenderer[] renderers, Material flashMaterial)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            // Cache every renderer's original state individually.
            if (!originalMaterials.ContainsKey(sr))
                originalMaterials[sr] = sr.sharedMaterial;
            if (!originalColors.ContainsKey(sr))
                originalColors[sr] = sr.color;
            if (!originalLayerNames.ContainsKey(sr))
                originalLayerNames[sr] = sr.sortingLayerName;
            if (!originalSortingOrders.ContainsKey(sr))
                originalSortingOrders[sr] = sr.sortingOrder;

            // Force absolute sorting override so nothing hides behind the background.
            sr.sortingLayerName = OverrideLayer;
            sr.sortingOrder     = CharSortingOrder;
            sr.material         = flashMaterial;
            sr.color            = Color.white;
        }
    }

    private void RestoreOriginalVisuals()
    {
        foreach (KeyValuePair<SpriteRenderer, Material> kvp in originalMaterials)
        {
            if (kvp.Key != null) kvp.Key.material = kvp.Value;
        }
        foreach (KeyValuePair<SpriteRenderer, Color> kvp in originalColors)
        {
            if (kvp.Key != null) kvp.Key.color = kvp.Value;
        }
        foreach (KeyValuePair<SpriteRenderer, string> kvp in originalLayerNames)
        {
            if (kvp.Key != null) kvp.Key.sortingLayerName = kvp.Value;
        }
        foreach (KeyValuePair<SpriteRenderer, int> kvp in originalSortingOrders)
        {
            if (kvp.Key != null) kvp.Key.sortingOrder = kvp.Value;
        }

        originalMaterials.Clear();
        originalColors.Clear();
        originalLayerNames.Clear();
        originalSortingOrders.Clear();
    }

    // -------------------------------------------------------------------------

    private Material GetWhiteMaterial()
    {
        if (whiteMaterial != null)
            return whiteMaterial;

        Shader shader = Shader.Find("GUI/Text Shader");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        whiteMaterial = new Material(shader);
        whiteMaterial.hideFlags = HideFlags.HideAndDontSave;
        if (whiteMaterial.HasProperty("_Color"))
            whiteMaterial.SetColor("_Color", Color.white);

        return whiteMaterial;
    }

    private Sprite CreateSolidSquareSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void DestroyBlackBackground()
    {
        if (activeFlashBackground != null)
        {
            Destroy(activeFlashBackground);
            activeFlashBackground = null;
        }
    }

    private int CountValidRenderers(SpriteRenderer[] renderers)
    {
        if (renderers == null) return 0;
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) count++;
        return count;
    }

    private SpriteRenderer GetFirstValidRenderer(SpriteRenderer[] renderers)
    {
        if (renderers == null) return null;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) return renderers[i];
        return null;
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        RestoreOriginalVisuals();
        DestroyBlackBackground();
        Time.timeScale = 1f;
    }
}
