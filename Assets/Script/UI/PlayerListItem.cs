using UnityEngine;
using TMPro;

public class PlayerListItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI deathsText;
    public TextMeshProUGUI coinsText;

    public void Setup(string name, int kills, int deaths, int rank)
    {
        nameText.text = name;
        killsText.text = "KILL : " + kills;
        deathsText.text = "DEATH : " + deaths;

        // Logic: Calculate coins based on kills (e.g., 1 Kill = 10 Coins)
        int coinsEarned = kills * 10;

        // Bonus for winning (Rank 1)
        if (rank == 1) coinsEarned += 100;

        coinsText.text = "+" + coinsEarned + " Coins";
    }
}