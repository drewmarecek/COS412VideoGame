using UnityEngine;

public class GunController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletForce = 20f;

    [Header("Firing Settings")]
    public float fireRate = 3f; // Shots per second
    private float nextTimeToFire = 0f;

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        AimAtMouse();

        // Changed to GetMouseButton (for holding) + fireRate check
        if (Input.GetMouseButton(0) && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
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

        // YOUR ORIGINAL FLIP LOGIC
        bool playerIsFlipped = transform.root.localScale.x < 0;

        if (playerIsFlipped)
        {
            // Keeping your specific scale values: -0.2f, -0.2f, 0.2f
            transform.localScale = new Vector3(-0.15f, -0.15f, 0.15f); 
        }
        else
        {
            // Keeping your specific scale values: 0.2f, 0.2f, 0.2f
            transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        }
    }

    void Shoot()
    {
        // Added a quick null check just in case the prefab is missing
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = transform.right * bulletForce;
        }
    }
}