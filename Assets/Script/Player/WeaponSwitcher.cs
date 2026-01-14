using UnityEngine;
using System.Collections;
using Photon.Pun;

public class WeaponSwitcher : MonoBehaviourPun
{
    [Header("All Available Guns")]
    public GameObject[] weapons;

    [Header("Current Loadout")]
    public int slot1_ID = 0;
    public int slot2_ID = 1;

    public int currentWeaponIndex = -1; // Made public so we can see it
    public Animator animator;
    private InputManager inputManager;
    private int wepIndex;
    private Weapon[] weaponScripts;

    private void Start()
    {
        inputManager = GetComponent<InputManager>();

        // 1. LOAD FROM FIREBASE (Only for ME)
        if (photonView.IsMine && FirebaseManager.instance != null)
        {
            slot1_ID = FirebaseManager.instance.primaryGunID;
            slot2_ID = FirebaseManager.instance.secondaryGunID;
        }

        // Initialize Scripts
        weaponScripts = new Weapon[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weaponScripts[i] = weapons[i].GetComponent<Weapon>();
        }

        // Turn everything off initially
        DeactivateAllWeapons();

        // If mine, equip starting gun
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_ToggleWeapon", RpcTarget.AllBuffered, slot1_ID);
        }
    }

    private void Update()
    {
        // --- INPUT GATEKEEPER ---
        // Only the local player can use keyboard inputs
        if (!photonView.IsMine) return;

        // Press '1'
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Instead of running logic locally, we tell the Network to run it
            photonView.RPC("RPC_ToggleWeapon", RpcTarget.All, slot1_ID);
        }
        // Press '2'
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            photonView.RPC("RPC_ToggleWeapon", RpcTarget.All, slot2_ID);
        }

        // Aiming Logic (Visuals)
        bool aiming = inputManager.shootInput || inputManager.scopeInput;
        // Only set this if the gun is actually out
        if (animator.GetBool("rifleActive"))
        {
            animator.SetBool("rifleAimActive", aiming);
        }
    }

    // --- PUBLIC FUNCTION FOR LOOT.CS ---
    public void ToggleWeapon(int weaponID)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_ToggleWeapon", RpcTarget.All, weaponID);
        }
    }

    // --- THE NETWORKED LOGIC (Replaces your old local methods) ---

    [PunRPC]
    public void RPC_ToggleWeapon(int weaponID)
    {
        // Safety Check
        if (weaponID < 0 || weaponID >= weapons.Length) return;

        // 1. CHECK: Are we already holding this gun?
        if (currentWeaponIndex == weaponID)
        {
            // Yes -> Put it away (Holster)
            DeactivateAllWeapons();
            return;
        }

        // No -> Equip it
        SelectWeapon(weaponID);
    }

    void SelectWeapon(int weaponIndex)
    {
        // 1. Update the Mesh logic (Turn off old, prep new)
        UpdateWeaponOnNetwork(weaponIndex);

        // 2. Play Animation (Set the Bool True)
        animator.SetBool("rifleActive", true);
    }

    void UpdateWeaponOnNetwork(int weaponIndex)
    {
        // Deactivate all weapons first (Clean slate)
        foreach (GameObject weapon in weapons)
        {
            if (weapon) weapon.SetActive(false);
        }

        // Activate selected weapon logic
        if (weaponIndex >= 0 && weaponIndex < weapons.Length)
        {
            wepIndex = weaponIndex;

            // CHECK MOVEMENT
            // Note: For enemies, 'inputManager' might be disabled. 
            // We check the Animator state instead, as that syncs over network.
            bool isMoving = animator.GetFloat("Vertical") > 0.1f
                            || animator.GetFloat("Vertical") < -0.1f
                            || animator.GetBool("CrawlingActive")
                            || animator.GetBool("CrouchActive");

            if (isMoving)
            {
                // MOVING: Snap Immediately
                if (weapons[weaponIndex]) weapons[weaponIndex].SetActive(true);
                currentWeaponIndex = weaponIndex;

                // Update UI only if it's ME
                if (photonView.IsMine && weaponScripts[weaponIndex])
                    weaponScripts[weaponIndex].UpdateAmmoUI();
            }
            else
            {
                // STILL: Wait for animation (Your 1 second delay)
                StopCoroutine("WeaponActive"); // Stop any old routine
                StartCoroutine("WeaponActive");
            }

            // Update UI anyway for good measure if instant
            if (photonView.IsMine && weaponScripts[wepIndex])
                weaponScripts[wepIndex].UpdateAmmoUI();
        }
    }

    IEnumerator WeaponActive()
    {
        // Your exact delay logic
        yield return new WaitForSeconds(1f);

        if (weapons[wepIndex]) weapons[wepIndex].SetActive(true);
        currentWeaponIndex = wepIndex;

        if (photonView.IsMine && weaponScripts[wepIndex])
            weaponScripts[wepIndex].UpdateAmmoUI();
    }

    void DeactivateAllWeapons()
    {
        foreach (GameObject weapon in weapons)
        {
            if (weapon) weapon.SetActive(false);
        }

        currentWeaponIndex = -1;
        animator.SetBool("rifleActive", false);
        animator.SetBool("rifleAimActive", false);
    }
}