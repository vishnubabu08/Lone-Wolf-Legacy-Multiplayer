using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using System.Collections.Generic;

public class GameAnalytics : MonoBehaviour
{
    public static GameAnalytics instance;

    async void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 1. Initialize Unity Services
            await UnityServices.InitializeAsync();

            // 2. Start Data Collection
            AnalyticsService.Instance.StartDataCollection();

            Debug.Log("Unity Analytics Initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- EVENT 1: MATCH FINISHED ---
    public void LogMatchFinished(int kills, int deaths, string mapName)
    {
        // FIX: Use 'CustomEvent' instead of 'Dictionary'
        CustomEvent myEvent = new CustomEvent("match_finished")
        {
            { "kills", kills },
            { "deaths", deaths },
            { "mapName", mapName },
            { "duration", Time.timeSinceLevelLoad }
        };

        // Send to Cloud
        AnalyticsService.Instance.RecordEvent(myEvent);
        Debug.Log("Analytics: Match Finished Logged");
    }

    // --- EVENT 2: ITEM BOUGHT ---
    public void LogItemBought(string itemName, int cost)
    {
        // FIX: Use 'CustomEvent'
        CustomEvent myEvent = new CustomEvent("item_purchased")
        {
            { "item_name", itemName },
            { "cost", cost }
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
    }

    // --- EVENT 3: GAME START ---
    public void LogGameStart(string mode)
    {
        // FIX: Use 'CustomEvent'
        CustomEvent myEvent = new CustomEvent("game_start")
        {
            { "mode", mode }
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
    }
}