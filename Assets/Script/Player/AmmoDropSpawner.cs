using UnityEngine;
using Photon.Pun;

public class AmmoDropSpawner : MonoBehaviourPun
{
    [Header("Settings")]
    public GameObject ammoDropPrefab;     // Drag your AmmoDrop prefab here
    public float dropHeightOffset = 0.5f; // Spawns slightly above ground

    // Call this from Health.cs when player/bot dies
    public void SpawnAmmoDrop()
    {
        // Only MasterClient spawns — prevents duplicate drops
        if (!PhotonNetwork.IsMasterClient) return;
        if (ammoDropPrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * dropHeightOffset;

        PhotonNetwork.InstantiateRoomObject(
            ammoDropPrefab.name,
            spawnPos,
            Quaternion.identity
        );
    }
}