using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss-death cinematic flash:
/// - Pause time
/// - Show fullscreen black background behind boss/player
/// - Force boss/player sprites to pure white
/// - Hold in realtime
/// - Restore everything
/// </summary>
public class SamuraiDeathFlash : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("All player SpriteRenderers to force white during the flash.")]
    [SerializeField] SpriteRenderer[] playerRenderers;

    [Tooltip("All boss SpriteRenderers to force white during the flash.")]
    [SerializeField] SpriteRenderer[] bossRenderers;

    [Header("Timing")]
    [Tooltip("Realtime duration while game is paused.")]
    [SerializeField] float flashDuration = 0.2f;

    [Header("Black Background UI")]
    [Tooltip("Optional existing black Image. If missing, one is created at runtime.")]
    [SerializeField] Image blackOverlayImage;

    [Tooltip("Optional canvas for the black image. If missing, one is created.")]
    [SerializeField] Canvas blackOverlayCanvas;

    [Tooltip("Canvas sorting order for the black background. Keep this below player/boss sort orders.")]
    [SerializeField] int blackOverlaySortingOrder = 0;

    Material whiteFlashMaterial;
    readonly Dictionary<SpriteRenderer, Material> originalMaterials = new Dictionary<SpriteRenderer, Material>();
    Coroutine activeRoutine;

    /// <summary>
    /// Starts the death flash sequence. Safe to call from boss death code.
    /// </summary>
    public void TriggerDeathFlash()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayFlashCoroutine());
    }

    IEnumerator PlayFlashCoroutine()
    {
        Time.timeScale = 0f;

        EnsureBlackBackground();
        SetBlackBackgroundVisible(true);

        ApplyWhiteMaterial(playerRenderers);
        ApplyWhiteMaterial(bossRenderers);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, flashDuration));

        RestoreOriginalMaterials();
        SetBlackBackgroundVisible(false);
        Time.timeScale = 1f;

        activeRoutine = null;
    }

    void EnsureBlackBackground()
    {
        if (blackOverlayCanvas == null)
        {
            GameObject canvasObj = new GameObject("SamuraiDeathFlashCanvas");
            blackOverlayCanvas = canvasObj.AddComponent<Canvas>();
            blackOverlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            blackOverlayCanvas.worldCamera = Camera.main;
            blackOverlayCanvas.planeDistance = 10f;
            blackOverlayCanvas.overrideSorting = true;
            blackOverlayCanvas.sortingOrder = blackOverlaySortingOrder;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        blackOverlayCanvas.overrideSorting = true;
        blackOverlayCanvas.sortingOrder = blackOverlaySortingOrder;

        if (blackOverlayImage == null)
        {
            GameObject imageObj = new GameObject("SamuraiDeathFlashBlack");
            imageObj.transform.SetParent(blackOverlayCanvas.transform, false);
            blackOverlayImage = imageObj.AddComponent<Image>();
            blackOverlayImage.color = Color.black;

            RectTransform rt = blackOverlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        blackOverlayImage.color = Color.black;
    }

    void SetBlackBackgroundVisible(bool visible)
    {
        if (blackOverlayCanvas != null)
            blackOverlayCanvas.enabled = visible;

        if (blackOverlayImage != null)
            blackOverlayImage.enabled = visible;
    }

    void ApplyWhiteMaterial(SpriteRenderer[] renderers)
    {
        if (renderers == null) return;

        Material whiteMat = GetWhiteFlashMaterial();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            if (!originalMaterials.ContainsKey(sr))
                originalMaterials[sr] = sr.material;

            sr.material = whiteMat;
        }
    }

    void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<SpriteRenderer, Material> kvp in originalMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.material = kvp.Value;
        }
        originalMaterials.Clear();
    }

    Material GetWhiteFlashMaterial()
    {
        if (whiteFlashMaterial != null)
            return whiteFlashMaterial;

        Shader shader = Shader.Find("GUI/Text Shader");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        whiteFlashMaterial = new Material(shader);
        whiteFlashMaterial.hideFlags = HideFlags.HideAndDontSave;

        if (whiteFlashMaterial.HasProperty("_Color"))
            whiteFlashMaterial.SetColor("_Color", Color.white);

        return whiteFlashMaterial;
    }

    void OnDisable()
    {
        // Safety restore if object is disabled mid-flash.
        RestoreOriginalMaterials();
        SetBlackBackgroundVisible(false);
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        Time.timeScale = 1f;
    }
}
