using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    private Camera gameplayCamera;

    void Awake()
    {
        gameplayCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (gameplayCamera == null) return;

        Vector3 mousePos = gameplayCamera.ScreenToWorldPoint(Input.mousePosition);

        // If mouse is to the left and player is facing right
        if (mousePos.x < transform.position.x && transform.localScale.x > 0)
        {
            Flip();
        }
        // If mouse is to the right and player is facing left
        else if (mousePos.x > transform.position.x && transform.localScale.x < 0)
        {
            Flip();
        }
    }

    void Flip()
    {
        // Multiply the player's x local scale by -1.
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }
}