using UnityEngine;
using Photon.Pun; // We need this to check if we are in a room

public class PauseMenu : MonoBehaviour
{
    public GameObject menuPanel;
    bool isPaused = false;

    void Start()
    {
        // Always close the menu when the game starts
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    void Update()
    {
        // 1. LISTEN FOR ESCAPE KEY
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 2. CHECK: ARE WE IN A ROOM?
            // If we are in the Lobby or Login, 'InRoom' is FALSE.
            // If we are in Global or Custom match, 'InRoom' is TRUE.
            if (PhotonNetwork.InRoom)
            {
                ToggleMenu();
            }
            else
            {
                // We are not in a room (Lobby/Login), so do nothing.
                Debug.Log("Pressed Escape, but not in a Room. Menu ignored.");
            }
        }
    }

    void ToggleMenu()
    {
        isPaused = !isPaused;

        if (menuPanel != null)
        {
            menuPanel.SetActive(isPaused);
        }

        // Mouse Logic: If paused, show mouse. If playing, hide mouse.
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Link this to your "EXIT" Button
    public void QuitMatch()
    {
        Debug.Log("Leaving Room...");

        // 1. Leave the Photon Room
        PhotonNetwork.LeaveRoom();

        // 2. Hide the menu immediately so it's gone when we hit the lobby
        if (menuPanel != null) menuPanel.SetActive(false);
        isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}