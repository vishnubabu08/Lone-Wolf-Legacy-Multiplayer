using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterShop : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInput;
    public TextMeshProUGUI coinsText;

    [Header("HEAD Section")]
    public TextMeshProUGUI headNameText;
    public Button headBuyButton;
    public TextMeshProUGUI headBuyButtonText;
    public Image headPreviewImage;
    public ItemBlueprint[] heads;

    [Header("HELMET Section")]
    public TextMeshProUGUI helmetNameText;
    public Button helmetBuyButton;
    public TextMeshProUGUI helmetBuyButtonText;
    public Image helmetPreviewImage;
    public ItemBlueprint[] helmets;

    [Header("VEST Section")]
    public TextMeshProUGUI vestNameText;
    public Button vestBuyButton;
    public TextMeshProUGUI vestBuyButtonText;
    public Image vestPreviewImage;
    public ItemBlueprint[] vests;

    // Local Indexes
    int currentHead = 0;
    int currentHelmet = 0;
    int currentVest = 0;

    void Start()
    {
        if (FirebaseManager.instance != null)
        {
            nameInput.text = FirebaseManager.instance.myName;
            currentHead = FirebaseManager.instance.headIndex;
            currentHelmet = FirebaseManager.instance.helmetIndex;
            currentVest = FirebaseManager.instance.vestIndex;

            // --- LOAD OWNERSHIP ---
            LoadOwnership(FirebaseManager.instance.headsOwned, heads);
            LoadOwnership(FirebaseManager.instance.helmetsOwned, helmets);
            LoadOwnership(FirebaseManager.instance.vestsOwned, vests);
        }
        else
        {
            // Debug Mode
            coinsText.text = "Coins: 9999";
        }

        UpdateAllModels();
        UpdateUI();
    }

    // --- HELPER TO READ "1010" STRING ---
    void LoadOwnership(string data, ItemBlueprint[] items)
    {
        if (string.IsNullOrEmpty(data)) return;

        char[] status = data.ToCharArray();

        for (int i = 0; i < items.Length; i++)
        {
            if (i < status.Length)
            {
                items[i].isOwned = (status[i] == '1');
            }
        }
    }

    // --- HELPER TO CREATE "1010" STRING ---
    string CreateOwnershipString(ItemBlueprint[] items)
    {
        string data = "";
        foreach (var item in items)
        {
            data += item.isOwned ? "1" : "0";
        }
        return data;
    }

    void UpdateAllModels()
    {
        for (int i = 0; i < heads.Length; i++) if (heads[i].model) heads[i].model.SetActive(i == currentHead);
        for (int i = 0; i < helmets.Length; i++) if (helmets[i].model) helmets[i].model.SetActive(i == currentHelmet);
        for (int i = 0; i < vests.Length; i++) if (vests[i].model) vests[i].model.SetActive(i == currentVest);

        if (FirebaseManager.instance) coinsText.text = "Coins: " + FirebaseManager.instance.myCoins;
    }

    public void ChangeHead(int dir) { currentHead = GetNextIndex(currentHead, heads.Length, dir); UpdateAllModels(); UpdateUI(); }
    public void ChangeHelmet(int dir) { currentHelmet = GetNextIndex(currentHelmet, helmets.Length, dir); UpdateAllModels(); UpdateUI(); }
    public void ChangeVest(int dir) { currentVest = GetNextIndex(currentVest, vests.Length, dir); UpdateAllModels(); UpdateUI(); }

    int GetNextIndex(int current, int length, int dir)
    {
        current += dir;
        if (current >= length) current = 0;
        if (current < 0) current = length - 1;
        return current;
    }

    public void OnClickHeadAction() { InteractItem(heads, currentHead, "Head"); }
    public void OnClickHelmetAction() { InteractItem(helmets, currentHelmet, "Helmet"); }
    public void OnClickVestAction() { InteractItem(vests, currentVest, "Vest"); }

    void InteractItem(ItemBlueprint[] list, int index, string type)
    {
        if (index == -1) return;
        var fm = FirebaseManager.instance;

        // DEBUG MODE
        if (fm == null)
        {
            list[index].isOwned = true;
            UpdateUI();
            return;
        }

        // REAL MODE
        if (list[index].isOwned)
        {
            UpdateUI();
        }
        else if (fm.myCoins >= list[index].price)
        {
            fm.myCoins -= list[index].price;
            list[index].isOwned = true;
            UpdateUI();
        }
    }

    public void OnConfirmClicked()
    {
        if (FirebaseManager.instance != null)
        {
            // 1. Check HEAD
            if (!heads[currentHead].isOwned)
            {
                currentHead = FirebaseManager.instance.headIndex;
            }

            // 2. Check HELMET (You said this works)
            if (!helmets[currentHelmet].isOwned)
            {
                currentHelmet = FirebaseManager.instance.helmetIndex;
            }

            // 3. Check VEST (MAKE SURE THIS IS HERE)
            // It must check 'vests' array and 'currentVest' index
            if (!vests[currentVest].isOwned)
            {
                Debug.Log("Restoring old vest because new one is not owned.");
                currentVest = FirebaseManager.instance.vestIndex;
            }
        }

        // --- Save Logic Continues Below ---
        string hData = CreateOwnershipString(heads);
        string helmData = CreateOwnershipString(helmets);
        string vData = CreateOwnershipString(vests);

        FirebaseManager.instance.SaveData(
            nameInput.text,
            FirebaseManager.instance.myKills,
            FirebaseManager.instance.myDeaths,
            FirebaseManager.instance.myCoins,
            currentHead, currentHelmet, currentVest,
            hData, helmData, vData,
            FirebaseManager.instance.primaryGunID,
            FirebaseManager.instance.secondaryGunID,
            FirebaseManager.instance.gunsOwned
        );

        SceneManager.LoadScene("3_Lobby");
    }

    void UpdateUI()
    {
        UpdateSectionUI(heads, currentHead, headNameText, headBuyButton, headBuyButtonText, headPreviewImage);
        UpdateSectionUI(helmets, currentHelmet, helmetNameText, helmetBuyButton, helmetBuyButtonText, helmetPreviewImage);
        UpdateSectionUI(vests, currentVest, vestNameText, vestBuyButton, vestBuyButtonText, vestPreviewImage);
    }

    void UpdateSectionUI(ItemBlueprint[] list, int index, TextMeshProUGUI nameTxt, Button btn, TextMeshProUGUI btnTxt, Image previewImg)
    {
        if (index == -1) return;

        nameTxt.text = list[index].itemName;
        btn.gameObject.SetActive(true);

        if (previewImg)
        {
            previewImg.gameObject.SetActive(true);
            previewImg.sprite = list[index].itemSprite;
        }

        if (list[index].isOwned) { btnTxt.text = "EQUIPPED"; btn.interactable = false; }
        else { btnTxt.text = "BUY " + list[index].price; btn.interactable = true; }
    }
}

[System.Serializable]
public class ItemBlueprint
{
    public string itemName;
    public int price;
    public GameObject model;
    public Sprite itemSprite;
    public bool isOwned;
}