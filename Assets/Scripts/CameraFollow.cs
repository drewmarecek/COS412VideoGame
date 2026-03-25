using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    [Header("Vertical")]
    [Tooltip("The default camera Y position when the player is on the ground")]
    public float baselineHeight = 0.7f;
    [Tooltip("How far above the camera center the player can go before the camera follows them up")]
    public float verticalOffset = 1.5f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up vertically (lower = smoother)")]
    public float verticalSmoothTime = 0.25f;

    private CameraShake cameraShake;
    private float currentY;
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
            currentY = baselineHeight;
    }

    void LateUpdate()
    {
        if (player == null) return;

        float targetX = player.position.x;

        // Camera stays at baseline, but follows upward once player
        // goes more than verticalOffset above the current camera Y
        float targetY = Mathf.Max(baselineHeight, player.position.y - verticalOffset);

        currentY = Mathf.SmoothDamp(currentY, targetY, ref velocityY, verticalSmoothTime);

        Vector3 basePos = new Vector3(targetX, currentY, -10f);
        Vector3 shakeOffset = cameraShake != null ? cameraShake.GetShakeOffset() : Vector3.zero;
        transform.position = basePos + shakeOffset;
    }
}
