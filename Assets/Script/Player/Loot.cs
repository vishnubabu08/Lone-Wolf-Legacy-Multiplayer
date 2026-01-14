using Unity.VisualScripting;
using UnityEngine;

public class Loot : MonoBehaviour
{

    public float pickupRange = 3f;
    public KeyCode pickupKey = KeyCode.F;
    public LayerMask lootLayer;
    public Inventory inventory;
    public GameObject inventoryUI;
    public WeaponSwitcher weaponSwitcher;
    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, lootLayer);

        foreach (Collider hit in hits)
        {
            LootableItem loot = hit.GetComponent<LootableItem>();
            if (loot != null)
            {
                // Optional: show loot name or floating UI here
                if (Input.GetKeyDown(pickupKey))
                {
                    inventory.AddItem(loot.itemName, loot.icon, loot.tag);
                    loot.HidePickupUI(); // call this before destroy
                    Destroy(hit.gameObject);
                   
                    if (loot.tag == "Gun")
                    {
                        weaponSwitcher.ToggleWeapon(0);

                    }
                   

                    if (loot.tag == "Mag")
                    {
                        

                        // Find active weapon and add mag
                        Weapon[] weapons = FindObjectsOfType<Weapon>();
                        foreach (Weapon weapon in weapons)
                        {
                            if (weapon.gameObject.activeInHierarchy)
                            {
                                weapon.AddMagFromPickup();
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.E)) // or I, or custom input
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        inventoryUI.SetActive(!inventoryUI.activeSelf);
        Cursor.visible = inventoryUI.activeSelf;
        Cursor.lockState = inventoryUI.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
    }

}
