using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;

public class BotController : MonoBehaviourPun, IPunObservable
{
    [Header("Bot Info")]
    public string botName;
    public int score = 0;
    public int kills = 0;
    public int deaths = 0;

    [Header("AI Zones")]
    public float detectRange = 100f;
    public float sprintRange = 20f;
    public float attackRange = 10f;

    [Header("Speeds")]
    public float runSpeed = 3.5f;
    public float sprintSpeed = 6.0f;

    [Header("Combat")]
    public float fireRate = 1.0f;
    public int damage = 10;
    public LayerMask targetLayer;
    public LayerMask Obstacle;

    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform gunBarrel;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip shootingSound;
    public Health healthScript;
    public Collider botCollider;

    private Transform currentTarget;
    private float nextFireTime;
    private bool isDead = false;
    private float nextJumpTime;
    private float protectionTimer = 3.0f;

    // --- SYNC STATS ---
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(score);
            stream.SendNext(kills);
            stream.SendNext(deaths);
            stream.SendNext(botName);
        }
        else
        {
            score = (int)stream.ReceiveNext();
            kills = (int)stream.ReceiveNext();
            deaths = (int)stream.ReceiveNext();
            botName = (string)stream.ReceiveNext();
        }
    }

    void OnEnable()
    {
        protectionTimer = 3.0f;
        isDead = false;
        if (healthScript != null) healthScript.health = 100;
        if (botCollider != null) botCollider.enabled = true;

        // Only assign a random name if we don't have one yet
        if (string.IsNullOrEmpty(botName))
        {
            botName = "Bot " + Random.Range(100, 999);
        }
    }

    // --- NEW: Receive Old Stats from Spawner ---
    [PunRPC]
    public void RPC_LoadOldStats(string oldName, int oldScore, int oldKills, int oldDeaths)
    {
        botName = oldName;
        score = oldScore;
        kills = oldKills;
        deaths = oldDeaths;
        Debug.Log($"Bot Restored: {botName} with Score: {score}");
    }

    void Start()
    {
        protectionTimer = 3.0f;
        if (!PhotonNetwork.IsMasterClient) { this.enabled = false; return; }

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthScript = GetComponent<Health>();
        if (botCollider == null) botCollider = GetComponent<Collider>();
        if (animator) animator.SetBool("rifleActive", true);
    }

    void Update()
    {
        if (!RoomManager.gameIsLive) return;
        if (!PhotonNetwork.IsMasterClient || isDead) return;

        if (protectionTimer > 0)
        {
            protectionTimer -= Time.deltaTime;
            if (healthScript != null) healthScript.health = 100;
            return;
        }

        if (healthScript != null && healthScript.health <= 0)
        {
            // Only add death locally, sync handles the rest
            deaths++;
            Die();
            return;
        }

        FindNearestTarget();

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(currentTarget.position);
                animator.SetBool("rifleAimActive", false);
                CheckObstacleJump();

                if (distance <= attackRange + sprintRange)
                {
                    agent.speed = sprintSpeed;
                    animator.SetFloat("Vertical", 2f);
                }
                else
                {
                    agent.speed = runSpeed;
                    animator.SetFloat("Vertical", 1f);
                }
            }
            else
            {
                agent.isStopped = true;
                agent.speed = 0;
                animator.SetFloat("Vertical", 0f);
                animator.SetBool("rifleAimActive", true);

                Vector3 direction = (currentTarget.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), Time.deltaTime * 10f);

                if (Vector3.Angle(transform.forward, direction) < 30f)
                {
                    if (Time.time >= nextFireTime)
                    {
                        Attack();
                        nextFireTime = Time.time + (1f / fireRate);
                    }
                }
            }
        }
        else
        {
            animator.SetFloat("Vertical", 0f);
        }
    }

    public void GiveKill(int scoreToAdd)
    {
        kills++;
        score += scoreToAdd;
    }

    void Attack()
    {
        if (gunBarrel == null) return;
        photonView.RPC("RPC_BotShootEffects", RpcTarget.All);

        Vector3 targetPoint = currentTarget.position + Vector3.up * 1.3f;
        Vector3 shootDir = (targetPoint - gunBarrel.position).normalized;

        if (Physics.Raycast(gunBarrel.position, shootDir, out RaycastHit hit, attackRange, targetLayer))
        {
            if (hit.transform.TryGetComponent(out Health hp))
            {
                int myID = photonView.ViewID;
                hit.transform.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, damage, myID);
            }
        }
    }

    // ... (Keep RPC_BotShootEffects, CheckObstacleJump, VaultRoutine exactly as before) ...
    void CheckObstacleJump()
    {
        if (Time.time < nextJumpTime) return;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        Vector3 rayDirection = transform.forward;
        float rayDistance = 1.2f;

        if (Physics.Raycast(rayOrigin, rayDirection, rayDistance, Obstacle))
        {
            StartCoroutine(VaultRoutine());
            nextJumpTime = Time.time + 2.0f;
        }
    }

    IEnumerator VaultRoutine()
    {
        if (animator) animator.SetTrigger("VaultWall");
        if (botCollider) botCollider.enabled = false;
        float originalOffset = agent.baseOffset;
        agent.baseOffset = 1.2f;
        float duration = 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            agent.Move(transform.forward * 4f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agent.baseOffset = originalOffset;
        if (botCollider) botCollider.enabled = true;
    }

    void FindNearestTarget()
    {
        Health[] allTargets = FindObjectsOfType<Health>();
        float closestDist = detectRange;
        Transform bestTarget = null;

        foreach (Health potentialTarget in allTargets)
        {
            if (potentialTarget.gameObject == this.gameObject) continue;
            if (potentialTarget.health <= 0) continue;

            float dist = Vector3.Distance(transform.position, potentialTarget.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = potentialTarget.transform;
            }
        }
        currentTarget = bestTarget;
    }

    [PunRPC]
    void RPC_BotShootEffects()
    {
        if (animator) animator.SetTrigger("Fire");
        if (muzzleFlash) muzzleFlash.Play();
        if (audioSource && shootingSound) audioSource.PlayOneShot(shootingSound);
    }

    void Die()
    {
        if (isDead) return;
      //  Debug.LogError("BOT DIED!");
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false;
        photonView.RPC("RPC_BotDeath", RpcTarget.All);
        StartCoroutine(DestroyBotRoutine());
    }

    [PunRPC]
    void RPC_BotDeath()
    {
        if (animator) animator.SetTrigger("Die");
        if (botCollider) botCollider.enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }
        if (agent != null)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            agent.enabled = false;
        }
    }

    // --- UPDATED DESTROY ROUTINE ---
    System.Collections.IEnumerator DestroyBotRoutine()
    {
       // Debug.Log("Bot Died. Waiting 5 seconds...");
        yield return new WaitForSeconds(5f);

        if (PhotonNetwork.IsMasterClient)
        {
            if (BotSpawner.instance != null && RoomManager.gameIsLive)
            {
                Debug.Log("Requesting Respawn with stats...");
                // PASS THE CURRENT STATS TO THE SPAWNER
                BotSpawner.instance.SpawnSingleBot(botName, score, kills, deaths);
            }
            PhotonNetwork.Destroy(gameObject);
        }
    }
}