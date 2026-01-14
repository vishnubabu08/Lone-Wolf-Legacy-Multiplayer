using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq; // <--- REQUIRED FOR LEADERBOARD SORTING

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance;

    // --- ADDED: TRAFFIC LIGHT VARIABLE ---
    // This controls when bots are allowed to spawn and attack
    public static bool gameIsLive = false;
    // -------------------------------------

    public enum GameMode { Global, Custom }
    [Header("Game Mode")]
    public GameMode currentGameMode = GameMode.Global;

    [Header("Player & Spawn")]
    public GameObject player;
    public Transform[] spawnPoints;   // Map1
    public Transform[] spawnPoints1;  // Map2

    [Header("UI References")]
    public GameObject roomCam;
    public GameObject playerObject;
    public Button startButton;
    public TextMeshProUGUI startButtonText;

    [Header("Status & Timer UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI startTimerText;
    public TextMeshProUGUI matchTimerText;

    [Header("Game Over / Leaderboard UI")]
    public GameObject gameOverPanel;        // Drag your "GameOverPanel" here
    public Transform top3Container;         // Drag the "Vertical Layout Group" here
    public GameObject playerRowPrefab;      // Drag your Text Prefab here
    public TextMeshProUGUI winnerText;      // Drag the Big "Winner Name" Text here
    public TextMeshProUGUI myStatsText;     // Drag the "Your Rank/Kills" Text here

    [Header("Lobby - Custom Room UI")]
    public GameObject customRoomPanel;
    public TMP_InputField roomNameInput;
    public TMP_InputField maxPlayersInput;
    public TMP_Dropdown timeSelectDropdown;

    [Header("Lobby - Mode Selection")]
    public TMP_Dropdown gameModeDropdown;

    [Header("Map Settings")]
    public GameObject MapSelectionUI;
    public GameObject floodGround;
    public GameObject wareHouse;
    public bool Map1 = true;
    public bool Map2 = false;

    // --- FLAGS & TIMERS ---
    public bool playerSpawned = false;
    private bool isPreGameCountdown = false;
    private bool isMatchLive = false;
    private bool timerHasStarted = false;

    private double preGameEndTime = 0;
    private double matchEndTime = 0;

    // === CONFIG ===
    [Header("Config")]
    public float preGameLength = 10f;
    public float defaultMatchLength = 600f;
    public int minPlayersToStart = 1;

    private float matchLengthInSeconds;
    private string nickName = "unnamed";

    // Stats
    public int kills = 0;
    public int deaths = 0;

    // Room property keys
    private const string PROP_MAP = "Map";
    private const string PROP_MATCH_STATE = "MatchState";
    private const string PROP_PRE_END = "PreGameEnd";
    private const string PROP_MATCH_END = "MatchEnd";
    private const string PROP_MATCH_LENGTH = "MatchLength";

    // join attempt state
    private int joinAttemptIndex = 0;
    private bool attemptingJoin = false;

    private void Awake()
    {
        instance = this;

        // --- ADDED: RED LIGHT (RESET ON LOAD) ---
        gameIsLive = false;
        // ----------------------------------------

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (gameModeDropdown != null)
        {
            gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            OnGameModeChanged(gameModeDropdown.value);
        }

        ChangeNickname();

        if (startTimerText != null) startTimerText.gameObject.SetActive(false);
        if (matchTimerText != null) matchTimerText.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    IEnumerator Start()
    {
        // 1. If already connected (Returning from Shop), don't reconnect
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
        {
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start";
            yield break;
        }

        // 2. Disconnect if stuck in a room
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // 3. Connect
        if (!PhotonNetwork.IsConnected)
        {
            yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // ---------------- UI Logic ----------------
    public void OnGameModeChanged(int idx)
    {
        if (idx == 0)
        {
            currentGameMode = GameMode.Global;
            if (customRoomPanel) customRoomPanel.SetActive(false);
        }
        else
        {
            currentGameMode = GameMode.Custom;
            if (customRoomPanel) customRoomPanel.SetActive(true);
        }
    }

    private void OnStartButtonClicked()
    {
        if (currentGameMode == GameMode.Global)
        {
            joinAttemptIndex = 0;
            attemptingJoin = true;
            StartCoroutine(TryJoinOrCreateMapRoomCoroutine());
        }
        else
        {
            if (roomNameInput == null || maxPlayersInput == null) return;
            if (string.IsNullOrEmpty(roomNameInput.text) || string.IsNullOrEmpty(maxPlayersInput.text)) return;
            JoinCustomRoom();
        }
    }

    // ---------------- Matchmaking ----------------
    IEnumerator TryJoinOrCreateMapRoomCoroutine()
    {
        if (startButton != null) startButton.interactable = false;
        if (startButtonText != null) startButtonText.text = "Matching...";

        while (attemptingJoin)
        {
            string targetName = BuildMapRoomName(joinAttemptIndex);

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = 20,
                IsVisible = true,
                IsOpen = true
            };

            Hashtable roomProps = new Hashtable();
            roomProps.Add(PROP_MAP, Map1 ? "Map1" : "Map2");
            roomProps.Add(PROP_MATCH_STATE, "Waiting");
            roomProps.Add(PROP_MATCH_LENGTH, (int)defaultMatchLength);

            options.CustomRoomProperties = roomProps;
            options.CustomRoomPropertiesForLobby = new string[] { PROP_MATCH_LENGTH };

            PhotonNetwork.JoinOrCreateRoom(targetName, options, TypedLobby.Default);

            float timeout = 2f;
            float t = 0f;
            while (attemptingJoin && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (attemptingJoin)
            {
                if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();

                joinAttemptIndex++;
                if (joinAttemptIndex > 50)
                {
                    Debug.LogError("Failed to find/create a waiting room.");
                    attemptingJoin = false;
                    if (startButton != null) startButton.interactable = true;
                    if (startButtonText != null) startButtonText.text = "Start";
                    yield break;
                }
            }
            else
            {
                yield break;
            }
        }
    }

    string BuildMapRoomName(int index)
    {
        string mapPrefix = Map1 ? "Global_Map1" : "Global_Map2";
        string baseName = mapPrefix + "_Match";
        if (index <= 0) return baseName;
        return baseName + "_" + index;
    }

    void JoinCustomRoom()
    {
        string roomName = roomNameInput.text;
        byte maxPlayers;
        if (!byte.TryParse(maxPlayersInput.text, out maxPlayers)) maxPlayers = 10;
        maxPlayers = (byte)Mathf.Clamp(maxPlayers, 1, 20);

        int selectedMinutes = 10;
        if (timeSelectDropdown != null)
        {
            string optionText = timeSelectDropdown.options[timeSelectDropdown.value].text;
            int.TryParse(optionText, out selectedMinutes);
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = maxPlayers;
        options.IsVisible = true;
        options.IsOpen = true;

        Hashtable roomProps = new Hashtable();
        roomProps.Add(PROP_MATCH_LENGTH, selectedMinutes * 60);
        roomProps.Add(PROP_MAP, Map1 ? "Map1" : "Map2");
        roomProps.Add(PROP_MATCH_STATE, "Waiting");

        options.CustomRoomProperties = roomProps;
        options.CustomRoomPropertiesForLobby = new string[] { PROP_MATCH_LENGTH };

        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);

        if (startButtonText) startButtonText.text = "Joining...";
        startButton.interactable = false;
    }

    // ---------------- Photon Callbacks ----------------
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        if (startButton != null) startButton.interactable = true;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        if (attemptingJoin)
        {
            bool accept = true;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_STATE))
            {
                string state = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_STATE] as string;
                if (state == "Started")
                {
                    if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_END))
                    {
                        double me = (double)PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_END];
                        if (PhotonNetwork.Time < me) accept = false;
                    }
                    else accept = false;
                }
            }

            if (!accept)
            {
                PhotonNetwork.LeaveRoom();
                joinAttemptIndex++;
                return;
            }
            attemptingJoin = false;
        }

        if (MapSelectionUI) MapSelectionUI.SetActive(false);
        if (customRoomPanel) customRoomPanel.SetActive(false);
        if (gameModeDropdown) gameModeDropdown.gameObject.SetActive(false);
        if (startButton) startButton.gameObject.SetActive(false);

        timerHasStarted = false;
        playerSpawned = false;
        isPreGameCountdown = false;
        isMatchLive = false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_LENGTH))
            matchLengthInSeconds = (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_LENGTH];
        else
            matchLengthInSeconds = defaultMatchLength;

        if (statusText) statusText.gameObject.SetActive(true);

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_PRE_END))
        {
            preGameEndTime = (double)PhotonNetwork.CurrentRoom.CustomProperties[PROP_PRE_END];
            if (preGameEndTime > PhotonNetwork.Time)
            {
                isPreGameCountdown = true;
                timerHasStarted = true;
                if (statusText) statusText.text = "Get Ready!";
                if (startTimerText) startTimerText.gameObject.SetActive(true);
            }
        }

        UpdateStatusText();
        CheckPlayerCountAndStart();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        if (attemptingJoin)
        {
            joinAttemptIndex++;
        }
        else
        {
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start";
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        UpdateStatusText();
        CheckPlayerCountAndStart();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (propertiesThatChanged.ContainsKey(PROP_PRE_END))
        {
            preGameEndTime = (double)propertiesThatChanged[PROP_PRE_END];
            isPreGameCountdown = true;
            timerHasStarted = true;
            if (statusText) statusText.text = "Get Ready!";
            if (startTimerText) startTimerText.gameObject.SetActive(true);
        }

        if (propertiesThatChanged.ContainsKey(PROP_MATCH_END))
        {
            matchEndTime = (double)propertiesThatChanged[PROP_MATCH_END];
            isMatchLive = true;
            if (matchTimerText) matchTimerText.gameObject.SetActive(true);
        }

        if (propertiesThatChanged.ContainsKey(PROP_MATCH_STATE))
        {
            string state = propertiesThatChanged[PROP_MATCH_STATE] as string;
            if (state == "Started")
            {
                isMatchLive = true;
                if (matchTimerText) matchTimerText.gameObject.SetActive(true);
            }
        }
    }

    // ---------------- Update Loop ----------------
    private void Update()
    {
        if (isPreGameCountdown && !playerSpawned)
        {
            double remaining = preGameEndTime - PhotonNetwork.Time;
            if (remaining > 0)
            {
                if (startTimerText != null)
                {
                    startTimerText.gameObject.SetActive(true);
                    startTimerText.text = "Match Starting in: " + remaining.ToString("F1") + "s";
                }
            }
            else
            {
                isPreGameCountdown = false;
                if (startTimerText) startTimerText.gameObject.SetActive(false);
                StartGame();
            }
        }

        if (isMatchLive)
        {
            double remaining = matchEndTime - PhotonNetwork.Time;
            if (remaining > 0)
            {
                int minutes = Mathf.FloorToInt((float)remaining / 60f);
                int seconds = Mathf.FloorToInt((float)remaining % 60f);
                if (matchTimerText != null)
                {
                    matchTimerText.gameObject.SetActive(true);
                    matchTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                }
            }
            else
            {
                EndGame();
            }
        }
    }

    // ---------------- Master logic ----------------
    void CheckPlayerCountAndStart()
    {
        UpdateStatusText();
        if (!PhotonNetwork.IsMasterClient) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_STATE))
        {
            string state = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_STATE] as string;
            if (state != "Waiting") return;
        }

        int players = PhotonNetwork.CurrentRoom.PlayerCount;
        if (players >= minPlayersToStart)
        {
            if (!timerHasStarted)
            {
                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_PRE_END))
                {
                    timerHasStarted = true;
                    double preGameEnd = PhotonNetwork.Time + preGameLength;
                    Hashtable props = new Hashtable { { PROP_PRE_END, preGameEnd } };
                    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                }
            }
        }
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        bool isWaiting = true;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_STATE))
        {
            string st = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_STATE] as string;
            isWaiting = st == "Waiting";
        }

        if (isWaiting && !isPreGameCountdown && !isMatchLive)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Waiting: " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        }
        else if (isPreGameCountdown) statusText.text = "Get Ready!";
        else if (isMatchLive) statusText.text = "Match Live";
    }

    // ---------------- Game start / spawn ----------------
    public void StartGame()
    {
        if (playerSpawned) return;

        playerSpawned = true;
        isPreGameCountdown = false;

        // --- ADDED: GREEN LIGHT (START ATTACKING) ---
        gameIsLive = true;
        // --------------------------------------------

        if (statusText) statusText.gameObject.SetActive(false);
        if (roomCam) roomCam.SetActive(false);
        if (playerObject) playerObject.SetActive(false);

        if (Map1) MapSpawnPlayer();
        else if (Map2) MapSpawnPlayer1();
        else MapSpawnPlayer();

        if (PhotonNetwork.IsMasterClient && !isMatchLive)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            Hashtable stateProp = new Hashtable { { PROP_MATCH_STATE, "Started" } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(stateProp);

            matchEndTime = PhotonNetwork.Time + matchLengthInSeconds;
            Hashtable props = new Hashtable { { PROP_MATCH_END, matchEndTime } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            isMatchLive = true;
        }
    }

    // ---------------- END GAME & LEADERBOARD ----------------
    void EndGame()
    {
        // --- ADDED: RED LIGHT (STOP ATTACKING) ---
        gameIsLive = false;
        // -----------------------------------------

        // CRASH FIX: If internet disconnected, stop safely
        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("Disconnected before game end. Loading Lobby.");
            SceneManager.LoadScene("3_Lobby");
            return;
        }

        isMatchLive = false;
        if (matchTimerText != null) matchTimerText.text = "GAME OVER";

        // Save Stats to Firebase
        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.matchesPlayed++;
            int newTotalKills = FirebaseManager.instance.myKills + kills;
            int newTotalDeaths = FirebaseManager.instance.myDeaths + deaths;
            int earnedCoins = kills * 2;
            int newTotalCoins = FirebaseManager.instance.myCoins + earnedCoins;

            FirebaseManager.instance.SaveData(
                FirebaseManager.instance.myName,
                newTotalKills,
                newTotalDeaths,
                newTotalCoins,
                FirebaseManager.instance.headIndex,
                FirebaseManager.instance.helmetIndex,
                FirebaseManager.instance.vestIndex,
                FirebaseManager.instance.headsOwned,
                FirebaseManager.instance.helmetsOwned,
                FirebaseManager.instance.vestsOwned,
                FirebaseManager.instance.primaryGunID,
                FirebaseManager.instance.secondaryGunID,
                FirebaseManager.instance.gunsOwned
            );
        }

        // Show UI
        ShowGameOverUI();

        // Unlock Cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowGameOverUI()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (roomCam != null) roomCam.SetActive(true);
        if (playerObject != null) playerObject.SetActive(false);

        // 1. Get Players & Sort by Kills
        var sortedPlayers = PhotonNetwork.PlayerList.OrderByDescending(p =>
            p.CustomProperties.ContainsKey("kills") ? (int)p.CustomProperties["kills"] : 0
        ).ToList();

        // 2. Clear old list
        if (top3Container != null)
        {
            foreach (Transform child in top3Container) Destroy(child.gameObject);
        }

        // 3. Create List using your Custom Prefab
        int count = 0;
        foreach (var p in sortedPlayers)
        {
            if (count >= 3) break; // Top 3 Only

            // Get Data
            int pKills = p.CustomProperties.ContainsKey("kills") ? (int)p.CustomProperties["kills"] : 0;
            int pDeaths = p.CustomProperties.ContainsKey("deaths") ? (int)p.CustomProperties["deaths"] : 0;

            if (playerRowPrefab != null && top3Container != null)
            {
                GameObject row = Instantiate(playerRowPrefab, top3Container);

                // --- CONNECT TO YOUR NEW SCRIPT ---
                PlayerListItem item = row.GetComponent<PlayerListItem>();
                if (item != null)
                {
                    // Pass the data to the prefab
                    item.Setup(p.NickName, pKills, pDeaths, count + 1);
                }
            }

            // Set Winner Text (Big text at top of screen)
            if (count == 0 && winnerText != null)
            {
                winnerText.text = $"WINNER\n{p.NickName}";
            }
            count++;
        }

        // 4. Show My Stats (At the bottom)
        if (myStatsText != null)
        {
            int myRank = sortedPlayers.IndexOf(PhotonNetwork.LocalPlayer) + 1;
            int myKills = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("kills") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["kills"] : 0;
            myStatsText.text = $"RANK: #{myRank} | KILLS: {myKills}";
        }
    }

    public void LeaveMatch()
    {
        Debug.Log("Leaving Match via Button...");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene("3_Lobby");
    }

    public void MapSpawnPlayer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
        SetupPlayer(_player);
    }

    public void MapSpawnPlayer1()
    {
        if (spawnPoints1 == null || spawnPoints1.Length == 0) return;
        Transform spawnPoint = spawnPoints1[Random.Range(0, spawnPoints1.Length)];
        GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
        SetupPlayer(_player);
    }

    void SetupPlayer(GameObject _player)
    {
        if (_player == null) return;
        var pu = _player.GetComponent<PlayerSetup>();
        if (pu != null) pu.IslocalPlayer();

        var pv = _player.GetComponent<PhotonView>();
        if (pv != null) pv.RPC("SetNickname", RpcTarget.AllBuffered, nickName);

        PhotonNetwork.LocalPlayer.NickName = nickName;
        var hp = _player.GetComponent<Health>();
        if (hp != null) hp.IsLocalPlayer = true;
    }

    public void ChangeNickname()
    {
        if (FirebaseManager.instance != null && !string.IsNullOrEmpty(FirebaseManager.instance.myName))
            nickName = FirebaseManager.instance.myName;
        else
        {
            int randomNum = Random.Range(1000, 9999);
            nickName = "Guest_" + randomNum;
        }
        if (PhotonNetwork.LocalPlayer != null) PhotonNetwork.LocalPlayer.NickName = nickName;
    }

    public void SetHashes()
    {
        try
        {
            Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
            hash["kills"] = kills;
            hash["deaths"] = deaths;
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
        catch { }
    }

    public void MapUIActive(bool isActive) { if (MapSelectionUI) MapSelectionUI.SetActive(isActive); }

    public void choosMap(bool map)
    {
        Map1 = map;
        Map2 = !map;
        if (floodGround) floodGround.SetActive(map);
        if (wareHouse) wareHouse.SetActive(!map);
    }
}