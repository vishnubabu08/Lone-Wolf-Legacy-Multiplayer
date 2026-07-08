using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine.UI;
using System.Collections;

public class Health : MonoBehaviourPun, IPunObservable
{
    public int health = 100;
    public bool IsLocalPlayer;

    [Header("Regeneration")]
    public bool enableRegen = true;
    public float regenWaitTime = 5.0f;

    [Header("Two-Phase Regen Speeds")]
    public float slowRegenSpeed = 3f;
    public float fastRegenSpeed = 15f;

    private float lastDamageTime;
    private float healthFloat;
    private float regenStartHealth = -1f;
    private bool regenStarted = false;

    [Header("UI")]
    public Slider healthSlider;

    [Header("Death Settings")]
    public Animator animator;
    public MonoBehaviour[] scriptsToDisable;

    [Header("Low Health Effects")]
    public AudioSource heartbeatAudioSource;
    public AudioClip heartbeatClip;
    public Image vignetteImage;

    [Header("Low Health Thresholds")]
    public int lowHealthThreshold = 25;
    public float vignetteMaxAlpha = 0.6f;
    public float vignettePulseSpeed = 2f;
    public float heartbeatFadeSpeed = 3f;

    private bool isLowHealth = false;
    private float vignetteTimer = 0f;

    private bool isDead = false;

    private void Awake()
    {
        health = 100;
        healthFloat = 100f;
        isDead = false;

        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0f;
            vignetteImage.color = c;
        }

