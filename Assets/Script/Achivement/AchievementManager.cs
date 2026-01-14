using UnityEngine;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    [Header("UI References")]
    public GameObject notificationBadge; // The Red Dot on the Achievement Button

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UpdateBadge();
    }

    // --- MAIN CHECKER FUNCTION ---
    public void CheckAchievements(int matchKills, int matchDeaths, float matchDuration, bool isWinner)
    {
        if (FirebaseManager.instance == null) return;
        var fm = FirebaseManager.instance;

        bool newUnlock = false;

        // --- 1. COMBAT ACHIEVEMENTS ---
        if (fm.myKills > 0 && !fm.myAchievements[0]) { Unlock(0, 50); newUnlock = true; } // First Blood
        if (fm.myKills >= 50 && !fm.myAchievements[1]) { Unlock(1, 200); newUnlock = true; } // Serial Killer
        if (fm.myKills >= 500 && !fm.myAchievements[2]) { Unlock(2, 1000); newUnlock = true; } // Terminator

        // Sharpshooter
        if (matchDeaths > 0)
        {
            float kd = (float)matchKills / matchDeaths;
            if (kd >= 2.0f && !fm.myAchievements[3]) { Unlock(3, 100); newUnlock = true; }
        }
        else if (matchKills >= 2 && !fm.myAchievements[3]) { Unlock(3, 100); newUnlock = true; }

        if (isWinner && matchDeaths == 0 && !fm.myAchievements[4]) { Unlock(4, 500); newUnlock = true; } // Untouchable

        // --- 2. VETERAN ACHIEVEMENTS ---
        if (fm.matchesPlayed >= 5 && !fm.myAchievements[5]) { Unlock(5, 100); newUnlock = true; } // Rookie
        if (fm.matchesPlayed >= 50 && !fm.myAchievements[6]) { Unlock(6, 500); newUnlock = true; } // Veteran
        if (fm.wins >= 10 && !fm.myAchievements[7]) { Unlock(7, 1000); newUnlock = true; } // Lone Wolf
        if (matchDuration >= 600f && !fm.myAchievements[8]) { Unlock(8, 150); newUnlock = true; } // Survivor

        // --- SAVE IF CHANGED ---
        if (newUnlock)
        {
            PlayerPrefs.SetInt("HasNewAchievement", 1);
            UpdateBadge();

            // --- FIX IS HERE: Added the 3 new WEAPON arguments ---
            fm.SaveData(
                fm.myName, fm.myKills, fm.myDeaths, fm.myCoins,
                fm.headIndex, fm.helmetIndex, fm.vestIndex,
                fm.headsOwned, fm.helmetsOwned, fm.vestsOwned,
                fm.primaryGunID, fm.secondaryGunID, fm.gunsOwned // <--- ADDED THESE 3
            );
        }
    }

    public void CheckBigSpender()
    {
        var fm = FirebaseManager.instance;
        if (!fm.myAchievements[9])
        {
            Unlock(9, 50);
            PlayerPrefs.SetInt("HasNewAchievement", 1);
            UpdateBadge();
        }
    }

    void Unlock(int index, int reward)
    {
        FirebaseManager.instance.myAchievements[index] = true;
        FirebaseManager.instance.myCoins += reward;
        Debug.Log("ACHIEVEMENT UNLOCKED: ID " + index + " | Reward: " + reward);
    }

    public void UpdateBadge()
    {
        if (notificationBadge)
        {
            bool hasNew = PlayerPrefs.GetInt("HasNewAchievement", 0) == 1;
            notificationBadge.SetActive(hasNew);
        }
    }

    public void OnAchievementMenuOpened()
    {
        PlayerPrefs.SetInt("HasNewAchievement", 0);
        UpdateBadge();
    }
}