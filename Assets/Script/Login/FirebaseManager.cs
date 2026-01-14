using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Firebase.Extensions;
using UnityEngine.UI; // Required for Button

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;

    [Header("Login UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI feedbackText;

    // NEW: Drag your Buttons here in the Inspector
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
    public bool[] myAchievements = new bool[10];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initial setup for the very first launch
            SetupButtonListeners();
        }
        else
        {
            // === THE FIX FOR "MISSING OBJECT" ===
            // 1. Copy the NEW UI elements to the OLD Alive Instance
            instance.emailInput = this.emailInput;
            instance.passwordInput = this.passwordInput;
            instance.feedbackText = this.feedbackText;
            instance.loginButton = this.loginButton;
            instance.registerButton = this.registerButton;

            // 2. Force the NEW buttons to connect to the OLD Alive Instance
            instance.SetupButtonListeners();

            // 3. Destroy this new duplicate manager
            Destroy(gameObject);
        }
    }

    // New Helper Function to Link Buttons Code-Style
    public void SetupButtonListeners()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners(); // Clear old broken links
            loginButton.onClick.AddListener(OnLoginPressed); // Add fresh link
        }

        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(OnRegisterPressed);
        }
    }

    void Start()
    {
        // Only initialize if this is the main alive instance
        if (instance == this)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                if (task.Result == DependencyStatus.Available) InitializeFirebase();
                else Debug.LogError("Could not fix dependencies: " + task.Result);
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

    // Button Functions
    public void OnLoginPressed()
    {
        // Always use 'instance' to ensure we run on the ALIVE object
        if (instance != null) instance.StartCoroutine(instance.LoginLogic(instance.emailInput.text, instance.passwordInput.text));
    }

    public void OnRegisterPressed()
    {
        if (instance != null) instance.StartCoroutine(instance.RegisterLogic(instance.emailInput.text, instance.passwordInput.text));
    }

    private IEnumerator LoginLogic(string email, string password)
    {
        if (feedbackText) feedbackText.text = "Logging in...";
        var task = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            if (feedbackText) feedbackText.text = "Error: " + task.Exception.InnerExceptions[0].Message;
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
            if (feedbackText) feedbackText.text = "Error: " + task.Exception.InnerExceptions[0].Message;
        }
        else
        {
            user = task.Result.User;
            if (feedbackText) feedbackText.text = "Account Created!";
            // Optional: Create initial data structure here
            SaveData("Player", 0, 0, 0, 0, -1, -1, "1", "1", "1", 0, 1, "11");
            SceneManager.LoadScene("2_Character");
        }
    }

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

        // Update Local Variables
        myName = name; myKills = kills; myDeaths = deaths; myCoins = coins;
        headIndex = hID; helmetIndex = helmID; vestIndex = vID;
        headsOwned = hOwned; helmetsOwned = helmOwned; vestsOwned = vOwned;
        primaryGunID = pGunID; secondaryGunID = sGunID; gunsOwned = gOwned;
    }

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
                if (snapshot.HasChild("userName")) myName = snapshot.Child("userName").Value.ToString();
                if (snapshot.HasChild("kills")) myKills = int.Parse(snapshot.Child("kills").Value.ToString());
                if (snapshot.HasChild("deaths")) myDeaths = int.Parse(snapshot.Child("deaths").Value.ToString());
                if (snapshot.HasChild("coins")) myCoins = int.Parse(snapshot.Child("coins").Value.ToString());

                if (snapshot.HasChild("matchesPlayed")) matchesPlayed = int.Parse(snapshot.Child("matchesPlayed").Value.ToString());
                if (snapshot.HasChild("wins")) wins = int.Parse(snapshot.Child("wins").Value.ToString());
                if (snapshot.HasChild("isPremium")) isPremiumUser = (bool)snapshot.Child("isPremium").Value;

                if (snapshot.HasChild("headID")) headIndex = int.Parse(snapshot.Child("headID").Value.ToString());
                if (snapshot.HasChild("helmetID")) helmetIndex = int.Parse(snapshot.Child("helmetID").Value.ToString());
                if (snapshot.HasChild("vestID")) vestIndex = int.Parse(snapshot.Child("vestID").Value.ToString());

                if (snapshot.HasChild("headsOwned")) headsOwned = snapshot.Child("headsOwned").Value.ToString(); else headsOwned = "1";
                if (snapshot.HasChild("helmetsOwned")) helmetsOwned = snapshot.Child("helmetsOwned").Value.ToString(); else helmetsOwned = "1";
                if (snapshot.HasChild("vestsOwned")) vestsOwned = snapshot.Child("vestsOwned").Value.ToString(); else vestsOwned = "1";

                if (snapshot.HasChild("primaryGunID")) primaryGunID = int.Parse(snapshot.Child("primaryGunID").Value.ToString()); else primaryGunID = 0;
                if (snapshot.HasChild("secondaryGunID")) secondaryGunID = int.Parse(snapshot.Child("secondaryGunID").Value.ToString()); else secondaryGunID = 1;
                if (snapshot.HasChild("gunsOwned")) gunsOwned = snapshot.Child("gunsOwned").Value.ToString(); else gunsOwned = "11";

                if (snapshot.HasChild("ach_FirstBlood")) myAchievements[0] = (bool)snapshot.Child("ach_FirstBlood").Value;

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

    public void ResetPassword() { if (instance != null && !string.IsNullOrEmpty(instance.emailInput.text)) auth.SendPasswordResetEmailAsync(instance.emailInput.text); }


    public void ResetAccount()
    {
        // Reset local data to defaults
        headIndex = 0;
        helmetIndex = 0;
        vestIndex = 0;

        // Reset ownership to "1" (Default only)
        headsOwned = "1";
        helmetsOwned = "1";
        vestsOwned = "1";
        gunsOwned = "11"; // Default 2 guns

        primaryGunID = 0;
        secondaryGunID = 1;

        myCoins = 1000; // Starting money

        // Save these defaults to Firebase immediately
        SaveData(myName, 0, 0, myCoins,
                 0, 0, 0,
                 "1", "1", "1",
                 0, 1, "11");

        // Reload the scene to update the visuals
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}

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

    // Outfit Selection
    public int headID;
    public int helmetID;
    public int vestID;

    // Ownership Strings
    public string headsOwned;
    public string helmetsOwned;
    public string vestsOwned;

    // Weapons
    public int primaryGunID;
    public int secondaryGunID;
    public string gunsOwned;

    public bool ach_FirstBlood;

    public UserGameData(string name, int k, int d, int c, int mp, int w, bool[] ach, bool prem,
                        int hID, int helmID, int vID,
                        string hOwn, string helmOwn, string vOwn,
                        int pGun, int sGun, string gOwn)
    {
        userName = name; kills = k; deaths = d; coins = c; matchesPlayed = mp; wins = w; isPremium = prem;
        headID = hID; helmetID = helmID; vestID = vID;
        headsOwned = hOwn; helmetsOwned = helmOwn; vestsOwned = vOwn;
        primaryGunID = pGun; secondaryGunID = sGun; gunsOwned = gOwn;

        if (ach != null && ach.Length > 0) ach_FirstBlood = ach[0];
    }
}