using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyInterface : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerCoinsText;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    // =============================================
    // Call this anytime coins change
    // =============================================
    public void RefreshUI()
    {
        if (FirebaseManager.instance != null)
        {
            if (playerNameText)
                playerNameText.text = FirebaseManager.instance.myName;

            if (playerCoinsText)
                playerCoinsText.text = "Coins: " + FirebaseManager.instance.myCoins;
        }
        else
        {
            if (playerNameText) playerNameText.text = "TestPlayer";
            if (playerCoinsText) playerCoinsText.text = "Coins: 0";
        }
    }

    // Called every frame to keep coins synced
    private void Update()
    {
        if (FirebaseManager.instance != null && playerCoinsText != null)
        {
            playerCoinsText.text = "Coins: " + FirebaseManager.instance.myCoins;
        }
    }

    public void OnCustomizeClicked()
    {
        SceneManager.LoadScene("2_Character");
    }

    public void OnQuitGameClicked()
    {
        Application.Quit();
    }
}