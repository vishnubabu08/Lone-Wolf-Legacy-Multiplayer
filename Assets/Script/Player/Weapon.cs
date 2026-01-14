using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using UnityEngine;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("Stats")]
    public int damage = 20;
    public float fireRate = 10f;
    public float reloadTime = 2.0f;

    [Header("Shooting Mechanics")]
    public bool isAutomatic = true;
    public float recoilForce = 2.0f;
    public float spreadFactor = 0.05f;

    [Header("Raycast Settings")]
    public LayerMask hitLayers;

    [Header("Ammo")]
    public int mag = 5;
    public int ammo = 30;
    public int magAmmo = 30;

    [Header("Refs")]
    public Camera camera;
    public Animator animator;
    public InputManager inputManager;
    public ParticleSystem muzzleFlash;
    public GameObject hitVFX;
    public TextMeshProUGUI magText;
    public TextMeshProUGUI ammoText;
    public AudioSource soundAudioSource;
    public AudioClip shootingSoundClip;
    public AudioClip reloadingSoundClip;

    private float nextFire;
    private bool isReloading;
    private bool triggerReleased = true;
    private CameraManager cameraManager;
    private bool isScoped = false;

    private void Start()
    {
        UpdateAmmoUI();
        cameraManager = FindObjectOfType<CameraManager>();
    }

    private void OnDisable()
    {
        isReloading = false;
        isScoped = false;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (nextFire > 0) nextFire -= Time.deltaTime;

        // Reload
        if (inputManager.reloadInput && !isReloading)
        {
            if (ammo < magAmmo && mag > 0) StartCoroutine(Reload());
        }

        // Fire
        if (ammo > 0 && !isReloading)
        {
            if (isAutomatic)
            {
                if (inputManager.shootInput && nextFire <= 0)
                {
                    Fire();
                    nextFire = 1 / fireRate;
                }
            }
            else
            {
                if (inputManager.shootInput && triggerReleased && nextFire <= 0)
                {
                    Fire();
                    nextFire = 1 / fireRate;
                    triggerReleased = false;
                }
                if (!inputManager.shootInput) triggerReleased = true;
            }
        }

        isScoped = inputManager.scopeInput && !isReloading;
    }

    IEnumerator Reload()
    {
        isReloading = true;
        animator.SetTrigger("Reload");
        if (soundAudioSource) soundAudioSource.PlayOneShot(reloadingSoundClip);

        yield return new WaitForSeconds(reloadTime);

        mag--;
        ammo = magAmmo;
        UpdateAmmoUI();
        isReloading = false;
    }

    void Fire()
    {
        ammo--;
        UpdateAmmoUI();
        if (muzzleFlash != null) muzzleFlash.Play();
        if (soundAudioSource != null && shootingSoundClip != null) soundAudioSource.PlayOneShot(shootingSoundClip);

        if (cameraManager != null) cameraManager.ApplyRecoil(recoilForce);

        Vector3 shootDirection = camera.transform.forward;
        float currentSpread = isScoped ? spreadFactor / 2 : spreadFactor;
        shootDirection.x += Random.Range(-currentSpread, currentSpread);
        shootDirection.y += Random.Range(-currentSpread, currentSpread);

        Ray ray = new Ray(camera.transform.position, shootDirection);
        RaycastHit hit;

        // Check if we hit anything within range
        if (Physics.Raycast(ray.origin, ray.direction, out hit, 280f, hitLayers))
        {
            Debug.Log("Hit: " + hit.transform.name);

            if (hitVFX != null)
            {
                PhotonNetwork.Instantiate(hitVFX.name, hit.point, Quaternion.identity);
            }

            // --- THE FIX: CRASH PREVENTION LOGIC ---
            // 1. Check if the object we hit has a Health Script
            if (hit.transform.TryGetComponent(out Health health))
            {
                // 2. Find the Target's PhotonView
                PhotonView targetPV = hit.transform.GetComponent<PhotonView>();

                // 3. Find MY PhotonView (Look in Parent because Weapon is a child object)
                PhotonView myPV = GetComponent<PhotonView>();
                if (myPV == null) myPV = GetComponentInParent<PhotonView>();

                // 4. If both exist, send the RPC safely
                if (targetPV != null && myPV != null)
                {
                    // Add local score immediately for feedback
                    PhotonNetwork.LocalPlayer.AddScore(damage);

                    // Send damage + My ID so I get the Kill/Score later
                    targetPV.RPC("TakeDamage", RpcTarget.All, damage, myPV.ViewID);
                }
                else
                {
                    Debug.LogWarning("Could not find PhotonView on Target or Shooter!");
                }
            }
        }
    }

    public void AddMagFromPickup() { mag++; UpdateAmmoUI(); }
    public void UpdateAmmoUI()
    {
        if (magText != null && ammoText != null)
        {
            magText.text = mag.ToString();
            ammoText.text = ammo + "/" + magAmmo;
        }
    }
}