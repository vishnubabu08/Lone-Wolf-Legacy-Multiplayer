using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Firebase.Extensions;
using UnityEngine.UI;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;

    [Header("Login UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI feedbackText;
    public Button loginButton;
    public Button registerButton;

    FirebaseAuth auth;
    DatabaseReference dbReference;
    FirebaseUser user;

    // --- PLAYER DATA ---
    public string myName;
    public int myKills;
    public int myDeaths;
    public int myCoins;
    public int matchesPlayed;
    public int wins;

    // --- OUTFIT SELECTION ---
    public int headIndex;
    public int helmetIndex;
    public int vestIndex;

    // --- WEAPON LOADOUT ---
    public int primaryGunID = 0;
    public int secondaryGunID = 1;

    // --- OWNERSHIP DATA ---
    public string headsOwned;
    public string helmetsOwned;
    public string vestsOwned;
    public string gunsOwned;

    public bool isPremiumUser;

    // ALL 10 achievements
    public bool[] myAchievements = new bool[10];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupButtonListeners();
        }
        else
        {
            instance.emailInput = this.emailInput;
            instance.passwordInput = this.passwordInput;
            instance.feedbackText = this.feedbackText;
            instance.loginButton = this.loginButton;
            instance.registerButton = this.registerButton;
            instance.SetupButtonListeners();
            Destroy(gameObject);
        }
    }

    public void SetupButtonListeners()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnLoginPressed);
        }
        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(OnRegisterPressed);
        }
    }

    void Start()
    {
        if (instance == this)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                if (task.Result == DependencyStatus.Available)
                    InitializeFirebase();
                else
                    Debug.LogError("Could not fix dependencies: " + task.Result);
            });
        }
    }

    void InitializeFirebase()
    {
        string dbURL = "https://lone-wolf-legacy-default-rtdb.asia-southeast1.firebasedatabase.app/";
        AppOptions options = new AppOptions();
        options.DatabaseUrl = new System.Uri(dbURL);
        FirebaseApp app = FirebaseApp.Create(options);

        auth = FirebaseAuth.GetAuth(app);
        dbReference = FirebaseDatabase.GetInstance(app).RootReference;

        if (auth.CurrentUser != null)
        {
            user = auth.CurrentUser;
            if (SceneManager.GetActiveScene().name == "1_Login")
            {
                if (feedbackText) feedbackText.text = "Auto-Logging in...";
                StartCoroutine(LoadUserData());
            }
        }
    }

    public void OnLoginPressed()
    {
        if (instance != null)
            instance.StartCoroutine(instance.LoginLogic(
                instance.emailInput.text,
                instance.passwordInput.text));
    }

    public void OnRegisterPressed()
    {
        if (instance != null)
            instance.StartCoroutine(instance.RegisterLogic(
                instance.emailInput.text,
                instance.passwordInput.text));
    }

    private IEnumerator LoginLogic(string email, string password)
    {
        if (feedbackText) feedbackText.text = "Logging in...";
        var task = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            if (feedbackText) feedbackText.text = "Error: " +
                task.Exception.InnerExceptions[0].Message;
        }
        else
        {
            user = task.Result.User;
            if (feedbackText) feedbackText.text = "Success! Loading data...";
            StartCoroutine(LoadUserData());
        }
    }

    private IEnumerator RegisterLogic(string email, string password)
    {
        if (feedbackText) feedbackText.text = "Creating Account...";
        var task = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            if (feedbackText) feedbackText.text = "Error: " +
                task.Exception.InnerExceptions[0].Message;
        }
        else
        {
            user = task.Result.User;
            if (feedbackText) feedbackText.text = "Account Created!";
            SaveData("Player", 0, 0, 0, 0, -1, -1, "1", "1", "1", 0, 1, "11");
            SceneManager.LoadScene("2_Character");
        }
    }

    // =============================================
    // SAVE DATA — saves ALL 10 achievements
    // =============================================
    public void SaveData(string name, int kills, int deaths, int coins,
                         int hID, int helmID, int vID,
                         string hOwned, string helmOwned, string vOwned,
                         int pGunID, int sGunID, string gOwned)
    {
        if (user == null) return;

        UserGameData data = new UserGameData(
            name, kills, deaths, coins,
            matchesPlayed, wins,
            myAchievements, isPremiumUser,
            hID, helmID, vID,
            hOwned, helmOwned, vOwned,
            pGunID, sGunID, gOwned
        );

        string json = JsonUtility.ToJson(data);
        dbReference.Child("users").Child(user.UserId).SetRawJsonValueAsync(json);

        // Update local variables
        myName = name;
        myKills = kills;
        myDeaths = deaths;
        myCoins = coins;
        headIndex = hID;
        helmetIndex = helmID;
        vestIndex = vID;
        headsOwned = hOwned;
        helmetsOwned = helmOwned;
        vestsOwned = vOwned;
        primaryGunID = pGunID;
        secondaryGunID = sGunID;
        gunsOwned = gOwned;

        Debug.Log("Data saved. Achievements: " + string.Join(",", myAchievements));
    }

    // =============================================
    // LOAD DATA — loads ALL 10 achievements
    // =============================================
    private IEnumerator LoadUserData()
    {
        if (user == null) yield break;

        var task = dbReference.Child("users").Child(user.UserId).GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result.Value != null)
        {
            DataSnapshot snapshot = task.Result;
            try
            {
                if (snapshot.HasChild("userName"))
                    myName = snapshot.Child("userName").Value.ToString();
                if (snapshot.HasChild("kills"))
                    myKills = int.Parse(snapshot.Child("kills").Value.ToString());
                if (snapshot.HasChild("deaths"))
                    myDeaths = int.Parse(snapshot.Child("deaths").Value.ToString());
                if (snapshot.HasChild("coins"))
                    myCoins = int.Parse(snapshot.Child("coins").Value.ToString());
                if (snapshot.HasChild("matchesPlayed"))
                    matchesPlayed = int.Parse(snapshot.Child("matchesPlayed").Value.ToString());
                if (snapshot.HasChild("wins"))
                    wins = int.Parse(snapshot.Child("wins").Value.ToString());
                if (snapshot.HasChild("isPremium"))
                    isPremiumUser = (bool)snapshot.Child("isPremium").Value;

                if (snapshot.HasChild("headID"))
                    headIndex = int.Parse(snapshot.Child("headID").Value.ToString());
                if (snapshot.HasChild("helmetID"))
                    helmetIndex = int.Parse(snapshot.Child("helmetID").Value.ToString());
                if (snapshot.HasChild("vestID"))
                    vestIndex = int.Parse(snapshot.Child("vestID").Value.ToString());

                if (snapshot.HasChild("headsOwned"))
                    headsOwned = snapshot.Child("headsOwned").Value.ToString();
                else headsOwned = "1";

                if (snapshot.HasChild("helmetsOwned"))
                    helmetsOwned = snapshot.Child("helmetsOwned").Value.ToString();
                else helmetsOwned = "1";

                if (snapshot.HasChild("vestsOwned"))
                    vestsOwned = snapshot.Child("vestsOwned").Value.ToString();
                else vestsOwned = "1";

                if (snapshot.HasChild("primaryGunID"))
                    primaryGunID = int.Parse(snapshot.Child("primaryGunID").Value.ToString());
                else primaryGunID = 0;

                if (snapshot.HasChild("secondaryGunID"))
                    secondaryGunID = int.Parse(snapshot.Child("secondaryGunID").Value.ToString());
                else secondaryGunID = 1;

                if (snapshot.HasChild("gunsOwned"))
                    gunsOwned = snapshot.Child("gunsOwned").Value.ToString();
                else gunsOwned = "11";

                // =============================================
                // LOAD ALL 10 ACHIEVEMENTS
                // =============================================
                myAchievements = new bool[10];

                myAchievements[0] = snapshot.HasChild("ach_FirstBlood") &&
                                    (bool)snapshot.Child("ach_FirstBlood").Value;

                myAchievements[1] = snapshot.HasChild("ach_SerialKiller") &&
                                    (bool)snapshot.Child("ach_SerialKiller").Value;

                myAchievements[2] = snapshot.HasChild("ach_Terminator") &&
                                    (bool)snapshot.Child("ach_Terminator").Value;

                myAchievements[3] = snapshot.HasChild("ach_Sharpshooter") &&
                                    (bool)snapshot.Child("ach_Sharpshooter").Value;

                myAchievements[4] = snapshot.HasChild("ach_Untouchable") &&
                                    (bool)snapshot.Child("ach_Untouchable").Value;

                myAchievements[5] = snapshot.HasChild("ach_Rookie") &&
                                    (bool)snapshot.Child("ach_Rookie").Value;

                myAchievements[6] = snapshot.HasChild("ach_Veteran") &&
                                    (bool)snapshot.Child("ach_Veteran").Value;

                myAchievements[7] = snapshot.HasChild("ach_LoneWolf") &&
                                    (bool)snapshot.Child("ach_LoneWolf").Value;

                myAchievements[8] = snapshot.HasChild("ach_Survivor") &&
                                    (bool)snapshot.Child("ach_Survivor").Value;

                myAchievements[9] = snapshot.HasChild("ach_BigSpender") &&
                                    (bool)snapshot.Child("ach_BigSpender").Value;

                Debug.Log("Achievements loaded: " + string.Join(",", myAchievements));
                // =============================================

                SceneManager.LoadScene("3_Lobby");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error Parsing Data: " + e.Message);
                SceneManager.LoadScene("2_Character");
            }
        }
        else
        {
            SceneManager.LoadScene("2_Character");
        }
    }

    public void ResetPassword()
    {
        if (instance != null && !string.IsNullOrEmpty(instance.emailInput.text))
            auth.SendPasswordResetEmailAsync(instance.emailInput.text);
    }

    public void ResetAccount()
    {
        headIndex = 0;
        helmetIndex = 0;
        vestIndex = 0;
        headsOwned = "1";
        helmetsOwned = "1";
        vestsOwned = "1";
        gunsOwned = "11";
        primaryGunID = 0;
        secondaryGunID = 1;
        myCoins = 1000;

        SaveData(myName, 0, 0, myCoins,
                 0, 0, 0,
                 "1", "1", "1",
                 0, 1, "11");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

// =============================================
// USER DATA CLASS — saves ALL 10 achievements
// =============================================
[System.Serializable]
public class UserGameData
{
    public string userName;
    public int kills;
    public int deaths;
    public int coins;
    public int matchesPlayed;
    public int wins;
    public bool isPremium;

    public int headID;
    public int helmetID;
    public int vestID;

    public string headsOwned;
    public string helmetsOwned;
    public string vestsOwned;

    public int primaryGunID;
    public int secondaryGunID;
    public string gunsOwned;

    // ALL 10 achievements as separate fields
    // (JsonUtility doesn't support bool[] directly in Firebase)
    public bool ach_FirstBlood;
    public bool ach_SerialKiller;
    public bool ach_Terminator;
    public bool ach_Sharpshooter;
    public bool ach_Untouchable;
    public bool ach_Rookie;
    public bool ach_Veteran;
    public bool ach_LoneWolf;
    public bool ach_Survivor;
    public bool ach_BigSpender;

    public UserGameData(string name, int k, int d, int c, int mp, int w,
                        bool[] ach, bool prem,
                        int hID, int helmID, int vID,
                        string hOwn, string helmOwn, string vOwn,
                        int pGun, int sGun, string gOwn)
    {
        userName = name;
        kills = k;
        deaths = d;
        coins = c;
        matchesPlayed = mp;
        wins = w;
        isPremium = prem;

        headID = hID;
        helmetID = helmID;
        vestID = vID;

        headsOwned = hOwn;
        helmetsOwned = helmOwn;
        vestsOwned = vOwn;

        primaryGunID = pGun;
        secondaryGunID = sGun;
        gunsOwned = gOwn;

        // Map array to individual fields
        if (ach != null && ach.Length >= 10)
        {
            ach_FirstBlood = ach[0];
            ach_SerialKiller = ach[1];
            ach_Terminator = ach[2];
            ach_Sharpshooter = ach[3];
            ach_Untouchable = ach[4];
            ach_Rookie = ach[5];
            ach_Veteran = ach[6];
            ach_LoneWolf = ach[7];
            ach_Survivor = ach[8];
            ach_BigSpender = ach[9];
        }
    }
}