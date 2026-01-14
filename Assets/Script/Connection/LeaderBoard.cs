using UnityEngine;
using System.Linq;
using System.Collections.Generic; // Required for List<>
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using TMPro;

public class LeaderBoard : MonoBehaviour
{
    public GameObject leaderboardUI;

    [Header("Options")]
    public float refreshRate = 0.5f;

    [Header("UI")]
    public GameObject[] slots;
    public TextMeshProUGUI[] nameText;
    public TextMeshProUGUI[] kdText;
    public TextMeshProUGUI[] scoreText;

    public GameObject wholeUI;
    public TextMeshProUGUI localPlayerNameText;
    public TextMeshProUGUI localPlayerKDtext;

    // A simple class to hold data for BOTH Humans and Bots
    public class PlayerData
    {
        public string name;
        public int score;
        public int kills;
        public int deaths;
        public bool isLocal;
    }

    private void Start()
    {
        if (wholeUI) wholeUI.SetActive(false);
        if (leaderboardUI) leaderboardUI.SetActive(false);

        // Refresh every X seconds
        InvokeRepeating(nameof(Refresh), 1f, refreshRate);
    }

    public void Refresh()
    {
        // 1. Reset all slots
        foreach (var slot in slots)
        {
            slot.SetActive(false);
        }

        // 2. Create a temporary list to hold EVERYONE (Bots + Humans)
        List<PlayerData> allPlayers = new List<PlayerData>();

        // --- A. GET HUMAN PLAYERS ---
        foreach (var player in PhotonNetwork.PlayerList)
        {
            PlayerData p = new PlayerData();
            p.name = string.IsNullOrEmpty(player.NickName) ? "Unnamed" : player.NickName;
            p.score = player.GetScore();
            p.isLocal = player.IsLocal;

            // Safely get Kills/Deaths from Custom Properties
            if (player.CustomProperties.ContainsKey("kills"))
                p.kills = (int)player.CustomProperties["kills"];
            else
                p.kills = 0;

            if (player.CustomProperties.ContainsKey("deaths"))
                p.deaths = (int)player.CustomProperties["deaths"];
            else
                p.deaths = 0;

            allPlayers.Add(p);
        }

        // --- B. GET BOTS ---
        // Find all bots currently in the scene
        BotController[] activeBots = FindObjectsOfType<BotController>();

        foreach (var bot in activeBots)
        {
            // Only show alive bots or bots that haven't been destroyed yet
            PlayerData b = new PlayerData();
            b.name = bot.botName; // Uses the name we added to BotController
            b.score = bot.score;
            b.kills = bot.kills;
            b.deaths = bot.deaths;
            b.isLocal = false; // Bots are never "Local"

            allPlayers.Add(b);
        }

        // 3. SORT LIST (Highest Score First)
        // using LINQ to sort descending
        var sortedList = allPlayers.OrderByDescending(x => x.score).ToList();

        // 4. DISPLAY IN UI
        int i = 0;
        foreach (var player in sortedList)
        {
            // Stop if we run out of slots in the UI
            if (i >= slots.Length) break;

            slots[i].SetActive(true);

            nameText[i].text = player.name;
            scoreText[i].text = player.score.ToString();
            kdText[i].text = $"{player.kills}/{player.deaths}";

            // Update the small HUD for the Local Player
            if (player.isLocal)
            {
                if (localPlayerNameText) localPlayerNameText.text = player.name;
                if (localPlayerKDtext) localPlayerKDtext.text = $"K/D {player.kills}/{player.deaths}";
            }

            i++;
        }
    }

    private void Update()
    {
        // Hide/Show logic
        if (RoomManager.instance != null && wholeUI != null)
        {
            wholeUI.SetActive(RoomManager.instance.playerSpawned);
        }

        // Hold TAB to see leaderboard
        if (leaderboardUI != null)
        {
            leaderboardUI.SetActive(Input.GetKey(KeyCode.Tab));
        }
    }
}