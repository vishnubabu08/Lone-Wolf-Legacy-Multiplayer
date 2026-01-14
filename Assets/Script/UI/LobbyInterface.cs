using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyInterface : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText; // Shows "PlayerName"
    public TextMeshProUGUI playerCoinsText; // Shows "Coins: 500"

    void Start()
    {
        // 1. Ensure Cursor is visible in Lobby (Critical for PC)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Load Data from FirebaseManager
        if (FirebaseManager.instance != null)
        {
            // Set Name
            if (playerNameText)
                playerNameText.text = FirebaseManager.instance.myName;

            // Set Coins
            if (playerCoinsText)
                playerCoinsText.text = "Coins: " + FirebaseManager.instance.myCoins;
        }
        else
        {
            // Fallback for testing without logging in
            if (playerNameText) playerNameText.text = "TestPlayer";
        }
    }

    // --- BUTTON FUNCTION ---
    public void OnCustomizeClicked()
    {
        // Save current state just in case
        // Go to Character Customization Scene
        SceneManager.LoadScene("2_Character");
    }

    public void OnQuitGameClicked()
    {
        Application.Quit();
    }
}