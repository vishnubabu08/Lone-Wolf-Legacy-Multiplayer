using UnityEngine;
using Photon.Pun;

public class AmmoDrop : MonoBehaviourPun
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30;           // How much ammo this drop gives
    public float lifetime = 30f;          // Disappears after 30 seconds
    public float pickupRange = 2f;        // How close player needs to be

    [Header("Visual Effects")]
    public float rotateSpeed = 90f;       // Spins so it's noticeable
    public float bobSpeed = 2f;           // Bobs up and down
    public float bobHeight = 0.3f;
    public Light glowLight;               // Optional point light for glow

    [Header("Flicker Warning")]
    // Starts flickering at this many seconds remaining to warn player
    public float flickerStartTime = 8f;
    public float flickerSpeed = 8f;

    private float timer;
    private Vector3 startPosition;
    private bool collected = false;
    private Renderer[] renderers;
    private Transform[] allChildObjects;

    private void Start()
    {
        timer = lifetime;
        startPosition = transform.position;

        // Get ALL renderers including inactive ones
        renderers = GetComponentsInChildren<Renderer>(true);

        // Also grab ALL child GameObjects to hide everything on flicker
        allChildObjects = GetComponentsInChildren<Transform>(true);

        if (PhotonNetwork.IsMasterClient)
            Invoke(nameof(DestroyDrop), lifetime);
    }
    private void Update()
    {
        if (collected) return;

        // --- ROTATE ---
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        // --- BOB UP AND DOWN ---
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // --- COUNTDOWN ---
        timer -= Time.deltaTime;

        // --- FLICKER WARNING when close to expiry ---
        if (timer <= flickerStartTime)
        {
            float flicker = Mathf.PingPong(Time.time * flickerSpeed, 1f);
            SetVisibility(flicker > 0.5f);
        }

        // --- PICKUP CHECK (local player only) ---
        // Find local player and check distance
        GameObject localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, localPlayer.transform.position);
            if (dist <= pickupRange)
            {
                Collect(localPlayer);
            }
        }
    }

    void Collect(GameObject player)
    {
        if (collected) return;
        collected = true;

        SetVisibility(false);
        enabled = false;

        // ONLY get the currently ACTIVE weapon — not all weapons
        Weapon activeWeapon = null;
        Weapon[] weapons = player.GetComponentsInChildren<Weapon>(true);

        foreach (Weapon w in weapons)
        {
            // Active weapon is the one whose GameObject is enabled
            if (w.gameObject.activeInHierarchy)
            {
                activeWeapon = w;
                break;
            }
        }

        if (activeWeapon != null)
        {
            // Priority 1: Gun is empty or not full — fill current mag
            if (activeWeapon.ammo < activeWeapon.magAmmo)
            {
                activeWeapon.ammo = activeWeapon.magAmmo;
                activeWeapon.UpdateAmmoUI();
                Debug.Log("Filled current mag: " + activeWeapon.ammo + "/" + activeWeapon.magAmmo);
            }
            // Priority 2: Current mag full — add ONE spare mag if under cap
            else if (activeWeapon.mag < activeWeapon.maxMags)
            {
                activeWeapon.mag++;
                activeWeapon.UpdateAmmoUI();
                Debug.Log("Added spare mag. Now: " + activeWeapon.mag + "/" + activeWeapon.maxMags);
            }
            else
            {
                Debug.Log("Mags full — drop ignored.");
            }
        }

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }

    void DestroyDrop()
    {
        if (!collected && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    void SetVisibility(bool visible)
    {
        // Hide every renderer on this object and all children
        foreach (var r in renderers)
            if (r != null) r.enabled = visible;

        if (glowLight != null)
            glowLight.enabled = visible;
    }

    // Find the local player GameObject
    GameObject GetLocalPlayer()
    {
        foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (pv.IsMine && pv.GetComponent<PlayerMovement>() != null)
                return pv.gameObject;
        }
        return null;
    }
}