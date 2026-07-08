using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun.UtilityScripts;

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
    public Button cancelButton;

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
    public GameObject hostOnlyPanel;
    public GameObject MiniMap;
    public GameObject RooMPanel;

    [Header("Lobby - Mode Selection")]
    public TMP_Dropdown gameModeDropdown;

    [Header("Dropdown Index Config")]
    public int globalModeIndex = 0;
    public int customModeIndex = 1;

    [Header("Map Settings")]
    public GameObject MapSelectionUI;
    public GameObject floodGround;
    public GameObject wareHouse;
    public bool Map1 = true;
    public bool Map2 = false;

    [Header("Config")]
    public float preGameLength = 10f;
    public float defaultMatchLength = 600f;
    public int minPlayersToStart = 1;

    // Flags
    public bool playerSpawned = false;
    private bool isPreGameCountdown = false;
    private bool isMatchLive = false;
    private bool timerHasStarted = false;
    private bool isCustomRoom = false;

    private double preGameEndTime = 0;
    private double matchEndTime = 0;
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

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelCustomRoom);
        }

        if (gameModeDropdown != null)
        {
            gameModeDropdown.onValueChanged.RemoveAllListeners();
            gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            OnGameModeChanged(gameModeDropdown.value);
        }

        ChangeNickname();

        if (startTimerText != null) startTimerText.gameObject.SetActive(false);
        if (matchTimerText != null) matchTimerText.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (customRoomPanel != null) customRoomPanel.SetActive(false);
        if (hostOnlyPanel != null) hostOnlyPanel.SetActive(false);
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
    // DROPDOWN CHANGED
    // =============================================
    public void OnGameModeChanged(int idx)
    {
        Debug.Log("Dropdown index changed to: " + idx);

        if (idx == customModeIndex)
        {
            currentGameMode = GameMode.Custom;
            if (customRoomPanel) customRoomPanel.SetActive(true);
            if (hostOnlyPanel) hostOnlyPanel.SetActive(true);
            Debug.Log("Mode: CUSTOM ROOM");
        }
        else
        {
            currentGameMode = GameMode.Global;
            if (customRoomPanel) customRoomPanel.SetActive(false);
            if (hostOnlyPanel) hostOnlyPanel.SetActive(false);
            Debug.Log("Mode: GLOBAL MATCH");
        }
    }

    // =============================================
    // START BUTTON
    // =============================================
    private void OnStartButtonClicked()
    {
        int dropdownValue = gameModeDropdown != null ? gameModeDropdown.value : 0;

        Debug.Log("=== START CLICKED ===");
        Debug.Log("Dropdown value: " + dropdownValue);
        Debug.Log("globalModeIndex: " + globalModeIndex);
        Debug.Log("customModeIndex: " + customModeIndex);
        Debug.Log("currentGameMode: " + currentGameMode);

        if (dropdownValue == globalModeIndex)
        {
            Debug.Log("Starting GLOBAL match");
            currentGameMode = GameMode.Global;
            isCustomRoom = false;
            joinAttemptIndex = 0;
            attemptingJoin = true;
            StartCoroutine(TryJoinOrCreateMapRoomCoroutine());
        }
        else if (dropdownValue == customModeIndex)
        {
            Debug.Log("Starting CUSTOM room");
            currentGameMode = GameMode.Custom;
            isCustomRoom = true;

            if (roomNameInput == null || string.IsNullOrEmpty(roomNameInput.text.Trim()))
            {
                ShowStatus("Please enter a room name!");
                return;
            }

            string roomName = roomNameInput.text.Trim();
            Debug.Log("Trying to join room: " + roomName);

            PhotonNetwork.JoinRoom(roomName);

            if (startButtonText) startButtonText.text = "Connecting...";
            if (startButton) startButton.interactable = false;
        }
    }

    // =============================================
    // CANCEL CUSTOM ROOM
    // =============================================
    public void CancelCustomRoom()
    {
        Debug.Log("Cancelling custom room...");

        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
            PhotonNetwork.LeaveRoom();
            // OnLeftRoom handles UI reset
        }
        else
        {
            ResetToLobbyUI();
        }
    }

    // =============================================
    // RESET LOBBY UI
    // =============================================
    void ResetToLobbyUI()
    {
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = true;
        }
        if (startButtonText != null) startButtonText.text = "Start";
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        if (gameModeDropdown != null) gameModeDropdown.gameObject.SetActive(true);

        int idx = gameModeDropdown != null ? gameModeDropdown.value : 0;
        if (idx == customModeIndex)
        {
            if (customRoomPanel != null) customRoomPanel.SetActive(true);
            if (hostOnlyPanel != null) hostOnlyPanel.SetActive(true);
        }
        else
        {
            if (customRoomPanel != null) customRoomPanel.SetActive(false);
            if (hostOnlyPanel != null) hostOnlyPanel.SetActive(false);
        }

        isCustomRoom = false;
        attemptingJoin = false;
        timerHasStarted = false;
        playerSpawned = false;
        isPreGameCountdown = false;
        isMatchLive = false;

        Debug.Log("Lobby UI reset.");
    }

    // =============================================
    // JOIN FAILED
    // =============================================
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.Log("Join failed: " + returnCode + " - " + message);

        if (attemptingJoin)
        {
            joinAttemptIndex++;
        }
        else if (isCustomRoom)
        {
            Debug.Log("Room not found — creating as host...");
            CreateCustomRoom();
        }
        else
        {
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start";
            ShowStatus("Failed to join: " + message);
        }
    }

    // =============================================
    // CREATE CUSTOM ROOM
    // =============================================
    void CreateCustomRoom()
    {
        string roomName = roomNameInput.text.Trim();

        byte maxPlayers = 4;
        if (maxPlayersInput != null && !string.IsNullOrEmpty(maxPlayersInput.text))
        {
            byte.TryParse(maxPlayersInput.text, out maxPlayers);
            maxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 20);
        }

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

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.LogWarning("Create room failed: " + returnCode + " - " + message);

        if (startButton != null) startButton.interactable = true;
        if (startButtonText != null) startButtonText.text = "Start";

        if (returnCode == 32766)
            ShowStatus("Room already exists! Try joining it.");
        else
            ShowStatus("Failed to create room: " + message);
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
                    Debug.LogError("Could not find or create a room.");
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

        string roomName = PhotonNetwork.CurrentRoom.Name;
        bool isGlobalRoom = roomName.StartsWith("Global_Map1") ||
                            roomName.StartsWith("Global_Map2");

        Debug.Log("Joined Room: " + roomName +
                  " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
                  " | Max: " + PhotonNetwork.CurrentRoom.MaxPlayers +
                  " | IsGlobal: " + isGlobalRoom);

        // Reject if global room match already started
        if (isGlobalRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_MATCH_STATE))
            {
                string state = PhotonNetwork.CurrentRoom.CustomProperties[PROP_MATCH_STATE] as string;
                if (state == "Started")
                {
                    PhotonNetwork.LeaveRoom();
                    joinAttemptIndex++;
                    return;
                }
            }
        }

        if (MapSelectionUI) MapSelectionUI.SetActive(false);
        if (customRoomPanel) customRoomPanel.SetActive(false);
        if (hostOnlyPanel) hostOnlyPanel.SetActive(false);
        if (gameModeDropdown) gameModeDropdown.gameObject.SetActive(false);
        if (startButton) startButton.gameObject.SetActive(false);

        // Show cancel button only for custom rooms
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(!isGlobalRoom);

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

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("Player joined: " + newPlayer.NickName +
                  " | Total: " + PhotonNetwork.CurrentRoom.PlayerCount);
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
    // UPDATE
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

        Debug.Log("CheckPlayerCountAndStart — isGlobal: " + isGlobalRoom +
                  " | players: " + players + "/" + maxPlayers);

        if (isGlobalRoom && cancelButton != null)
            cancelButton.gameObject.SetActive(false);

        if (isGlobalRoom)
        {
            if (players >= minPlayersToStart)
                TriggerPreGameCountdown();
        }
        else
        {
            if (players >= maxPlayers)
            {
                if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                Debug.Log("Custom room full — starting countdown!");
                TriggerPreGameCountdown();
            }
            else
            {
                if (cancelButton != null) cancelButton.gameObject.SetActive(true);

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
        if (timerHasStarted) return;
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_PRE_END)) return;

        timerHasStarted = true;
        double preGameEnd = PhotonNetwork.Time + preGameLength;
        Hashtable props = new Hashtable { { PROP_PRE_END, preGameEnd } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        Debug.Log("Countdown triggered. Starts in " + preGameLength + "s");
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
                statusText.text = "Waiting: " +
                    PhotonNetwork.CurrentRoom.PlayerCount + "/" +
                    PhotonNetwork.CurrentRoom.MaxPlayers;
            else
                statusText.text = "Waiting for friends: " +
                    PhotonNetwork.CurrentRoom.PlayerCount + "/" +
                    PhotonNetwork.CurrentRoom.MaxPlayers +
                    "\nRoom: " + roomName;
        }
        else if (isPreGameCountdown) statusText.text = "Get Ready!";
        else if (isMatchLive) statusText.text = "Match Live";
    }
    
    // =============================================
    // START GAME
    // =============================================
    public void StartGame()
    {
        if (playerSpawned) return;

        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        playerSpawned = true;
        isPreGameCountdown = false;
        gameIsLive = true;
        MiniMap.SetActive(true);
        RooMPanel.SetActive(false);

        // =============================================
        // RESET MATCH STATS FOR NEW MATCH
        // =============================================
        kills = 0;
        deaths = 0;

        // Reset Photon custom properties so leaderboard starts fresh
        ExitGames.Client.Photon.Hashtable resetProps = new ExitGames.Client.Photon.Hashtable();
        resetProps["kills"] = 0;
        resetProps["deaths"] = 0;
        PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);

        PhotonNetwork.LocalPlayer.SetScore(0);

        Debug.Log("Match stats reset. kills=0 deaths=0");
        // =============================================

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
    // END GAME
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
            var fm = FirebaseManager.instance;

            // Get match kills
            int propsKills = 0;
            int propsDeaths = 0;

            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("kills"))
                propsKills = (int)PhotonNetwork.LocalPlayer.CustomProperties["kills"];
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("deaths"))
                propsDeaths = (int)PhotonNetwork.LocalPlayer.CustomProperties["deaths"];

            int myMatchKills = Mathf.Max(propsKills, kills);
            int myMatchDeaths = Mathf.Max(propsDeaths, deaths);

            Debug.Log("Match kills: " + myMatchKills +
                      " | Match deaths: " + myMatchDeaths);

            fm.matchesPlayed++;

            // =============================================
            // WINNER CHECK — beats ALL players AND bots
            // =============================================
            bool isWinner = false;

            if (myMatchKills > 0)
            {
                bool anyoneHasMoreKills = false;

                // Check bots
                BotController[] bots = FindObjectsOfType<BotController>();
                foreach (var bot in bots)
                {
                    if (bot.kills > myMatchKills)
                    {
                        anyoneHasMoreKills = true;
                        Debug.Log("Lost to bot: " + bot.botName +
                                  " with " + bot.kills + " kills");
                        break;
                    }
                }

                // Check real players
                if (!anyoneHasMoreKills)
                {
                    foreach (var p in PhotonNetwork.PlayerList)
                    {
                        if (p.IsLocal) continue;
                        int pKills = p.CustomProperties.ContainsKey("kills") ?
                                     (int)p.CustomProperties["kills"] : 0;
                        if (pKills > myMatchKills)
                        {
                            anyoneHasMoreKills = true;
                            Debug.Log("Lost to player: " + p.NickName +
                                      " with " + pKills + " kills");
                            break;
                        }
                    }
                }

                isWinner = !anyoneHasMoreKills;
            }

            Debug.Log("isWinner: " + isWinner);
            if (isWinner) fm.wins++;

            // Update stats
            fm.myKills += myMatchKills;
            fm.myDeaths += myMatchDeaths;

            // =============================================
            // COINS — 2 per kill + 100 winner bonus
            // =============================================
            int killCoins = myMatchKills * 2;
            int winnerBonus = isWinner ? 100 : 0;
            int earnedCoins = killCoins + winnerBonus;

            fm.myCoins += earnedCoins;

            Debug.Log("Kill coins: " + killCoins +
                      " | Winner bonus: " + winnerBonus +
                      " | Earned: " + earnedCoins +
                      " | Total: " + fm.myCoins);

            // Check achievements
            if (AchievementManager.instance != null)
            {
                AchievementManager.instance.CheckAchievements(
                    myMatchKills,
                    myMatchDeaths,
                    matchLengthInSeconds,
                    isWinner
                );
            }

            // Save
            fm.SaveData(
                fm.myName,
                fm.myKills,
                fm.myDeaths,
                fm.myCoins,
                fm.headIndex,
                fm.helmetIndex,
                fm.vestIndex,
                fm.headsOwned,
                fm.helmetsOwned,
                fm.vestsOwned,
                fm.primaryGunID,
                fm.secondaryGunID,
                fm.gunsOwned
            );

            Debug.Log("Final saved coins: " + fm.myCoins);
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

        // =============================================
        // BUILD COMBINED LIST — humans + bots
        // =============================================
        List<GameOverPlayerData> allPlayers = new List<GameOverPlayerData>();

        // Add real players
        foreach (var p in PhotonNetwork.PlayerList)
        {
            int pKills = p.CustomProperties.ContainsKey("kills") ?
                         (int)p.CustomProperties["kills"] : 0;
            int pDeaths = p.CustomProperties.ContainsKey("deaths") ?
                          (int)p.CustomProperties["deaths"] : 0;

            allPlayers.Add(new GameOverPlayerData
            {
                name = string.IsNullOrEmpty(p.NickName) ? "Player" : p.NickName,
                kills = pKills,
                deaths = pDeaths,
                isLocal = p.IsLocal,
                isBot = false
            });
        }

        // Add bots
        BotController[] bots = FindObjectsOfType<BotController>();
        foreach (var bot in bots)
        {
            allPlayers.Add(new GameOverPlayerData
            {
                name = bot.botName,
                kills = bot.kills,
                deaths = bot.deaths,
                isLocal = false,
                isBot = true
            });
        }

        // =============================================
        // SORT — most kills first, least deaths as tiebreaker
        // =============================================
        allPlayers.Sort((a, b) =>
        {
            if (b.kills != a.kills)
                return b.kills.CompareTo(a.kills); // Higher kills first
            return a.deaths.CompareTo(b.deaths);   // Lower deaths as tiebreaker
        });

        Debug.Log("Game Over — Total players ranked: " + allPlayers.Count);
        for (int i = 0; i < allPlayers.Count; i++)
            Debug.Log("#" + (i + 1) + " " + allPlayers[i].name +
                      " K:" + allPlayers[i].kills +
                      " D:" + allPlayers[i].deaths);

        // =============================================
        // WINNER TEXT — #1 player
        // =============================================
        if (allPlayers.Count > 0)
        {
            var winner = allPlayers[0];
            if (winnerText != null)
            {
                if (winner.isLocal)
                    winnerText.text = "WINNER\n" + winner.name + "\n🏆 YOU WIN!";
                else
                    winnerText.text = "WINNER\n" + winner.name;
            }
        }

        // =============================================
        // TOP 3 LIST
        // =============================================
        if (top3Container != null)
            foreach (Transform child in top3Container)
                Destroy(child.gameObject);

        int count = Mathf.Min(3, allPlayers.Count);
        for (int i = 0; i < count; i++)
        {
            if (playerRowPrefab == null || top3Container == null) break;

            GameObject row = Instantiate(playerRowPrefab, top3Container);
            PlayerListItem item = row.GetComponent<PlayerListItem>();
            if (item != null)
                item.Setup(allPlayers[i].name, allPlayers[i].kills,
                           allPlayers[i].deaths, i + 1);
        }

        // =============================================
        // MY STATS — find local player rank
        // =============================================
        if (myStatsText != null)
        {
            int myRank = -1;
            int myKills = 0;
            int myDeaths = 0;

            for (int i = 0; i < allPlayers.Count; i++)
            {
                if (allPlayers[i].isLocal)
                {
                    myRank = i + 1;
                    myKills = allPlayers[i].kills;
                    myDeaths = allPlayers[i].deaths;
                    break;
                }
            }

            if (myRank > 0)
                myStatsText.text = "YOUR RANK: #" + myRank +
                                   " | KILLS: " + myKills +
                                   " | DEATHS: " + myDeaths;
            else
                myStatsText.text = "No stats found";
        }
    }

    // =============================================
    // HELPER CLASS for game over ranking
    // =============================================
    private class GameOverPlayerData
    {
        public string name;
        public int kills;
        public int deaths;
        public bool isLocal;
        public bool isBot;
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
        Debug.Log("LeaveMatch called. InRoom: " + PhotonNetwork.InRoom);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // Already on master server — go to lobby directly
            SceneManager.LoadScene("3_Lobby");
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        Debug.Log("Left room. gameIsLive: " + gameIsLive);

        if (gameIsLive)
        {
            // Game was active — go back to lobby scene
            gameIsLive = false;
            SceneManager.LoadScene("3_Lobby");
        }
        else
        {
            // Game hadn't started — cancelled custom room or left lobby
            // Stay on same scene and reset UI
            ResetToLobbyUI();
        }
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