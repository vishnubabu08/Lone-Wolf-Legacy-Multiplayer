using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Stat Text References")]
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI deathsText;
    public TextMeshProUGUI winsText;
    public TextMeshProUGUI matchesPlayedText;

    [Header("Optional Derived Stats")]
    public TextMeshProUGUI kdRatioText;    // Kills / Deaths
    public TextMeshProUGUI winRateText;    // Wins / MatchesPlayed %
  

    void OnEnable()
    {
        // Panel might be enabled before FirebaseManager finishes loading
        // (e.g. first frame after scene load), so wait a frame and retry
        // if data isn't ready yet.
        StartCoroutine(RefreshWhenReady());
    }

    private IEnumerator RefreshWhenReady()
    {
        // Wait until FirebaseManager.instance actually exists.
        while (FirebaseManager.instance == null)
            yield return null;

        RefreshUI();
    }

    // Call this manually any time stats might have changed
    // (e.g. right after EndGame() saves, or when this panel is opened).
    public void RefreshUI()
    {
        FirebaseManager fm = FirebaseManager.instance;
        if (fm == null) return;

        int kills = fm.myKills;
        int deaths = fm.myDeaths;
        int wins = fm.wins;
        int matches = fm.matchesPlayed;

        if (killsText != null) killsText.text = "Total Kills : "+kills.ToString();
        if (deathsText != null) deathsText.text = "Total Death : "+ deaths.ToString();
        if (winsText != null) winsText.text = "Total Win : " + wins.ToString();
        if (matchesPlayedText != null) matchesPlayedText.text = "Matches Played : " + matches.ToString();
        

        if (kdRatioText != null)
        {
            // Avoid divide-by-zero: treat 0 deaths as a KD equal to kills
            float kd = deaths > 0 ? (float)kills / deaths : kills;
            kdRatioText.text = "K/D : " + kd.ToString("0.00");
        }

        if (winRateText != null)
        {
            float rate = matches > 0 ? ((float)wins / matches) * 100f : 0f;
            winRateText.text = "Win Rate : "+ rate.ToString("0") + "%";
        }
    }
}