using UnityEngine;
using Photon.Pun;

public class BotAppearance : MonoBehaviourPun
{
    [Header("Customization Options")]
    // Drag all your models here in the Inspector
    public GameObject[] heads;
    public GameObject[] helmets;
    public GameObject[] vests;
    public GameObject[] guns;

    [Header("References")]
    // We need to tell the BotController which gun barrel to use for shooting
    public BotController botController;
    public Transform[] gunBarrels; // Drag the "FirePoint" of each gun here

    void Start()
    {
        // 1. Only the Master Client decides the outfit
        // This prevents every player from seeing a different outfit for the same bot
        if (PhotonNetwork.IsMasterClient)
        {
            int rHead = Random.Range(0, heads.Length);
            int rHelm = Random.Range(0, helmets.Length); // Random.Range for int is exclusive on max, but let's be safe
            // Actually Random.Range(0, length) is correct for arrays
            int rVest = Random.Range(0, vests.Length);
            int rGun = Random.Range(0, guns.Length);

            // 2. Tell everyone (RPC) what this bot is wearing
            photonView.RPC("SyncBotOutfit", RpcTarget.AllBuffered, rHead, rHelm, rVest, rGun);
        }
    }

    [PunRPC]
    public void SyncBotOutfit(int headIndex, int helmIndex, int vestIndex, int gunIndex)
    {
        // Helper function to turn off all, turn on one
        ActivateItem(heads, headIndex);
        ActivateItem(helmets, helmIndex);
        ActivateItem(vests, vestIndex);
        ActivateItem(guns, gunIndex);

        // Update the BotController so it shoots from the correct gun barrel
        if (botController != null && gunBarrels.Length > gunIndex)
        {
            botController.gunBarrel = gunBarrels[gunIndex];
        }
    }

    void ActivateItem(GameObject[] list, int index)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
            {
                list[i].SetActive(i == index);
            }
        }
    }
}