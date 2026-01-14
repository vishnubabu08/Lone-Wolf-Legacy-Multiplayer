using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;      // Assign the PLAYER's root GameObject (for horizontal rotation)
    public Transform cameraPivot;    // Assign the CAMERA's PARENT (for vertical rotation)

    [Header("Settings")]
    public float sensitivity = 0.3f;
    public bool invertY = false;
    public float maxPitchAngle = 80f; // Limit up/down rotation

    private float _currentPitch = 0f; // Tracks vertical rotation
    private Vector2 _lastTouchPos;

    void Update()
    {
        /*  if (Input.touchCount > 0)
          {
              Touch touch = Input.GetTouch(0);

              // Only process right-side touches
              if (touch.position.x > Screen.width * 0.5f)
              {
                  switch (touch.phase)
                  {
                      case TouchPhase.Began:
                          _lastTouchPos = touch.position;
                          break;

                      case TouchPhase.Moved:
                          Vector2 delta = touch.position - _lastTouchPos;
                          RotateCamera(delta);
                          _lastTouchPos = touch.position;
                          break;
                  }
              }
          }
      }

      private void RotateCamera(Vector2 delta)
      {
          // 1. Horizontal rotation (player body)
          float yaw = delta.x * sensitivity;
          playerBody.Rotate(Vector3.up * yaw);

          // 2. Vertical rotation (camera pivot)
          float pitch = delta.y * sensitivity * (invertY ? 1 : -1);
          _currentPitch = Mathf.Clamp(_currentPitch + pitch, -maxPitchAngle, maxPitchAngle);

          // Apply rotation to the camera pivot
          cameraPivot.localEulerAngles = new Vector3(_currentPitch, 0, 0);
          Debug.Log("Camera Pivot Rotation: " + cameraPivot.localEulerAngles); // Debug line
      }*/
    }
}