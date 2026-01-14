using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputManager inputManager;
    PlayerMovement playerMovement;
    public CameraManager cameraManager;

    private void Awake()
    {
        inputManager = GetComponent<InputManager>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        inputManager.HandleAllInput();
        cameraManager.HandleAllCameraMovement();
    }

    private void LateUpdate()
    {
        playerMovement.HandleAllMovement();

    }
}
