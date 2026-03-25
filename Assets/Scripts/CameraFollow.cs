using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    [Header("Spawn Offset")]
    [Tooltip("How far above the player the camera starts at spawn. Only affects the initial position.")]
    public float spawnOffset = 1.0f;

    [Header("Deadzone (player can move freely inside this box without the camera moving)")]
    [Tooltip("Half-width of the deadzone box. Player must push past this horizontally to move the camera.")]
    public float deadzoneX = 0.5f;
    [Tooltip("Half-height of the deadzone box. Player must push past this vertically to move the camera.")]
    public float deadzoneY = 1.0f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up horizontally")]
    public float horizontalSmoothTime = 0.1f;
    [Tooltip("How quickly the camera catches up vertically")]
    public float verticalSmoothTime = 0.2f;

    private CameraShake cameraShake;
    private float targetX;
    private float targetY;
    private float currentX;
    private float currentY;
    private float velocityX;
    private float velocityY;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        cameraShake = GetComponent<CameraShake>();

        if (player != null)
        {
            targetX = player.position.x;
            targetY = player.position.y + spawnOffset;
            currentX = targetX;
            currentY = targetY;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        float px = player.position.x;
        float py = player.position.y;

        // Horizontal: only move target when player pushes past the deadzone edge
        if (px > targetX + deadzoneX)
            targetX = px - deadzoneX;
        else if (px < targetX - deadzoneX)
            targetX = px + deadzoneX;

        // Vertical: only move target when player pushes past the deadzone edge
        if (py > targetY + deadzoneY)
            targetY = py - deadzoneY;
        else if (py < targetY - deadzoneY)
            targetY = py + deadzoneY;

        currentX = Mathf.SmoothDamp(currentX, targetX, ref velocityX, horizontalSmoothTime);
        currentY = Mathf.SmoothDamp(currentY, targetY, ref velocityY, verticalSmoothTime);

        Vector3 basePos = new Vector3(currentX, currentY, -10f);
        Vector3 shakeOffset = cameraShake != null ? cameraShake.GetShakeOffset() : Vector3.zero;
        transform.position = basePos + shakeOffset;
    }

    public void ResetToPlayer()
    {
        if (player == null) return;
        targetX = player.position.x;
        targetY = player.position.y + spawnOffset;
        currentX = targetX;
        currentY = targetY;
        velocityX = 0f;
        velocityY = 0f;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(new Vector3(targetX, targetY, 0f), new Vector2(deadzoneX * 2f, deadzoneY * 2f));
    }
}
