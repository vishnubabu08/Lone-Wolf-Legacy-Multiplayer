using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance;

    public static bool gameIsLive = false;

    public enum GameMode { Global, Custom }
    [Header("Game Mode")]
    public GameMode currentGameMode = GameMode.Global;

    [Header("Player & Spawn")]
    public GameObject player;
    public Transform[] spawnPoints;
    public Transform[] spawnPoints1;

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
    public GameObject gameOverPanel;
    public Transform top3Container;
    public GameObject playerRowPrefab;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI myStatsText;

    [Header("Lobby - Custom Room UI")]
    public GameObject customRoomPanel;
    public TMP_InputField roomNameInput;
    public TMP_InputField maxPlayersInput;
    public TMP_Dropdown timeSelectDropdown;

    [Header("Host Only UI (hide for joiners)")]
    public GameObject hostOnlyPanel;  // Parent panel containing maxPlayers + time
  //  private Toggle isHostToggle;       // "I am creating the room" toggle

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
    private bool isHost = false;

    private double preGameEndTime = 0;
    private double matchEndTime = 0;

    [Header("Config")]
    public float preGameLength = 10f;
    public float defaultMatchLength = 600f;
    public int minPlayersToStart = 1;

    private float matchLengthInSeconds;
    private string nickName = "unnamed";

    public int kills = 0;
    public int deaths = 0;

    private const string PROP_MAP = "Map";
    private const string PROP_MATCH_STATE = "MatchState";
    private const string PROP_PRE_END = "PreGameEnd";
    private const string PROP_MATCH_END = "MatchEnd";
    private const string PROP_MATCH_LENGTH = "MatchLength";

    private int joinAttemptIndex = 0;
    private bool attemptingJoin = false;

    private void Awake()
    {
        instance = this;
        gameIsLive = false;

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (gameModeDropdown != null)
        {
            gameModeDropdown.onValueChanged.RemoveAllListeners();
            gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            OnGameModeChanged(gameModeDropdown.value);
        }

     /*   // Wire host toggle
        if (isHostToggle != null)
        {
            isHostToggle.onValueChanged.RemoveAllListeners();
            isHostToggle.onValueChanged.AddListener(SetAsHost);
            SetAsHost(false); // Default: joiner mode
        }
*/
        ChangeNickname();

        if (startTimerText != null) startTimerText.gameObject.SetActive(false);
        if (matchTimerText != null) matchTimerText.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (customRoomPanel != null) customRoomPanel.SetActive(false);
    }

    IEnumerator Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
        {
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start";
            yield break;
        }

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        if (!PhotonNetwork.IsConnected)
        {
            yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // =============================================
    // HOST TOGGLE
    // =============================================
    public void SetAsHost(bool hostMode)
    {
        isHost = hostMode;

        // Show host-only settings (max players + time) only for host
        if (hostOnlyPanel != null)
            hostOnlyPanel.SetActive(hostMode);

        Debug.Log("Host mode: " + hostMode);
    }

    // =============================================
    // GAME MODE DROPDOWN
    // =============================================
    public void OnGameModeChanged(int idx)
    {
        Debug.Log("Game mode changed to index: " + idx);

        // Always reset host state when switching modes
        isHost = false;
     //   if (isHostToggle != null) isHostToggle.isOn = false;

        if (idx == 0)
        {
            currentGameMode = GameMode.Global;
            if (customRoomPanel) customRoomPanel.SetActive(false);
            if (hostOnlyPanel) hostOnlyPanel.SetActive(false);
        }
        else
        {
            currentGameMode = GameMode.Custom;
            if (customRoomPanel) customRoomPanel.SetActive(true);
            // Show host panel by default for custom room
            if (hostOnlyPanel) hostOnlyPanel.SetActive(true);
        }
    }
    // =============================================
    // START BUTTON
    // =============================================
    private void OnStartButtonClicked()
    {
        int dropdownValue = 0;
        if (gameModeDropdown != null)
            dropdownValue = gameModeDropdown.value;

        Debug.Log("Start clicked. Dropdown: " + dropdownValue);

        if (dropdownValue == 0)
        {
            // Global match
            currentGameMode = GameMode.Global;
            joinAttemptIndex = 0;
            attemptingJoin = true;
            StartCoroutine(TryJoinOrCreateMapRoomCoroutine());
        }
        else
        {
            // Custom room
            currentGameMode = GameMode.Custom;

            if (roomNameInput == null || string.IsNullOrEmpty(roomNameInput.text.Trim()))
            {
                ShowStatus("Please enter a room name!");
                return;
            }

            // Try to JOIN first — if room doesn't exist, CREATE it
            // No toggle needed — joining handles both cases
            string roomName = roomNameInput.text.Trim();
            Debug.Log("Attempting to join room: " + roomName);

            PhotonNetwork.JoinRoom(roomName);

            if (startButtonText) startButtonText.text = "Connecting...";
            if (startButton) startButton.interactable = false;
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.Log("Join failed: " + returnCode + " - " + message);

        if (attemptingJoin)
        {
            joinAttemptIndex++;
        }
        else
        {
            // Room doesn't exist — this player becomes the host and creates it
            Debug.Log("Room not found — becoming host and creating room...");
            CreateCustomRoom();
        }
    }

    // =============================================
    // HOST CREATES ROOM
    // =============================================
    void CreateCustomRoom()
    {
        string roomName = roomNameInput.text.Trim();

        // Read max players
        byte maxPlayers = 4;
        if (maxPlayersInput != null && !string.IsNullOrEmpty(maxPlayersInput.text))
        {
            byte.TryParse(maxPlayersInput.text, out maxPlayers);
            maxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 20);
        }

        // Read match time
        int selectedMinutes = 10;
        if (timeSelectDropdown != null)
        {
            string optionText = timeSelectDropdown.options[timeSelectDropdown.value].text;
            int.TryParse(optionText, out selectedMinutes);
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = maxPlayers;
        options.IsVisible = false;
        options.IsOpen = true;

        Hashtable roomProps = new Hashtable();
        roomProps.Add(PROP_MATCH_LENGTH, selectedMinutes * 60);
        roomProps.Add(PROP_MAP, Map1 ? "Map1" : "Map2");
        roomProps.Add(PROP_MATCH_STATE, "Waiting");

        options.CustomRoomProperties = roomProps;
        options.CustomRoomPropertiesForLobby = new string[]
        {
        PROP_MATCH_LENGTH, PROP_MATCH_STATE, PROP_MAP
        };

        Debug.Log("Creating room: " + roomName +
                  " | Max: " + maxPlayers +
                  " | Time: " + selectedMinutes + " mins");

        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);

        if (startButtonText) startButtonText.text = "Creating...";
        if (startButton) startButton.interactable = false;
    }

    // =============================================
    // FRIEND JOINS ROOM BY NAME
    // =============================================
    void JoinCustomRoom()
    {
        string roomName = roomNameInput.text.Trim();

        Debug.Log("Joining room: " + roomName);

        PhotonNetwork.JoinRoom(roomName);

        if (startButtonText) startButtonText.text = "Joining...";
        if (startButton) startButton.interactable = false;
    }

    // =============================================
    // GLOBAL MATCHMAKING
    // =============================================
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

    // =============================================
    // PHOTON CALLBACKS
    // =============================================
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        if (startButton != null) startButton.interactable = true;
        if (startButtonText != null) startButtonText.text = "Start";
        Debug.Log("Joined Lobby. Ready.");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        attemptingJoin = false;
        StopAllCoroutines();

        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
                  " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
                  " | Max: " + PhotonNetwork.CurrentRoom.MaxPlayers);

        // For global rooms only — reject if match already started
        string roomName = PhotonNetwork.CurrentRoom.Name;
        bool isGlobalRoom = roomName.StartsWith("Global_Map1") ||
                            roomName.StartsWith("Global_Map2");

        if (isGlobalRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_STATE))
            {
                string state = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_STATE] as string;
                if (state == "Started")
                {
                    if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_END))
                    {
                        double me = (double)PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_END];
                        if (PhotonNetwork.Time < me)
                        {
                            PhotonNetwork.LeaveRoom();
                            joinAttemptIndex++;
                            return;
                        }
                    }
                    else
                    {
                        PhotonNetwork.LeaveRoom();
                        joinAttemptIndex++;
                        return;
                    }
                }
            }
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

   /* public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.LogWarning("Join room failed: " + returnCode + " - " + message);

        if (attemptingJoin)
        {
            joinAttemptIndex++;
        }
        else
        {
            // Custom room join failed
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start";
            ShowStatus("Room not found! Ask your friend for the correct room name.");
        }
    }
*/
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.LogWarning("Create room failed: " + returnCode + " - " + message);

        if (!attemptingJoin)
        {
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start";

            // Room name already taken
            if (returnCode == 32766)
                ShowStatus("Room name already exists! Choose another name.");
            else
                ShowStatus("Failed to create room: " + message);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("Player joined: " + newPlayer.NickName);
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

    // =============================================
    // UPDATE LOOP
    // =============================================
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
                    startTimerText.text = "Match Starting in: " +
                                          remaining.ToString("F1") + "s";
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

    // =============================================
    // MATCH START LOGIC
    // =============================================
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
        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

        string roomName = PhotonNetwork.CurrentRoom.Name;
        bool isGlobalRoom = roomName.StartsWith("Global_Map1") ||
                            roomName.StartsWith("Global_Map2");

        if (isGlobalRoom)
        {
            // Global — start when min players reached
            if (players >= minPlayersToStart)
            {
                TriggerPreGameCountdown();
            }
        }
        else
        {
            // Custom — wait until room is completely full
            if (players >= maxPlayers)
            {
                TriggerPreGameCountdown();
            }
            else
            {
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "Waiting for friends: " +
                                      players + "/" + maxPlayers +
                                      "\nRoom: " + roomName;
                }
            }
        }
    }

    void TriggerPreGameCountdown()
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

    void UpdateStatusText()
    {
        if (statusText == null) return;

        string roomName = PhotonNetwork.CurrentRoom != null ?
                          PhotonNetwork.CurrentRoom.Name : "";
        bool isGlobalRoom = roomName.StartsWith("Global_Map1") ||
                            roomName.StartsWith("Global_Map2");

        bool isWaiting = true;
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_STATE))
        {
            string st = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_STATE] as string;
            isWaiting = st == "Waiting";
        }

        if (isWaiting && !isPreGameCountdown && !isMatchLive)
        {
            statusText.gameObject.SetActive(true);

            if (isGlobalRoom)
            {
                statusText.text = "Waiting: " +
                    PhotonNetwork.CurrentRoom.PlayerCount + "/" +
                    PhotonNetwork.CurrentRoom.MaxPlayers;
            }
            else
            {
                statusText.text = "Waiting for friends: " +
                    PhotonNetwork.CurrentRoom.PlayerCount + "/" +
                    PhotonNetwork.CurrentRoom.MaxPlayers +
                    "\nRoom: " + roomName;
            }
        }
        else if (isPreGameCountdown) statusText.text = "Get Ready!";
        else if (isMatchLive) statusText.text = "Match Live";
    }

    // =============================================
    // GAME START
    // =============================================
    public void StartGame()
    {
        if (playerSpawned) return;

        playerSpawned = true;
        isPreGameCountdown = false;
        gameIsLive = true;

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

    // =============================================
    // GAME END
    // =============================================
    void EndGame()
    {
        gameIsLive = false;

        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            SceneManager.LoadScene("3_Lobby");
            return;
        }

        isMatchLive = false;
        if (matchTimerText != null) matchTimerText.text = "GAME OVER";

        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.matchesPlayed++;
            int newTotalKills = FirebaseManager.instance.myKills + kills;
            int newTotalDeaths = FirebaseManager.instance.myDeaths + deaths;
            int earnedCoins = kills * 2;
            int newTotalCoins = FirebaseManager.instance.myCoins + earnedCoins;

            FirebaseManager.instance.SaveData(
                FirebaseManager.instance.myName,
                newTotalKills, newTotalDeaths, newTotalCoins,
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

        ShowGameOverUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowGameOverUI()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (roomCam != null) roomCam.SetActive(true);
        if (playerObject != null) playerObject.SetActive(false);

        var sortedPlayers = PhotonNetwork.PlayerList.OrderByDescending(p =>
            p.CustomProperties.ContainsKey("kills") ?
            (int)p.CustomProperties["kills"] : 0
        ).ToList();

        if (top3Container != null)
            foreach (Transform child in top3Container)
                Destroy(child.gameObject);

        int count = 0;
        foreach (var p in sortedPlayers)
        {
            if (count >= 3) break;

            int pKills = p.CustomProperties.ContainsKey("kills") ?
                         (int)p.CustomProperties["kills"] : 0;
            int pDeaths = p.CustomProperties.ContainsKey("deaths") ?
                          (int)p.CustomProperties["deaths"] : 0;

            if (playerRowPrefab != null && top3Container != null)
            {
                GameObject row = Instantiate(playerRowPrefab, top3Container);
                PlayerListItem item = row.GetComponent<PlayerListItem>();
                if (item != null) item.Setup(p.NickName, pKills, pDeaths, count + 1);
            }

            if (count == 0 && winnerText != null)
                winnerText.text = "WINNER\n" + p.NickName;

            count++;
        }

        if (myStatsText != null)
        {
            int myRank = sortedPlayers.IndexOf(PhotonNetwork.LocalPlayer) + 1;
            int myKills = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("kills")
                ? (int)PhotonNetwork.LocalPlayer.CustomProperties["kills"] : 0;
            myStatsText.text = "RANK: #" + myRank + " | KILLS: " + myKills;
        }
    }

    // =============================================
    // HELPERS
    // =============================================
    void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
        }
        Debug.Log("Status: " + message);
    }

    public void LeaveMatch()
    {
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
        GameObject _player = PhotonNetwork.Instantiate(
            player.name, spawnPoint.position, Quaternion.identity);
        SetupPlayer(_player);
    }

    public void MapSpawnPlayer1()
    {
        if (spawnPoints1 == null || spawnPoints1.Length == 0) return;
        Transform spawnPoint = spawnPoints1[Random.Range(0, spawnPoints1.Length)];
        GameObject _player = PhotonNetwork.Instantiate(
            player.name, spawnPoint.position, Quaternion.identity);
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
        if (FirebaseManager.instance != null &&
            !string.IsNullOrEmpty(FirebaseManager.instance.myName))
            nickName = FirebaseManager.instance.myName;
        else
            nickName = "Guest_" + Random.Range(1000, 9999);

        if (PhotonNetwork.LocalPlayer != null)
            PhotonNetwork.LocalPlayer.NickName = nickName;
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

    public void MapUIActive(bool isActive)
    {
        if (MapSelectionUI) MapSelectionUI.SetActive(isActive);
    }

    public void choosMap(bool map)
    {
        Map1 = map;
        Map2 = !map;
        if (floodGround) floodGround.SetActive(map);
        if (wareHouse) wareHouse.SetActive(!map);
    }
}