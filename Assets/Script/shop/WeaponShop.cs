using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WeaponShop : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInput;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gunNameText;
    public Image gunPreviewImage;

    [Header("Buttons")]
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public Button equipSlot1Button;
    public Button equipSlot2Button;
    public TextMeshProUGUI slot1Text;
    public TextMeshProUGUI slot2Text;

    [Header("Data")]
    public ItemBlueprint[] allGuns;

    [Header("3D Preview Models (Drag from Hierarchy)")]
    // Drag the gun objects that are in the Character's Hand here.
    public GameObject[] gunModelsInHand;

    int currentGunIndex = 0;

    void Start()
    {
        if (FirebaseManager.instance != null)
        {
            if (nameInput) nameInput.text = FirebaseManager.instance.myName;
            LoadOwnership(FirebaseManager.instance.gunsOwned, allGuns);
        }
        else
        {
            if (coinsText) coinsText.text = "9999";
        }

        UpdateUI();
    }

    public void NextGun()
    {
        currentGunIndex++;
        if (currentGunIndex >= allGuns.Length) currentGunIndex = 0;
        UpdateUI();
    }

    public void PreviousGun()
    {
        currentGunIndex--;
        if (currentGunIndex < 0) currentGunIndex = allGuns.Length - 1;
        UpdateUI();
    }

    public void OnBuyClicked()
    {
        if (allGuns[currentGunIndex].isOwned) return;

        var fm = FirebaseManager.instance;
        int price = allGuns[currentGunIndex].price;

        if (fm != null)
        {
            if (fm.myCoins >= price)
            {
                fm.myCoins -= price;
                allGuns[currentGunIndex].isOwned = true;

                // Update the "1010" string immediately
                fm.gunsOwned = CreateOwnershipString(allGuns);

                UpdateUI();
            }
        }
    }

    public void OnEquipSlot1Clicked()
    {
        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.primaryGunID = currentGunIndex;
            UpdateUI();
        }
    }

    public void OnEquipSlot2Clicked()
    {
        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.secondaryGunID = currentGunIndex;
            UpdateUI();
        }
    }

    public void OnBackClicked()
    {
        if (FirebaseManager.instance != null)
        {
            var fm = FirebaseManager.instance;

            string gunsOwnedString = CreateOwnershipString(allGuns);

            fm.SaveData(
                fm.myName, fm.myKills, fm.myDeaths, fm.myCoins,
                fm.headIndex, fm.helmetIndex, fm.vestIndex,
                fm.headsOwned, fm.helmetsOwned, fm.vestsOwned,
                fm.primaryGunID,
                fm.secondaryGunID,
                gunsOwnedString
            );
        }

        SceneManager.LoadScene("3_Lobby");
    }

    void UpdateUI()
    {
        ItemBlueprint currentGun = allGuns[currentGunIndex];

        // 1. Basic Info
        gunNameText.text = currentGun.itemName;
        if (gunPreviewImage) gunPreviewImage.sprite = currentGun.itemSprite;
        if (FirebaseManager.instance) coinsText.text = "Coins: " + FirebaseManager.instance.myCoins;

        // 2. Update 3D Model
        if (gunModelsInHand != null && gunModelsInHand.Length > 0)
        {
            for (int i = 0; i < gunModelsInHand.Length; i++)
            {
                if (gunModelsInHand[i] != null)
                {
                    gunModelsInHand[i].SetActive(i == currentGunIndex);
                }
            }
        }

        // 3. Button Logic
        if (currentGun.isOwned)
        {
            buyButton.gameObject.SetActive(false);
            equipSlot1Button.gameObject.SetActive(true);
            equipSlot2Button.gameObject.SetActive(true);

            bool isSlot1 = (FirebaseManager.instance != null && FirebaseManager.instance.primaryGunID == currentGunIndex);
            bool isSlot2 = (FirebaseManager.instance != null && FirebaseManager.instance.secondaryGunID == currentGunIndex);

            slot1Text.text = isSlot1 ? "EQUIPPED (1)" : "EQUIP TO SLOT 1";
            slot2Text.text = isSlot2 ? "EQUIPPED (2)" : "EQUIP TO SLOT 2";

            equipSlot1Button.interactable = !isSlot1;
            equipSlot2Button.interactable = !isSlot2;
        }
        else
        {
            buyButton.gameObject.SetActive(true);
            buyButtonText.text = "BUY " + currentGun.price;
            equipSlot1Button.gameObject.SetActive(false);
            equipSlot2Button.gameObject.SetActive(false);
        }
    }

    // --- FIX: THESE ARE THE MISSING FUNCTIONS ---

    void LoadOwnership(string data, ItemBlueprint[] items)
    {
        if (string.IsNullOrEmpty(data)) return;
        char[] status = data.ToCharArray();
        for (int i = 0; i < items.Length; i++)
        {
            if (i < status.Length) items[i].isOwned = (status[i] == '1');
        }
    }

    string CreateOwnershipString(ItemBlueprint[] items)
    {
        string data = "";
        foreach (var item in items)
        {
            data += item.isOwned ? "1" : "0";
        }
        return data; // <--- THIS WAS MISSING
    }
}