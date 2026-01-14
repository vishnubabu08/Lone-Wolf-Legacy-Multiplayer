using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine.UI;
using System.Collections;

// 1. Add IPunObservable to Sync Health continuously
public class Health : MonoBehaviourPun, IPunObservable
{
    public int health = 100;
    public bool IsLocalPlayer;

    [Header("Regeneration")]
    public bool enableRegen = true;
    public float regenWaitTime = 5.0f; // Wait 5s before healing
    public float regenSpeed = 10.0f;   // Heal 10 HP per second
    private float lastDamageTime;
    private float healthFloat; // Float for smooth calculation

    [Header("UI")]
    public Slider healthSlider;

    [Header("Death Settings")]
    public Animator animator;
    public MonoBehaviour[] scriptsToDisable;

    private bool isDead = false;

    private void Awake()
    {
        health = 100;
        healthFloat = 100f;
        isDead = false;
    }

    private void Update()
    {
        // UI Update (Run on everyone's screen)
        if (healthSlider != null)
        {
            healthSlider.value = health;
        }

        // --- REGENERATION LOGIC ---
        // Only the Owner (Local Player) or Master Client (for Bots) runs the math.
        // Everyone else just receives the result via OnPhotonSerializeView.
        bool canRegen = (IsLocalPlayer && photonView.IsMine) || (GetComponent<BotController>() && PhotonNetwork.IsMasterClient);

        if (canRegen && enableRegen && !isDead)
        {
            // If hurt AND enough time passed since last hit
            if (health < 100 && Time.time > lastDamageTime + regenWaitTime)
            {
                // Smoothly add health
                healthFloat += regenSpeed * Time.deltaTime;
                health = Mathf.FloorToInt(healthFloat);

                // Cap at 100
                if (health > 100)
                {
                    health = 100;
                    healthFloat = 100f;
                }
            }
        }
    }

    // --- SYNC HEALTH (Critical for Regen) ---
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Owner sends current health
            stream.SendNext(health);
        }
        else
        {
            // Others receive it
            health = (int)stream.ReceiveNext();
            healthFloat = health; // Sync float too so it doesn't glitch
        }
    }

    [PunRPC]
    public void TakeDamage(int _damage, int attackerViewID)
    {
        if (isDead) return;

        health -= _damage;
        healthFloat = health; // Sync the float logic

        // --- RESET REGEN TIMER ---
        lastDamageTime = Time.time;
        // -------------------------

        if (healthSlider != null) healthSlider.value = health;

        if (health <= 0)
        {
            isDead = true;

            // --- AWARD KILL TO ATTACKER ---
            PhotonView attacker = PhotonView.Find(attackerViewID);
            if (attacker != null)
            {
                // Attacker is Bot
                if (attacker.TryGetComponent(out BotController bot))
                {
                    if (PhotonNetwork.IsMasterClient) bot.GiveKill(100);
                }
                // Attacker is Human
                else if (attacker.Owner != null)
                {
                    Photon.Realtime.Player player = attacker.Owner;
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

            // --- DEATH LOGIC ---
            if (GetComponent<BotController>() != null) return;

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

    IEnumerator PlayerDeathRoutine()
    {
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
}