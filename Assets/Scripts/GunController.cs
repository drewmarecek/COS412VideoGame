using UnityEngine;

public class GunController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;     // Assign a tiny empty object at the gun's tip
    public float bulletForce = 20f;

    void Update()
    {
        AimAtMouse();

        if (Input.GetMouseButtonDown(0)) // Left Click
        {
            Shoot();
        }
    }

    void AimAtMouse()
    {
        // Get mouse position in world space
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Calculate direction from gun to mouse
        Vector3 direction = mousePos - transform.position;
        
        // Calculate the angle (Mathf.Atan2 returns radians, convert to degrees)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Apply rotation
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    void Shoot()
    {
        // Create bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        // Add force to the bullet's Rigidbody
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(firePoint.right * bulletForce, ForceMode2D.Impulse);
        }
    }
}