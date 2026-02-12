using UnityEngine;

public class GunPickup : MonoBehaviour
{
    public float hoverSpeed = 2f;
    public float hoverHeight = 0.5f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Simple floating animation using Sin wave
        float newY = startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. Unlock the gun on the player
            WeaponManager manager = collision.GetComponent<WeaponManager>();
            if (manager != null)
            {
                manager.UnlockGun();
            }

            // 2. Destroy this object (the one on the ground)
            Destroy(gameObject);
        }
    }
}