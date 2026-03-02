using UnityEngine;

public class PendulumSwing : MonoBehaviour
{
    public float speed = 2.0f;    // How fast it swings
    public float maxAngle = 75.0f; // How far it swings left/right

    void Update()
    {
        // Sin creates a smooth wave between -1 and 1
        // Multiplying by maxAngle makes it -75 to 75
        float angle = Mathf.Sin(Time.time * speed) * maxAngle;
        
        // Apply the rotation to the Z axis
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}