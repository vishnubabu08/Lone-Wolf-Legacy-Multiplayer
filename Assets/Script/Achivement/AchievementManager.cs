using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    [Header("UI References")]
    public GameObject notificationBadge;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UpdateBadge();
    }

    // =============================================
    // MAIN CHECKER — call this at end of every match
    // =============================================
    public void CheckAchievements(int matchKills, int matchDeaths,
                                 float matchDuration, bool isWinner)
    {
        if (FirebaseManager.instance == null)
        {
            Debug.LogWarning("FirebaseManager is null!");
            return;
        }

        var fm = FirebaseManager.instance;
        bool newUnlock = false;

        Debug.Log("=== CHECKING ACHIEVEMENTS ===");
        Debug.Log("fm.myKills: " + fm.myKills +
                  " | fm.matchesPlayed: " + fm.matchesPlayed +
                  " | fm.wins: " + fm.wins);

        if (fm.myKills > 0 && !fm.myAchievements[0])
        { Unlock(0, 50); newUnlock = true; }

        if (fm.myKills >= 50 && !fm.myAchievements[1])
        { Unlock(1, 200); newUnlock = true; }

        if (fm.myKills >= 500 && !fm.myAchievements[2])
        { Unlock(2, 1000); newUnlock = true; }

        if (!fm.myAchievements[3])
        {
            if (matchDeaths > 0)
            {
                float kd = (float)matchKills / matchDeaths;
                if (kd >= 2.0f) { Unlock(3, 100); newUnlock = true; }
            }
            else if (matchKills >= 2)
            { Unlock(3, 100); newUnlock = true; }
        }

        if (isWinner && matchDeaths == 0 && !fm.myAchievements[4])
        { Unlock(4, 500); newUnlock = true; }

        if (fm.matchesPlayed >= 5 && !fm.myAchievements[5])
        { Unlock(5, 100); newUnlock = true; }

        if (fm.matchesPlayed >= 50 && !fm.myAchievements[6])
        { Unlock(6, 500); newUnlock = true; }

        if (fm.wins >= 10 && !fm.myAchievements[7])
        { Unlock(7, 1000); newUnlock = true; }

        if (matchDuration >= 600f && !fm.myAchievements[8])
        { Unlock(8, 150); newUnlock = true; }

        if (newUnlock)
        {
            PlayerPrefs.SetInt("HasNewAchievement", 1);
            UpdateBadge();
            // NO SaveData here — EndGame handles saving after this returns
            Debug.Log("Achievements unlocked. Coins now: " + fm.myCoins);
        }
        else
        {
            Debug.Log("No new achievements.");
        }
    }

    // =============================================
    // BIG SPENDER — call when player buys something
    // =============================================
    public void CheckBigSpender()
    {
        if (FirebaseManager.instance == null) return;
        var fm = FirebaseManager.instance;

        if (!fm.myAchievements[9])
        {
            Unlock(9, 50);
            PlayerPrefs.SetInt("HasNewAchievement", 1);
            UpdateBadge();

            fm.SaveData(
                fm.myName, fm.myKills, fm.myDeaths, fm.myCoins,
                fm.headIndex, fm.helmetIndex, fm.vestIndex,
                fm.headsOwned, fm.helmetsOwned, fm.vestsOwned,
                fm.primaryGunID, fm.secondaryGunID, fm.gunsOwned
            );

            Debug.Log("Unlocked: Big Spender");
        }
    }

    void Unlock(int index, int reward)
    {
        FirebaseManager.instance.myAchievements[index] = true;
        FirebaseManager.instance.myCoins += reward;
        Debug.Log("Achievement[" + index + "] unlocked | +" + reward + " coins | Total: " +
                  FirebaseManager.instance.myCoins);
    }

    public void UpdateBadge()
    {
        if (notificationBadge != null)
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