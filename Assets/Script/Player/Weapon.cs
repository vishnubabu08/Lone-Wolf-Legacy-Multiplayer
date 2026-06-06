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
    public int mag = 5;          // Spare magazines
    public int maxMags = 10;     // Max spare magazines cap
    public int ammo = 30;        // Bullets in current magazine
    public int magAmmo = 30;     // Max bullets per magazine

    [Header("Refs")]
    public Camera camera;
    public Animator animator;
    public InputManager inputManager;
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public GameObject hitVFX;
    public GameObject surfaceHitVFX;
    public TextMeshProUGUI magText;       // Shows spare mag COUNT (5, 4, 3...)
    public TextMeshProUGUI ammoText;      // Shows bullets in gun (30/30, 15/30...)
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
        // Make sure ammo starts correct
        ammo = magAmmo;
        UpdateAmmoUI();
        cameraManager = FindObjectOfType<CameraManager>();
    }

    private void OnEnable()
    {
        // Reset reload state when weapon is switched back
        isReloading = false;
        isScoped = false;
        UpdateAmmoUI();
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

        // AUTO RELOAD when ammo hits 0 and spare mags available
        if (ammo <= 0 && !isReloading && mag > 0)
        {
            StartCoroutine(Reload());
        }

        // Manual reload
        if (inputManager.reloadInput && !isReloading)
        {
            if (ammo < magAmmo && mag > 0)
                StartCoroutine(Reload());
        }

        // Fire — only when ammo > 0 and NOT reloading
        if (ammo > 0 && !isReloading)
        {
            if (isAutomatic)
            {
                if (inputManager.shootInput && nextFire <= 0)
                {
                    Fire();
                    nextFire = 1f / fireRate;
                }
            }
            else
            {
                if (inputManager.shootInput && triggerReleased && nextFire <= 0)
                {
                    Fire();
                    nextFire = 1f / fireRate;
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
        if (animator) animator.SetTrigger("Reload");
        if (soundAudioSource) soundAudioSource.PlayOneShot(reloadingSoundClip);

        yield return new WaitForSeconds(reloadTime);

        // Safety check — mag might have changed during reload wait
        if (mag > 0)
        {
            mag--;
            ammo = magAmmo;
        }

        UpdateAmmoUI();
        isReloading = false;
    }

    void Fire()
    {
        ammo--;
        UpdateAmmoUI();

        if (muzzleFlash != null) muzzleFlash.Play();
        if (soundAudioSource != null && shootingSoundClip != null)
            soundAudioSource.PlayOneShot(shootingSoundClip);
        if (muzzleLight != null) StartCoroutine(FlashMuzzleLight());
        if (cameraManager != null) cameraManager.ApplyRecoil(recoilForce);

        Vector3 shootDirection = camera.transform.forward;
        float currentSpread = isScoped ? spreadFactor / 2 : spreadFactor;
        shootDirection.x += Random.Range(-currentSpread, currentSpread);
        shootDirection.y += Random.Range(-currentSpread, currentSpread);

        Ray ray = new Ray(camera.transform.position, shootDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray.origin, ray.direction, out hit, 280f, hitLayers))
        {
            Debug.Log("Hit: " + hit.transform.name);

            bool hitCharacter = hit.transform.TryGetComponent(out Health health);

            if (hitCharacter)
            {
                if (hitVFX != null)
                    PhotonNetwork.Instantiate(hitVFX.name, hit.point, Quaternion.identity);

                PhotonView targetPV = hit.transform.GetComponent<PhotonView>();
                PhotonView myPV = GetComponent<PhotonView>();
                if (myPV == null) myPV = GetComponentInParent<PhotonView>();

                if (targetPV != null && myPV != null)
                {
                    PhotonNetwork.LocalPlayer.AddScore(damage);
                    targetPV.RPC("TakeDamage", RpcTarget.All, damage, myPV.ViewID);
                }
            }
            else
            {
                if (surfaceHitVFX != null)
                {
                    Quaternion hitRotation = Quaternion.LookRotation(hit.normal);
                    PhotonNetwork.Instantiate(surfaceHitVFX.name, hit.point, hitRotation);
                }
            }
        }
    }

    IEnumerator FlashMuzzleLight()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        muzzleLight.enabled = false;
    }

    // =============================================
    // UI — called from here AND from AmmoDrop
    // =============================================
    public void UpdateAmmoUI()
    {
        if (magText != null)
            magText.text = mag.ToString();          // Just the number: 5, 4, 3...

        if (ammoText != null)
            ammoText.text = ammo + "/" + magAmmo;   // Bullets: 30/30, 15/30...
    }

    public void AddMagFromPickup()
    {
        if (mag < maxMags)
        {
            mag++;
            UpdateAmmoUI();
        }
    }
}