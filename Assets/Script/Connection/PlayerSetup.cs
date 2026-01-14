using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

// CHANGED: Must inherit from MonoBehaviourPun to use photonView
public class PlayerSetup : MonoBehaviourPun
{
    [Header("Components")]
    public PlayerMovement playerMovement;
    public InputManager inputManager;
    public AnimatorManager animatorManager;
    public PlayerManager playerManager;
    public WeaponSwitcher weaponSwitcher;

    // --- NEW: COSTUME ARRAYS (Drag these in Inspector) ---
    [Header("Appearance Sync")]
    public GameObject[] heads;
    public GameObject[] helmets;
    public GameObject[] vests;

    [Header("Objects")]
    public GameObject CameraObject; // Renamed from 'Camera' to avoid confusion with Unity Type
    public string nickName;

    void Start()
    {
        // 1. GAME LOGIC
        if (photonView.IsMine)
        {
            IslocalPlayer();

            // --- NEW: SEND MY OUTFIT DATA TO OTHERS ---
            // We check if Firebase exists (in case you are testing offline)
            if (FirebaseManager.instance != null)
            {
                int h = FirebaseManager.instance.headIndex;
                int helm = FirebaseManager.instance.helmetIndex;
                int v = FirebaseManager.instance.vestIndex;

                // "RpcTarget.AllBuffered" ensures players who join LATER still see your outfit
                photonView.RPC("SyncCostume", RpcTarget.AllBuffered, h, helm, v);
            }
        }
        else
        {
            // If this is not me (an enemy), disable their controls
            IsRemotePlayer();
        }

        if (photonView.IsMine)
        {
            // Find the Minimap Camera in the scene
            // Note: Replace "MinimapCamera" with the exact name of your script if it's different
            MinimapFollow minimap = FindObjectOfType<MinimapFollow>();

            if (minimap != null)
            {
                // Force the minimap to follow THIS player (Me)
                minimap.player = this.transform;
            }
        }
    }

    // --- NEW: THE NETWORK FUNCTION ---
    [PunRPC]
    public void SyncCostume(int headID, int helmetID, int vestID)
    {
        // Activate the correct models based on the numbers received from the network
        ActivateModel(heads, headID);
        ActivateModel(helmets, helmetID);
        ActivateModel(vests, vestID);
    }

    // Helper function to turn on 1 item and turn off the rest
    void ActivateModel(GameObject[] list, int index)
    {
        if (list == null) return;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
            {
                // If i matches the index, set True. Otherwise set False.
                list[i].SetActive(i == index);
            }
        }
    }

    public void IslocalPlayer()
    {
        playerMovement.enabled = true;
        inputManager.enabled = true;
        animatorManager.enabled = true;
        playerManager.enabled = true;
        weaponSwitcher.enabled = true;

        if (CameraObject != null) CameraObject.SetActive(true);

        // Ensure Local Player is on "Default" layer so enemies can shoot you
        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Default"));
    }

    public void IsRemotePlayer()
    {
        playerMovement.enabled = false;
        inputManager.enabled = false;
        animatorManager.enabled = true; // Animations must stay ON
        playerManager.enabled = true;
        weaponSwitcher.enabled = true;

        if (CameraObject != null) CameraObject.SetActive(false);
    }

    [PunRPC]
    public void SetNickname(string _name)
    {
        nickName = _name;
    }


    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // --- THE FIX ---
        // If the object is ALREADY on the "Minimap" layer, DO NOT change it.
        // Make sure you type "Minimap" exactly as it appears in your Unity Layers.
        if (obj.layer == LayerMask.NameToLayer("MinimapItem"))
        {
            return; // Skip this object and don't touch it
        }
        // ----------------

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

}