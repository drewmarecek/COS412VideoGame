using UnityEngine;

public class GunController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletForce = 20f;

    void Update()
    {
        AimAtMouse();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void AimAtMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Use transform.rotation (World Space) to ignore parent rotation
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // FIX THE DOUBLE FLIP:
        // We check if the player's scale is negative. 
        // If player is flipped (-1), the gun needs to flip its Y to stay upright.
        bool playerIsFlipped = transform.root.localScale.x < 0;

        if (playerIsFlipped)
        {
            // If player is facing Left, the gun's "up" is actually "down"
            // We flip the Y to compensate for the parent's -1 Scale X
            transform.localScale = new Vector3(-0.2f, -0.2f, 0.2f); 
        }
        else
        {
            // If player is facing Right, keep gun scale normal
            transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Use transform.right so it always shoots where the barrel is pointing
            rb.linearVelocity = transform.right * bulletForce;
        }
    }
}