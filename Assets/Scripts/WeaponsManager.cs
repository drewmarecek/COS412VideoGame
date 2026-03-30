using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    // Persist weapon inventory/state across scene loads.
    private static bool persistedHasGun = false;
    private static bool persistedGunEquipped = false;

    [Header("Weapon Objects")]
    public GameObject swordObject; // Drag your Sword child here
    public GameObject gunObject;   // Drag your Gun child here

    [Header("Inventory")]
    public bool hasGun = false;    // Starts false

    void Start()
    {
        // Restore inventory/equipped state when entering a new scene.
        hasGun = persistedHasGun;
        ApplyWeaponVisualState();
    }

    void Update()
    {

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

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

        // Toggle weapons with Q, BUT only if we have picked up the gun
        if (Input.GetKeyDown(KeyCode.Q) && hasGun)
        {
            ToggleWeapon();
        }
    }

    void Flip()
    {
        // Multiply the player's x local scale by -1.
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }

    void ToggleWeapon()
    {
        bool isSwordActive = swordObject != null && swordObject.activeSelf;
        
        if (swordObject != null) swordObject.SetActive(!isSwordActive);
        if (gunObject != null) gunObject.SetActive(isSwordActive);

        persistedGunEquipped = gunObject != null && gunObject.activeSelf;
    }

    // Call this function when the player touches the pickup
    public void UnlockGun()
    {
        hasGun = true;
        persistedHasGun = true;
        persistedGunEquipped = true;

        // Optional: Auto-switch to gun immediately upon pickup
        if (swordObject != null) swordObject.SetActive(false);
        if (gunObject != null) gunObject.SetActive(true);
    }

    private void ApplyWeaponVisualState()
    {
        // Default for new game / no gun.
        if (!hasGun)
        {
            if (swordObject != null) swordObject.SetActive(true);
            if (gunObject != null) gunObject.SetActive(false);
            return;
        }

        // Has gun: restore last equipped weapon.
        if (persistedGunEquipped)
        {
            if (swordObject != null) swordObject.SetActive(false);
            if (gunObject != null) gunObject.SetActive(true);
        }
        else
        {
            if (swordObject != null) swordObject.SetActive(true);
            if (gunObject != null) gunObject.SetActive(false);
        }
    }
}