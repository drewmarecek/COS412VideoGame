using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Objects")]
    public GameObject swordObject; // Drag your Sword child here
    public GameObject gunObject;   // Drag your Gun child here

    [Header("Inventory")]
    public bool hasGun = false;    // Starts false

    void Start()
    {
        // The "if != null" checks prevent the error if a slot is empty
        if (swordObject != null) swordObject.SetActive(true);
        if (gunObject != null) gunObject.SetActive(false);
    }

    void Update()
    {
        // Toggle weapons with Q, BUT only if we have picked up the gun
        if (Input.GetKeyDown(KeyCode.Q) && hasGun)
        {
            ToggleWeapon();
        }
    }

    void ToggleWeapon()
    {
        bool isSwordActive = swordObject != null && swordObject.activeSelf;
        
        if (swordObject != null) swordObject.SetActive(!isSwordActive);
        if (gunObject != null) gunObject.SetActive(isSwordActive);
    }

    // Call this function when the player touches the pickup
    public void UnlockGun()
    {
        hasGun = true;
        // Optional: Auto-switch to gun immediately upon pickup
        swordObject.SetActive(false);
        gunObject.SetActive(true);
    }
}