using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    // Call this from the "Customize/Shop" Button
    public void GoToCharacterShop()
    {
        SceneManager.LoadScene("2_Character");
    }

    // Call this from the "Exit" Button
    public void QuitGame()
    {
        Debug.Log("Quitting Game..."); // Shows in Unity Editor
        Application.Quit();            // Works in the final PC Build
    }
}