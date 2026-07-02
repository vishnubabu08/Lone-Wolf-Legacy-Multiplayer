using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BotSpawner : MonoBehaviourPunCallbacks
{
    public static BotSpawner instance;

    [Header("Settings")]
    public GameObject botPrefab;
    public Transform[] spawnPoints;

    private bool hasSpawned = false;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (!RoomManager.gameIsLive) return;
        if (hasSpawned) return;
        if (!PhotonNetwork.IsMasterClient) return;

        SpawnInitialBatch();
        hasSpawned = true;
    }

    void SpawnInitialBatch()
    {
        if (botPrefab == null) return;

        // Check if this is a custom (friends) room or global room
        string roomName = PhotonNetwork.CurrentRoom.Name;
        bool isGlobalRoom = roomName.StartsWith("Global_Map1") ||
                            roomName.StartsWith("Global_Map2");

        if (!isGlobalRoom)
        {
            Debug.Log("Custom room — no bots spawned.");
            return;
        }

        int realPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int roomMaxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
        int botsNeeded = roomMaxPlayers - realPlayers;
        if (botsNeeded < 0) botsNeeded = 0;

        Debug.Log($"BOT SPAWNER: Room max={roomMaxPlayers} | " +
                  $"Real={realPlayers} | Bots={botsNeeded}");

        for (int i = 0; i < botsNeeded; i++)
            SpawnSingleBot();
    }

    public void SpawnSingleBot(string oldName = "", int oldScore = 0,
                               int oldKills = 0, int oldDeaths = 0)
    {
        if (botPrefab == null || spawnPoints.Length == 0) return;

        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 randomPosition = sp.position + new Vector3(
            Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        Vector3 finalPos = sp.position + Vector3.up * 2f;
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(
            randomPosition, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            finalPos = hit.position;
        }

        GameObject newBot = PhotonNetwork.Instantiate(
            botPrefab.name, finalPos, Quaternion.identity);

        if (!string.IsNullOrEmpty(oldName))
        {
            newBot.GetComponent<PhotonView>().RPC(
                "RPC_LoadOldStats", RpcTarget.All,
                oldName, oldScore, oldKills, oldDeaths);
        }
    }
}