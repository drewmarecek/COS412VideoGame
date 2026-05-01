using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(Collider2D))]
public class BossDefeatTeleportZone : MonoBehaviour
{
    [Header("Boss Gate")]
    [SerializeField] private BossController boss1;
    [SerializeField] private bool autoFindBossIfMissing = true;

    [Header("Teleport")]
    [SerializeField] private string level2SceneName = "second_level";
    [SerializeField] private bool requireKeyPress = false;
    [SerializeField] private KeyCode teleportKey = KeyCode.E;

    [Header("Placement Behavior")]
    [SerializeField] private bool followPlayerWhileLocked = true;
    [SerializeField] private bool detachFromParentOnUnlock = true;
    [Tooltip("Offset from the player when the zone unlocks. X is always applied to the RIGHT of the player " +
             "(this is a left-to-right platformer), regardless of which side of the boss the player was on.")]
    [SerializeField] private Vector2 spawnAheadOffset = new Vector2(2f, 0f);

    [Header("Optional Visuals")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    private Collider2D zoneTrigger;
    private bool isUnlocked;
    private bool wasUnlockedLastFrame;
    private bool playerInside;
    private Transform player;

    private void Awake()
    {
        zoneTrigger = GetComponent<Collider2D>();
        zoneTrigger.isTrigger = true;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (boss1 == null && autoFindBossIfMissing)
            boss1 = FindFirstObjectByType<BossController>();

        RefreshLockState(force: true);
    }

    private void Update()
    {
        RefreshLockState(force: false);
        HandleZonePlacement();

        if (!isUnlocked || !requireKeyPress || !playerInside)
            return;

        if (Input.GetKeyDown(teleportKey))
            TeleportToLevel2();
    }

    private void RefreshLockState(bool force)
    {
        bool shouldBeUnlocked = boss1 != null && boss1.IsDefeated;
        if (!force && shouldBeUnlocked == isUnlocked) return;

        isUnlocked = shouldBeUnlocked;

        // Keep zone disabled while locked so player cannot trigger teleport early.
        zoneTrigger.enabled = isUnlocked;

        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }

    private void HandleZonePlacement()
    {
        if (player == null) return;

        if (!isUnlocked)
        {
            if (followPlayerWhileLocked)
                transform.position = player.position;

            wasUnlockedLastFrame = false;
            return;
        }

        if (wasUnlockedLastFrame) return;

        if (detachFromParentOnUnlock)
            transform.SetParent(null, true);

        // Always place the spawn to the RIGHT of the player. This is a left-to-right
        // platformer, so the next-level portal must be ahead of the player no matter
        // which side of the boss they were on at the moment of defeat.
        Vector3 spawnPos = player.position + new Vector3(Mathf.Abs(spawnAheadOffset.x), spawnAheadOffset.y, 0f);
        transform.position = spawnPos;
        wasUnlockedLastFrame = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked || !other.CompareTag("Player")) return;

        playerInside = true;
        if (!requireKeyPress)
            TeleportToLevel2();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    private void TeleportToLevel2()
    {
        if (!isUnlocked) return;

        int buildIndex = ResolveBuildIndex(level2SceneName);
        if (buildIndex < 0)
        {
#if UNITY_EDITOR
            string editorScenePath = FindScenePathInProject(level2SceneName);
            if (!string.IsNullOrEmpty(editorScenePath))
            {
                EditorSceneManager.LoadSceneInPlayMode(editorScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogError("Level2 scene is not in Build Settings or name/path is wrong: " + level2SceneName);
            return;
        }

        SceneManager.LoadScene(buildIndex);
    }

    private int ResolveBuildIndex(string sceneNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(sceneNameOrPath))
            return -1;

        // Fast path for exact scene name/path
        if (Application.CanStreamedLevelBeLoaded(sceneNameOrPath))
        {
            int directIndex = SceneUtility.GetBuildIndexByScenePath(sceneNameOrPath);
            if (directIndex >= 0) return directIndex;
        }

        // Robust path: scan scenes in build settings and match by file name.
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(scenePath)) continue;

            string fileName = ScenePathToName(scenePath);
            if (string.Equals(fileName, sceneNameOrPath, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>Returns the scene name from a Unity scene path without requiring System.IO.</summary>
    static string ScenePathToName(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return string.Empty;
        int slash = scenePath.LastIndexOf('/');
        int start = slash >= 0 ? slash + 1 : 0;
        int dot   = scenePath.LastIndexOf('.');
        int end   = dot > start ? dot : scenePath.Length;
        return scenePath.Substring(start, end - start);
    }

#if UNITY_EDITOR
    private string FindScenePathInProject(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return null;

        string[] guids = AssetDatabase.FindAssets(sceneName + " t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = ScenePathToName(path);
            if (string.Equals(fileName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }
#endif
}
