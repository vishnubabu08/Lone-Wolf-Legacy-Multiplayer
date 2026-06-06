using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class ArmorSystem : MonoBehaviourPun, IPunObservable
{
    [Header("Helmet Models (same order as shop)")]
    public GameObject[] helmetModels;

    [Header("Vest Models (same order as shop)")]
    public GameObject[] vestModels;

    [Header("Helmet HP Values (same order as shop)")]
    public int[] helmetHPValues;   // e.g. { 40, 70, 100 }

    [Header("Vest HP Values (same order as shop)")]
    public int[] vestHPValues;     // e.g. { 60, 100, 150 }

    [Header("UI")]
    public Slider helmetSlider;
    public Slider vestSlider;
    public GameObject helmetUIPanel;
    public GameObject vestUIPanel;
    public Image helmetFill;
    public Image vestFill;

    // Runtime values
    private int helmetHP;
    private int vestHP;
    private int helmetMaxHP;
    private int vestMaxHP;

    private int activeHelmetIndex = -1;
    private int activeVestIndex = -1;

    private bool helmetBroken = false;
    private bool vestBroken = false;

    private void Awake()
    {
        helmetHP = 0;
        vestHP = 0;
    }

    private void Update()
    {
        UpdateArmorUI();
    }

    // =============================================
    // CALLED BY PlayerSetup.SyncCostume
    // =============================================
    public void SetActiveArmor(int helmetIndex, int vestIndex)
    {
        activeHelmetIndex = helmetIndex;
        activeVestIndex = vestIndex;
        helmetBroken = false;
        vestBroken = false;

        // Read HP directly from int array
        if (helmetIndex >= 0 && helmetIndex < helmetHPValues.Length)
        {
            helmetMaxHP = helmetHPValues[helmetIndex];
            helmetHP = helmetMaxHP;
            if (helmetSlider != null) helmetSlider.maxValue = helmetMaxHP;
        }
        else
        {
            helmetMaxHP = 0;
            helmetHP = 0;
        }

        if (vestIndex >= 0 && vestIndex < vestHPValues.Length)
        {
            vestMaxHP = vestHPValues[vestIndex];
            vestHP = vestMaxHP;
            if (vestSlider != null) vestSlider.maxValue = vestMaxHP;
        }
        else
        {
            vestMaxHP = 0;
            vestHP = 0;
        }
    }

    // =============================================
    // DAMAGE ABSORPTION
    // =============================================
    public int AbsorbDamage(int incomingDamage)
    {
        int remainingDamage = incomingDamage;

        // Helmet absorbs 30%
        if (activeHelmetIndex >= 0 && !helmetBroken && helmetHP > 0)
        {
            int helmetAbsorb = Mathf.CeilToInt(incomingDamage * 0.30f);
            helmetAbsorb = Mathf.Min(helmetAbsorb, helmetHP);

            helmetHP -= helmetAbsorb;
            remainingDamage -= helmetAbsorb;

            if (helmetHP <= 0)
            {
                helmetHP = 0;
                helmetBroken = true;
                photonView.RPC("RPC_BreakArmor", RpcTarget.All, "Helmet");
            }
        }

        // Vest absorbs 40%
        if (activeVestIndex >= 0 && !vestBroken && vestHP > 0)
        {
            int vestAbsorb = Mathf.CeilToInt(incomingDamage * 0.40f);
            vestAbsorb = Mathf.Min(vestAbsorb, vestHP);

            vestHP -= vestAbsorb;
            remainingDamage -= vestAbsorb;

            if (vestHP <= 0)
            {
                vestHP = 0;
                vestBroken = true;
                photonView.RPC("RPC_BreakArmor", RpcTarget.All, "Vest");
            }
        }

        return Mathf.Max(remainingDamage, 0);
    }

    // =============================================
    // BREAK VISUAL
    // =============================================
    [PunRPC]
    void RPC_BreakArmor(string armorType)
    {
        if (armorType == "Helmet")
        {
            if (activeHelmetIndex >= 0 && activeHelmetIndex < helmetModels.Length)
                if (helmetModels[activeHelmetIndex] != null)
                    helmetModels[activeHelmetIndex].SetActive(false);
        }
        else if (armorType == "Vest")
        {
            if (activeVestIndex >= 0 && activeVestIndex < vestModels.Length)
                if (vestModels[activeVestIndex] != null)
                    vestModels[activeVestIndex].SetActive(false);
        }
    }

    // =============================================
    // UI UPDATE
    // =============================================
    void UpdateArmorUI()
    {
        bool showHelmet = activeHelmetIndex >= 0 && !helmetBroken && helmetMaxHP > 0;
        bool showVest = activeVestIndex >= 0 && !vestBroken && vestMaxHP > 0;

        if (helmetUIPanel != null) helmetUIPanel.SetActive(showHelmet);
        if (vestUIPanel != null) vestUIPanel.SetActive(showVest);

        if (helmetSlider != null) helmetSlider.value = helmetHP;
        if (vestSlider != null) vestSlider.value = vestHP;

        if (helmetFill != null && helmetMaxHP > 0)
            helmetFill.color = Color.Lerp(Color.red, Color.green, (float)helmetHP / helmetMaxHP);

        if (vestFill != null && vestMaxHP > 0)
            vestFill.color = Color.Lerp(Color.red, Color.green, (float)vestHP / vestMaxHP);
    }

    // =============================================
    // PHOTON SYNC
    // =============================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(helmetHP);
            stream.SendNext(vestHP);
            stream.SendNext(helmetBroken);
            stream.SendNext(vestBroken);
            stream.SendNext(activeHelmetIndex);
            stream.SendNext(activeVestIndex);
            stream.SendNext(helmetMaxHP);
            stream.SendNext(vestMaxHP);
        }
        else
        {
            helmetHP = (int)stream.ReceiveNext();
            vestHP = (int)stream.ReceiveNext();
            helmetBroken = (bool)stream.ReceiveNext();
            vestBroken = (bool)stream.ReceiveNext();
            activeHelmetIndex = (int)stream.ReceiveNext();
            activeVestIndex = (int)stream.ReceiveNext();
            helmetMaxHP = (int)stream.ReceiveNext();
            vestMaxHP = (int)stream.ReceiveNext();
        }
    }
}