using UnityEngine;

public class HeadAim : MonoBehaviour
{
    void Update()
    {
        // 1. Get mouse position
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;

        // 2. Calculate Angle
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 3. Rotate the head
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 4. FLIP LOGIC: If mouse is to the left of the player, flip the head
        if (mousePos.x < transform.position.x)
        {
            // Flip the head on the Y axis so it's not upside down
            transform.localScale = new Vector3(1, -1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }
}