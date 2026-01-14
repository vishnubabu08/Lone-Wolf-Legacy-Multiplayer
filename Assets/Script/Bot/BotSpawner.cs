using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BotSpawner : MonoBehaviourPunCallbacks
{
    public static BotSpawner instance;

    [Header("Settings")]
    public GameObject botPrefab;
    public Transform[] spawnPoints;
    public int maxPlayersInRoom = 20;

    private bool hasSpawned = false;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (!RoomManager.gameIsLive) return;
        if (hasSpawned) return;

        if (PhotonNetwork.IsMasterClient)
        {
            SpawnInitialBatch();
        }
        hasSpawned = true;
    }

    void SpawnInitialBatch()
    {
        if (botPrefab == null) return;

        int realPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int botsNeeded = maxPlayersInRoom - realPlayers;
        if (botsNeeded < 0) botsNeeded = 0;

        Debug.Log($"BOT SPAWNER: Spawning {botsNeeded} Bots.");

        for (int i = 0; i < botsNeeded; i++)
        {
            SpawnSingleBot();
        }
    }

    // --- UPDATED: NOW ACCEPTS STATS (OPTIONAL) ---
    public void SpawnSingleBot(string oldName = "", int oldScore = 0, int oldKills = 0, int oldDeaths = 0)
    {
        if (botPrefab == null || spawnPoints.Length == 0) return;

        // 1. Pick a random spawn point
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 randomPosition = sp.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        // 2. Spawn safely on NavMesh
        Vector3 finalPos = sp.position + Vector3.up * 2f; // Default air spawn
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(randomPosition, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            finalPos = hit.position;
        }

        // 3. Instantiate the Bot
        GameObject newBot = PhotonNetwork.Instantiate(botPrefab.name, finalPos, Quaternion.identity);

        // 4. RESTORE STATS (If this is a respawn)
        if (!string.IsNullOrEmpty(oldName))
        {
            // Call the RPC we made in BotController to inject the old score
            newBot.GetComponent<PhotonView>().RPC("RPC_LoadOldStats", RpcTarget.All, oldName, oldScore, oldKills, oldDeaths);
        }
    }
}