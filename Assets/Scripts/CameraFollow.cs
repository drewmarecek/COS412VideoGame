using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public Transform player;

    [Header("Settings")]
    public float verticalThreshold = 2.0f; // How high player can jump before cam moves

    void Update()
    {
        if (player == null) return;

        // 1. X-AXIS: Lock strictly to the player (Left/Right)
        float targetX = player.position.x;

        // 2. Y-AXIS: Start with the current camera height
        float targetY = transform.position.y;

        // "If the player is higher than the Camera + Threshold..."
        if (player.position.y > transform.position.y + verticalThreshold)
        {
            // "...Snap the camera up to follow them."
            targetY = player.position.y - verticalThreshold;
        }
        // (Optional) If you want the camera to move DOWN when they fall:
        else if (player.position.y < transform.position.y - verticalThreshold)
        {
             targetY = player.position.y + verticalThreshold;
        }

        // 3. Apply the position immediately (No smoothing, no elasticity)
        transform.position = new Vector3(targetX, targetY, -10f);
    }

    // VISUALIZER: Draws a yellow line in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Draw the "Ceiling" line
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 10, transform.position.y + verticalThreshold, 0), 
            new Vector3(transform.position.x + 10, transform.position.y + verticalThreshold, 0)
        );
    }
}