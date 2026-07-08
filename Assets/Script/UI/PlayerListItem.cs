using UnityEngine;
using TMPro;

public class PlayerListItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI deathsText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI rankText;  // Add this — shows #1 #2 #3

    public void Setup(string name, int kills, int deaths, int rank)
    {
        // Rank medal
        if (rankText != null)
        {
            if (rank == 1) rankText.text = "🥇";
            else if (rank == 2) rankText.text = "🥈";
            else if (rank == 3) rankText.text = "🥉";
            else rankText.text = "#" + rank;
        }

        if (nameText != null) nameText.text = name;
        if (killsText != null) killsText.text = "KILLS: " + kills;
        if (deathsText != null) deathsText.text = "DEATHS: " + deaths;

        // Coins earned this match
        int coinsEarned = kills * 2;
        if (rank == 1) coinsEarned += 100; // Winner bonus
        if (coinsText != null) coinsText.text = "+" + coinsEarned + " Coins";
    }
}