using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // If we are offline, connect immediately
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("FORCE CONNECTING...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master. Joining Random Room...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room found. Creating one...");
        PhotonNetwork.CreateRoom("TestRoom", new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("JOINED ROOM! Now Bots can spawn.");
        // We don't need to call SpawnBots() here manually because 
        // BotSpawner.cs is likely waiting for the connection or can be restarted.

        // If BotSpawner failed in Start(), we might need to kickstart it here:
       // if (FindObjectOfType<BotSpawner>())
           // FindObjectOfType<BotSpawner>().Start();
    }
}