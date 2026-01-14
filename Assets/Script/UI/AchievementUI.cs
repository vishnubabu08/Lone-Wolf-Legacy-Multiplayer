using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementUI : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject rowPrefab;        // Drag your "Achievement_Row" prefab here
    public Transform contentParent;     // Drag the "Content" object of the ScrollView here

    [Header("Data")]
    public AchievementData[] achievements; // We will fill this list in Inspector

    // Run this whenever the menu is turned on
    void OnEnable()
    {
        RefreshList();

        // Clear the Red Dot notification
        if (AchievementManager.instance != null)
        {
            AchievementManager.instance.OnAchievementMenuOpened();
        }
    }

    void RefreshList()
    {
        // 1. Delete old items (to prevent duplicates)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Loop through all 10 achievements
        for (int i = 0; i < achievements.Length; i++)
        {
            // Create the row
            GameObject newRow = Instantiate(rowPrefab, contentParent);

            // Get the Script inside the row (We will create this helper class below)
            AchievementRowItem ui = newRow.GetComponent<AchievementRowItem>();

            // 3. Set Text
            ui.titleText.text = achievements[i].title;
            ui.descText.text = achievements[i].description;
            ui.rewardText.text = "+" + achievements[i].reward + " Coins";

            // 4. Check status from FirebaseManager
            bool isUnlocked = false;
            if (FirebaseManager.instance != null && i < FirebaseManager.instance.myAchievements.Length)
            {
                isUnlocked = FirebaseManager.instance.myAchievements[i];
            }

            // 5. Visual Logic (Show/Hide Lock)
            if (isUnlocked)
            {
                ui.lockOverlay.SetActive(false); // Hide lock
                ui.titleText.color = Color.white; // Bright text
            }
            else
            {
                ui.lockOverlay.SetActive(true); // Show lock
                ui.titleText.color = Color.grey; // Dim text
            }
        }
    }
}

// Simple class to hold data in Inspector
[System.Serializable]
public class AchievementData
{
    public string title;
    public string description;
    public int reward;
}