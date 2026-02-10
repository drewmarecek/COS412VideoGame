using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    // Set this to the Y-position where you want the camera to sit by default
    public float baselineHeight = 0.7f;

    // 1. Define the step size here (1.5 units)
    public float stepSize = 1.5f;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Debug.Log("Camera found the Player automatically!");
            }
            else
            {
                Debug.LogError("CAMERA ERROR: Could not find an object tagged 'Player'!");
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // --- X AXIS (Horizontal) ---
        float targetX = player.position.x;

        // --- Y AXIS (Vertical) ---
        // OLD LOGIC: targetY = baselineHeight;
        
        // NEW LOGIC: Grid Snapping
        // 1. Get player's distance from the baseline
        float difference = player.position.y - baselineHeight;

        // 2. Calculate how many "1.5 steps" fit into that difference.
        //    (int) casting cuts off the decimal, creating the "step" effect.
        int steps = (int)(difference / stepSize);

        // 3. Multiply the steps back by the stepSize to get the grid position
        //    Example: If steps = 2, targetY becomes 3.0.
        float targetY = baselineHeight + (steps * stepSize);

        // Apply position immediately.
        transform.position = new Vector3(targetX, targetY, -10f);
    }
}