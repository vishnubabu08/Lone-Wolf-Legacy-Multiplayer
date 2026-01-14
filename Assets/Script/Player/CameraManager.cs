using Photon.Pun;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public InputManager inputManager;
    public Transform playerTransform;
    public Transform cameraPivote;
    public Camera camera;
    public Animator animator;

    // --- FIX 1: Made Public so you can see it in Inspector ---
    public PlayerMovement playerMovement;

    private Vector3 camFollowVelocity = Vector3.zero;

    [Header("Camera Movement")]
    public float camFollowSpeed = 0.1f;
    public float camLookSpeed = 0.1f;
    public float camPivotSpeed = 0.1f;

    [Header("Camera Position Adjustment")]
    public Vector3 pivotOffset = new Vector3(0, 0, 0);

    [Header("Height Settings")]
    public float standingHeight = 1.6f;
    public float crouchingHeight = 1.1f;
    public float crawlingHeight = 0.5f;
    public float heightAdjustSpeed = 5f;

    private float currentHeight;
    private float targetHeight;

    [Header("Rotation Limits")]
    public float lookAngle;
    public float pivotAngle;
    public float minimumPivotAngle = -60f;
    public float maximumPivotAngle = 30f;

    [Header("Scope Settings")]
    public GameObject Scope;
    public float scopeFOV = 30f;
    public float defaultFOV = 60f;

    public float aimZ = 0.6f;
    public float aimSpeed = 10f;
    private bool isScoped = false;
    private bool isShoot = false;

    [Header("Camera Collision")]
    public Transform cameraTransform;
    public LayerMask collisionLayers;
    public float cameraCollisionRadius = 0.2f;
    public float cameraMinDistance = 0.5f;
    public float cameraMaxDistance = 4f;
    public float cameraCollisionSmooth = 0.05f;

    private float currentCameraDistance;

    private void Awake()
    {
        // 1. Try to find local player automatically
        foreach (var pm in FindObjectsOfType<PlayerManager>())
        {
            if (pm.GetComponent<PhotonView>().IsMine)
            {
                playerTransform = pm.transform;
                playerMovement = pm.GetComponent<PlayerMovement>();
                inputManager = pm.GetComponent<InputManager>();
                break;
            }
        }

        // --- FIX 2: Fallback - If not found, look on parent ---
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }
        if (inputManager == null)
        {
            inputManager = GetComponentInParent<InputManager>();
        }

        animator = GetComponentInParent<Animator>();
        currentCameraDistance = cameraMaxDistance;
        currentHeight = standingHeight;
        targetHeight = standingHeight;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void HandleAllCameraMovement()
    {
        // --- FIX 3: SAFETY CHECK ---
        // If the player is still missing, STOP here. Do not run the code below.
        if (playerTransform == null || playerMovement == null) return;

        CalculateTargetHeight();
        FollowTarget();
        RotateCamera();
        HandleScopedFOV();
        HandleFireRotation();
    }

    void CalculateTargetHeight()
    {
        if (playerMovement.isCrawling) targetHeight = crawlingHeight;
        else if (playerMovement.isCrouching) targetHeight = crouchingHeight;
        else targetHeight = standingHeight;

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightAdjustSpeed);
    }

    void FollowTarget()
    {
        Vector3 targetPos = playerTransform.position + (Vector3.up * currentHeight);
        targetPos += playerTransform.TransformDirection(pivotOffset);

        Vector3 finalPos = Vector3.SmoothDamp(transform.position, targetPos, ref camFollowVelocity, camFollowSpeed);
        transform.position = finalPos;

        HandleCameraPosition();
    }

    void HandleCameraPosition()
    {
        if (isScoped)
        {
            float currentZ = cameraTransform.localPosition.z;
            float nextZ = Mathf.Lerp(currentZ, aimZ, Time.deltaTime * aimSpeed);
            cameraTransform.localPosition = new Vector3(0, 0, nextZ);
        }
        else
        {
            float targetDistance = cameraMaxDistance;
            RaycastHit hit;
            Vector3 direction = cameraTransform.position - cameraPivote.position;
            direction.Normalize();

            if (Physics.SphereCast(cameraPivote.position, cameraCollisionRadius, direction, out hit, cameraMaxDistance, collisionLayers))
            {
                targetDistance = Mathf.Clamp(hit.distance, cameraMinDistance, cameraMaxDistance);
            }

            currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetDistance, Time.deltaTime / cameraCollisionSmooth);
            cameraTransform.localPosition = new Vector3(0, 0, -currentCameraDistance);
        }
    }

    void RotateCamera()
    {
        lookAngle += (inputManager.cameraInputX * camLookSpeed * 100 * Time.deltaTime);
        pivotAngle -= (inputManager.cameraInputY * camLookSpeed * 100 * Time.deltaTime);
        pivotAngle = Mathf.Clamp(pivotAngle, minimumPivotAngle, maximumPivotAngle);

        Vector3 rotation = Vector3.zero;
        rotation.y = lookAngle;
        transform.rotation = Quaternion.Euler(rotation);

        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        cameraPivote.localRotation = Quaternion.Euler(rotation);

        if (isScoped || isShoot)
        {
            playerTransform.rotation = Quaternion.Euler(0, lookAngle, 0);
            camLookSpeed = 0.05f;
            camPivotSpeed = 0.05f;
        }
        else
        {
            camLookSpeed = 0.1f;
            camPivotSpeed = 0.1f;
        }
    }

    private void HandleScopedFOV()
    {
        if (Scope == null) return;

        if (inputManager.scopeInput && animator.GetBool("rifleActive"))
        {
            isScoped = true;
            Scope.SetActive(true);
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, scopeFOV, Time.deltaTime * 10f);
        }
        else
        {
            isScoped = false;
            Scope.SetActive(false);
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, defaultFOV, Time.deltaTime * 10f);
        }
    }

    private void HandleFireRotation() { isShoot = inputManager.shootInput; }

    public void ApplyRecoil(float recoilAmount)
    {
        pivotAngle -= recoilAmount;
        pivotAngle = Mathf.Clamp(pivotAngle, minimumPivotAngle, maximumPivotAngle);
    }
}