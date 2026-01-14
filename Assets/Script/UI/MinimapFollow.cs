using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player; // Drag your Player here
    public GameObject bigMapPanel; // Drag your BigMapPanel here
    public Camera miniMapCam;      // Drag your MinimapCamera here
    public float Hight=210f;      


    void Update()
    {
        // Toggle Map with 'M' key or a UI Button
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }
    public void ToggleMap()
    {
        bool isActive = !bigMapPanel.activeSelf;
        bigMapPanel.SetActive(isActive);

        if (isActive)
        {
            miniMapCam.orthographicSize = 100; // Zoom out to see whole map
        }
        else
        {
            miniMapCam.orthographicSize = 20; // Zoom back in for minimap
        }
    }
    void LateUpdate()
    {
        if (player == null)
        {
            // Find the object tagged "Player" that belongs to ME
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players)
            {
                // Check if this is MY local player (you might need your PhotonView check here)
                if (p.GetComponent<Photon.Pun.PhotonView>().IsMine)
                {
                    player = p.transform;
                    break;
                }
            }
            return;
        }

        Vector3 newPosition = player.position;
        newPosition.y = Hight;
        transform.position = newPosition;
    }
}