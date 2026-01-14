using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Scene Selection")]
    public string loginSceneName = "Login"; // Type the EXACT name of your Login scene here

    public void OnLogoutClick()
    {
        Debug.Log("Logging out...");

        // OPTIONAL: Clear saved data (like "Remember Me")
        // If you saved the username/password in PlayerPrefs, delete them here:
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Password");
        PlayerPrefs.Save();

        // 1. Disconnect from Photon
        PhotonNetwork.Disconnect();
    }

    // This runs automatically after Photon disconnects
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected. Returning to Login.");

        // 2. Go to Login Scene
        SceneManager.LoadScene(loginSceneName);
    }
}