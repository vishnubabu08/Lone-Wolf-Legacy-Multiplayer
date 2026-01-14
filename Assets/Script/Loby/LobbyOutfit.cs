using UnityEngine;

public class LobbyOutfit : MonoBehaviour
{
    [Header("3D Models References")]
    public GameObject[] heads;   // Drag all Heads here
    public GameObject[] helmets; // Drag all Helmets here
    public GameObject[] vests;   // Drag all Vests here

    [Header("Weapon References")]
    public GameObject[] gunModelsInHand; // Drag all Guns in the Hand here

    void Start()
    {
        // 1. Get data from Firebase (or use defaults if testing)
        int headIndex = 0;
        int helmetIndex = -1;
        int vestIndex = -1;
        int gunIndex = 0; // Default to first gun

        if (FirebaseManager.instance != null)
        {
            headIndex = FirebaseManager.instance.headIndex;
            helmetIndex = FirebaseManager.instance.helmetIndex;
            vestIndex = FirebaseManager.instance.vestIndex;

            // NEW: Get the Primary Gun ID
            gunIndex = FirebaseManager.instance.primaryGunID;
        }

        // 2. Update the character appearance
        UpdateModel(heads, headIndex);
        UpdateModel(helmets, helmetIndex);
        UpdateModel(vests, vestIndex);

        // NEW: Update the Gun
        UpdateModel(gunModelsInHand, gunIndex);
    }

    void UpdateModel(GameObject[] list, int index)
    {
        // Safety Check: If list is empty, skip it
        if (list == null || list.Length == 0) return;

        for (int i = 0; i < list.Length; i++)
        {
            // If the item exists, turn it on ONLY if it matches the saved index
            if (list[i] != null)
            {
                list[i].SetActive(i == index);
            }
        }
    }
}