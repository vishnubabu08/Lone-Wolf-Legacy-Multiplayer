using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Script Ref")]
    InputManager inputManager;

    [Header("Movement")]
    Vector3 moveDirection;
    public Transform camObject;
    Rigidbody playerRigidbody;

    public float movementSpeed = 2f;
    public float rotationSpeed = 12f;
    public float sprintingSpeed = 5f;
    public float jumpForce = 5f;
    public bool isGrounded;

    public Animator animator;
    public WeaponSwitcher weaponSwitcher;

    [Header("Movement Flags")]
    public bool isMoving;
    public bool isSprinting;
    public bool isCrouching;
    public bool isCrawling;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float fallingSpeed = 5f;

    [Header("Crouch Settings")]
    CapsuleCollider playerCollider;
    public float CrouchHeight = 1f;
    private float originalHeight;
    private Vector3 originalCenter;

    [Header("Vault Detection")]
    public float vaultDetectRange = 2.5f;
    public float lowRayHeight = 0.5f;
    public float midRayHeight = 1.2f;
    public float highRayHeight = 1.8f;
    public LayerMask obstacleLayer;
    public LayerMask WindowLayer;
    private bool rifleActive = true;

    private void Awake()
    {
        inputManager = GetComponent<InputManager>();
        playerRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();

        originalHeight = playerCollider.height;
        originalCenter = playerCollider.center;
    }

    private void Update()
    {
        HandleJump();
        HandleCrouch();
        HandleCrawl();

        if (isSprinting)
            inputManager.reloadInput = false;

        if (inputManager.shootInput)
        {
            movementSpeed = 2f;
            sprintingSpeed = 5f;
            if (isCrawling)
                inputManager.MovementInput = Vector2.zero;
        }
        else
        {
            movementSpeed = 5f;
            sprintingSpeed = 8f;
        }

        if (isCrawling && movementSpeed > 1)
        {
            movementSpeed = 0.8f;
            inputManager.sprintInput = false;
        }

        if (isCrouching)
        {
            movementSpeed = 2.5f;
            sprintingSpeed = 5f;
        }
    }

    public void HandleAllMovement()
    {
        HandleMovement();
        HandleRotation();
        ApplyGravity();
    }

    void HandleMovement()
    {
        moveDirection = camObject.forward * inputManager.VerticalInput;
        moveDirection += camObject.right * inputManager.HorizontalInput;
        moveDirection.Normalize();
        moveDirection.y = 0;

        float speed = isSprinting ? sprintingSpeed : movementSpeed;
        moveDirection *= speed;

        Vector3 movementVelocity = moveDirection;
        movementVelocity.y = playerRigidbody.linearVelocity.y;
        playerRigidbody.linearVelocity = movementVelocity;

        isMoving = inputManager.moveAmount > 0.5f;

    }

    void HandleRotation()
    {
        Vector3 targetDirection = camObject.forward * inputManager.VerticalInput +
                                  camObject.right * inputManager.HorizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;

        if (targetDirection == Vector3.zero)
            targetDirection = transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
 
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            Vector3 currentVelocity = playerRigidbody.linearVelocity;
            currentVelocity.y += gravity * fallingSpeed * Time.deltaTime;
            playerRigidbody.linearVelocity = currentVelocity;
           

        }
    }

    private void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
     //   Debug.Log("working");

    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
      // Debug.Log("notworking");

    }

    private string DetectJumpType()
    {
        Vector3 origin = transform.position;

        Vector3 lowOrigin = origin + Vector3.up * lowRayHeight;
        Vector3 midOrigin = origin + Vector3.up * midRayHeight;
        Vector3 highOrigin = origin + Vector3.up * highRayHeight;

        bool lowHit = Physics.Raycast(lowOrigin, transform.forward, vaultDetectRange, obstacleLayer);
        bool midHit = Physics.Raycast(midOrigin, transform.forward, vaultDetectRange, obstacleLayer);
        bool highHit = Physics.Raycast(highOrigin, transform.forward, vaultDetectRange, obstacleLayer);
        bool WidowHit = Physics.Raycast(midOrigin, transform.forward, vaultDetectRange, WindowLayer);

        Debug.DrawRay(lowOrigin, transform.forward * vaultDetectRange, Color.green);
        Debug.DrawRay(midOrigin, transform.forward * vaultDetectRange, Color.yellow);
        Debug.DrawRay(highOrigin, transform.forward * vaultDetectRange, Color.red);

        if (WidowHit)
            return "Window";

        else if (lowHit || highHit|| lowHit)
        {

            return "Wall";
           
        }
           
        else
            return "Normal";
    }

    private void HandleJump()
    {
        if (inputManager.jumpInput && isGrounded && !isCrouching && !isCrawling)
        {
            string jumpType = DetectJumpType();

            if (jumpType == "Window")
            {
                animator.SetTrigger("Window");
                StartCoroutine(VaultMove(transform.forward + Vector3.up * 0.4f, 2f, 0.5f));

            }
            else if (jumpType == "Wall")
            {
              /*  if (animator.GetBool("rifleActive")==true)
                    {
                    animator.SetBool("rifleActive", false);
                    rifleActive = false;
                    Debug.Log("sdkfhlaskhfk");
                    }*/

                animator.SetTrigger("VaultWall");
                StartCoroutine(VaultMove(transform.forward + Vector3.up * .3f, 2.3f, 0.2f));
            }
            else
            {
                // Normal physics jump
               // StartCoroutine(VaultMove(transform.forward + Vector3.up * 0.7f, 2f, 0.6f));
               Vector3 jumpDir = (transform.forward + Vector3.up).normalized;
                playerRigidbody.AddForce(jumpDir * jumpForce, ForceMode.Impulse);
                animator.SetTrigger("Jump");
                isGrounded = false;
            }

            isGrounded = false;
        }
    }

    IEnumerator VaultMove(Vector3 direction, float distance, float duration)
    {
        playerRigidbody.isKinematic = true;

        Vector3 start = transform.position;
        Vector3 end = start + direction.normalized * distance;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        playerRigidbody.isKinematic = false;

        if (!rifleActive)
        {
            animator.SetBool("rifleActive", true);
            rifleActive = true; // Update flag as well
        }
    }

    /*private void HandleCrouch()
    {
        if (inputManager.crouchInput)
        {
            isCrawling = false;
            animator.SetBool("CrawlingActive", false);
            isCrouching = !isCrouching;
            animator.SetBool("CrouchActive", isCrouching);

            if (isCrouching)
            {
                playerCollider.height = CrouchHeight;
                playerCollider.center = new Vector3(playerCollider.center.x, CrouchHeight / 2, playerCollider.center.z);
            }
            else
            {
                playerCollider.height = originalHeight;
                playerCollider.center = originalCenter;
            }
        }
    }*/

    private void HandleCrouch()
    {
        if (inputManager.crouchInput)
        {
            isCrawling = false;
            animator.SetBool("CrawlingActive", false);

            isCrouching = !isCrouching;
            animator.SetBool("CrouchActive", isCrouching);

            if (isCrouching)
            {
                playerCollider.direction = 1; // Y-axis (standing/crouching mode)
                playerCollider.height = CrouchHeight;
                playerCollider.center = new Vector3(playerCollider.center.x, CrouchHeight / 2, playerCollider.center.z);
            }
            else
            {
                playerCollider.direction = 1; // Ensure standing direction
                playerCollider.height = originalHeight;
                playerCollider.center = originalCenter;
            }
        }
    }



    private void HandleCrawl()
    {
        if (inputManager.crawlInput)
        {
            isCrouching = false;
            animator.SetBool("CrouchActive", false);
            isCrawling = !isCrawling;
            animator.SetBool("CrawlingActive", isCrawling);

            if (isCrawling)
            {
                playerCollider.direction = 2; // Z axis (horizontal)
                playerCollider.height = 1.2f; // Length of the body while lying down
                playerCollider.radius = 0.27f; // Body thickness
                playerCollider.center = new Vector3(0, 0.3f, 0); // Keep it near the ground
            }
            else
            {
                playerCollider.direction = 1; // Reset to Y axis (standing/crouch mode)
                playerCollider.height = originalHeight;
                playerCollider.radius = 0.27f; // Optional: match original
                playerCollider.center = originalCenter;
            }
        }
    }

}
