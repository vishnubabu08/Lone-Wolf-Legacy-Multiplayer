using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class AppLoader : MonoBehaviour
{
    public Slider progressBar;
    public Text progressText;
    public Text pressToStartText; // UI text: "Press Any Button to Start"

    public float slowLoadSpeed = 0.15f; // slower loading

    private bool loadingFinished = false;

    void Start()
    {
        if (pressToStartText) pressToStartText.gameObject.SetActive(false);
        StartCoroutine(LoadGameAssets());
    }

    IEnumerator LoadGameAssets()
    {
        float progress = 0f;

        // Fake loading simulation (slow and smooth)
        while (progress < 1f)
        {
            progress += Time.deltaTime * slowLoadSpeed;

            if (progressBar) progressBar.value = progress;
            if (progressText) progressText.text = (int)(progress * 100) + "%";

            yield return null;
        }

        // Loading complete
        loadingFinished = true;

        if (progressText) progressText.text = "100%";

        // Show "Press Any Button to Start"
        if (pressToStartText)
        {
            pressToStartText.text = "Press Any Button to Start";
            pressToStartText.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        // Wait for input only AFTER loading finished
        if (loadingFinished)
        {
            if (Input.anyKeyDown)
            {
                LoadNextScene();
            }
        }
    }

    void LoadNextScene()
    {
        // If user is logged in → go to Lobby
        if (PlayerPrefs.HasKey("IsLoggedIn"))
            SceneManager.LoadScene("3_Lobby");
        else
            SceneManager.LoadScene("1_Login");
    }
}