        if (heartbeatAudioSource != null)
        {
            heartbeatAudioSource.loop = true;
            heartbeatAudioSource.volume = 0f;
            heartbeatAudioSource.clip = heartbeatClip;
        }
    }

    private void Update()
    {
        if (healthSlider != null)
            healthSlider.value = health;

        if (IsLocalPlayer && photonView.IsMine)
        {
            HandleLowHealthEffects();
        }

        bool canRegen = (IsLocalPlayer && photonView.IsMine) ||
                        (GetComponent<BotController>() && PhotonNetwork.IsMasterClient);

        if (canRegen && enableRegen && !isDead)
        {
            if (health < 100 && Time.time > lastDamageTime + regenWaitTime)
            {
                if (!regenStarted)
                {
                    regenStartHealth = healthFloat;
                    regenStarted = true;
                }

                float missingHealth = 100f - regenStartHealth;
                float slowPhaseEnd = regenStartHealth + (missingHealth * 0.25f);
                float currentSpeed = (healthFloat < slowPhaseEnd) ? slowRegenSpeed : fastRegenSpeed;

                healthFloat += currentSpeed * Time.deltaTime;
                health = Mathf.FloorToInt(healthFloat);

                if (health >= 100)
                {
                    health = 100;
                    healthFloat = 100f;
                    regenStarted = false;
                }
            }
            else if (health >= 100)
            {
                regenStarted = false;
            }
            else
            {
                regenStarted = false;
            }
        }
    }

    private void HandleLowHealthEffects()
    {
        bool shouldBeActive = (health > 0 && health <= lowHealthThreshold);

        if (shouldBeActive)
        {
            isLowHealth = true;

            if (heartbeatAudioSource != null && heartbeatClip != null)
            {
                if (!heartbeatAudioSource.isPlaying)
                    heartbeatAudioSource.Play();

                float targetVolume = Mathf.Lerp(1f, 0.3f, (float)health / lowHealthThreshold);
                heartbeatAudioSource.volume = Mathf.MoveTowards(
                    heartbeatAudioSource.volume,
                    targetVolume,
                    Time.deltaTime * heartbeatFadeSpeed
                );

                heartbeatAudioSource.pitch = Mathf.Lerp(1.6f, 0.9f, (float)health / lowHealthThreshold);
            }

            if (vignetteImage != null)
            {
                vignetteTimer += Time.deltaTime * vignettePulseSpeed;
                float pulse = Mathf.PingPong(vignetteTimer, 1f);

                float healthPercent = (float)health / lowHealthThreshold;
                float minAlpha = Mathf.Lerp(vignetteMaxAlpha * 0.6f, 0.1f, healthPercent);
                float maxAlpha = Mathf.Lerp(vignetteMaxAlpha, vignetteMaxAlpha * 0.3f, healthPercent);
                float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);

                Color c = vignetteImage.color;
                c.a = targetAlpha;
                vignetteImage.color = c;
            }
        }
        else
        {
            isLowHealth = false;

            if (heartbeatAudioSource != null)
            {
                heartbeatAudioSource.volume = Mathf.MoveTowards(
                    heartbeatAudioSource.volume,
                    0f,
                    Time.deltaTime * heartbeatFadeSpeed
                );

                if (heartbeatAudioSource.volume <= 0f && heartbeatAudioSource.isPlaying)
                    heartbeatAudioSource.Stop();
            }

            if (vignetteImage != null)
            {
                Color c = vignetteImage.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 2f);
                vignetteImage.color = c;

                if (c.a <= 0f) vignetteTimer = 0f;
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(health);
        else
        {
            health = (int)stream.ReceiveNext();
            healthFloat = health;
        }
    }

    [PunRPC]
    public void TakeDamage(int _damage, int attackerViewID)
    {
        if (isDead) return;

        ArmorSystem armor = GetComponent<ArmorSystem>();
        int actualDamage = (armor != null) ? armor.AbsorbDamage(_damage) : _damage;

        health -= actualDamage;
        healthFloat = health;
        lastDamageTime = Time.time;
        regenStarted = false;

        if (healthSlider != null) healthSlider.value = health;

        if (health <= 0)
        {
            isDead = true;

            if (heartbeatAudioSource != null) { heartbeatAudioSource.Stop(); heartbeatAudioSource.volume = 0f; }
            if (vignetteImage != null) { Color c = vignetteImage.color; c.a = 0f; vignetteImage.color = c; }

            PhotonView attacker = PhotonView.Find(attackerViewID);
            if (attacker != null)
            {
                if (attacker.TryGetComponent(out BotController bot))
                {
                    if (PhotonNetwork.IsMasterClient) bot.GiveKill(100);
                }
                // In TakeDamage, where attacker gets kill credit
                // In TakeDamage where attacker gets kill credit
                else if (attacker.Owner != null)
                {
                    Photon.Realtime.Player player = attacker.Owner;

                    // ADD score for this kill only
                    player.AddScore(100);

                    if (PhotonNetwork.IsMasterClient)
                    {
                        var props = player.CustomProperties;
                        int currentKills = props.ContainsKey("kills") ? (int)props["kills"] : 0;
                        props["kills"] = currentKills + 1;
                        player.SetCustomProperties(props);
                    }
                }
            }

            // =============================================
            // BOT DEATH — spawn ammo drop then return
            // =============================================
            if (GetComponent<BotController>() != null)
            {
                AmmoDropSpawner dropSpawner = GetComponent<AmmoDropSpawner>();
                if (dropSpawner != null) dropSpawner.SpawnAmmoDrop();
                return;
            }
            // =============================================

            photonView.RPC("RPC_RealPlayerDeath", RpcTarget.All);
            if (photonView.IsMine) StartCoroutine(PlayerDeathRoutine());
        }
    }

    [PunRPC]
    void RPC_RealPlayerDeath()
    {
        if (animator != null) animator.SetTrigger("Die");
        foreach (var script in scriptsToDisable) if (script != null) script.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; rb.linearVelocity = Vector3.zero; }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
    }

    // =============================================
    // PLAYER DEATH — spawn ammo drop then respawn
    // =============================================
    IEnumerator PlayerDeathRoutine()
    {
        // Spawn ammo drop immediately at death position
        AmmoDropSpawner dropSpawner = GetComponent<AmmoDropSpawner>();
        if (dropSpawner != null) dropSpawner.SpawnAmmoDrop();

        yield return new WaitForSeconds(3.0f);

        if (RoomManager.instance != null)
        {
            var props = PhotonNetwork.LocalPlayer.CustomProperties;
            int currentDeaths = props.ContainsKey("deaths") ? (int)props["deaths"] : 0;
            props["deaths"] = currentDeaths + 1;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            RoomManager.instance.MapSpawnPlayer();
        }
        PhotonNetwork.Destroy(gameObject);
    }
    // =============================================
}