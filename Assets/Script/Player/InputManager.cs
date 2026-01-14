using System.Collections.Generic;
using UnityEngine;
using System .Collections;
using Terresquall;


public class InputManager : MonoBehaviour
{
    public PlayerControls playerConrols;
    PlayerMovement playerMovement;

    AnimatorManager animatorManager;
    

    public Vector2 MovementInput;

    public float VerticalInput;
    public float HorizontalInput;

    public float moveAmount;

    private Vector2 cameraInput;
    public float cameraInputX;
    public float cameraInputY;

    [Header("Input Button Flag")]
    public bool sprintInput;

    public bool jumpInput;

    public bool shootInput;

    public bool reloadInput;

    public bool scopeInput;

    public bool crouchInput;

    public bool crawlInput;

   // public bool useMobileInputs = false;

    


    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animatorManager = GetComponent<AnimatorManager>();

   /*      
    RuntimePlatform platform = Application.platform;

        if (platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer)
        {
            useMobileInputs = true;
        }
        else
        {
            useMobileInputs = false;
        }*/
    }

   public void onSprintButtonDown()
    {
        sprintInput = true;
    }
    public void onSprintButtonUp()
    {
        sprintInput = false;
    }


    public void onJumpButtonPressed()
    {
        jumpInput = true;
        StartCoroutine(ResetJumpInput());
    }
    IEnumerator ResetJumpInput()
    {
        yield return new WaitForSeconds(20f);
        jumpInput = false;

    }

    public void onShootButtonDown()
    {
       shootInput = true;
    }
    public void onShootButtonUp()
    {
       shootInput = false;
    }

    public void onReloadButtonPressed()
    {
        reloadInput = true;
        StartCoroutine(ResetReloadInput());
    }
    IEnumerator ResetReloadInput()
    {
        yield return new WaitForSeconds(0.2f);
        reloadInput = false;

    }

    public void onScopeButtonDown()
    {
       scopeInput = true;
    }
    public void onScopeButtonUp()
    {
        scopeInput = false;
    }

    public void ToggleCrouchInput()
    {
        crouchInput = !crouchInput;
    }

    public void ToggleCrawl()
    {
       crawlInput = !crouchInput;
    }

    public void OnEnable()
    {
        if (playerConrols == null)
        {
            playerConrols = new PlayerControls();

            playerConrols.PlayerMovement.Movement.performed += i => MovementInput = i.ReadValue<Vector2>();
            playerConrols.PlayerMovement.CameraMovement.performed += i => cameraInput = i.ReadValue<Vector2>();
            playerConrols.PlayerActions.Sprint.performed += i => sprintInput = true;
            playerConrols.PlayerActions.Sprint.canceled += i => sprintInput = false;
            playerConrols.PlayerActions.Jump.performed += i => jumpInput = true;
            playerConrols.PlayerActions.Shoot.performed += i => shootInput = true;
            playerConrols.PlayerActions.Shoot.canceled += i => shootInput = false;
            playerConrols.PlayerActions.Reload.performed += i => reloadInput = true;
            playerConrols.PlayerActions.Reload.canceled += i => reloadInput = false; 
            playerConrols.PlayerActions.Scope.performed += i =>scopeInput = true;
            playerConrols.PlayerActions.Scope.canceled += i => scopeInput= false;
            playerConrols.PlayerActions.C.performed += i => crouchInput = true;
            
            playerConrols.PlayerActions.Crawling.performed += i => crawlInput = true;


        }
        playerConrols.Enable();
    }

    public void OnDisable()
    {
        playerConrols.Disable();
    }
    public void HandleAllInput()
    {
        HandleMovementInput();
        HandleSprintingInput();
        StartCoroutine (HandleJumpInput());
        StartCoroutine (HandleCrouchInput());
        StartCoroutine(HandleCrawlInput());
    
    }
   public void HandleMovementInput()
    {
   /*     if(useMobileInputs==true)
        {
            VerticalInput = VirtualJoystick.GetAxis("Vertical");
            HorizontalInput = VirtualJoystick.GetAxis("Horizontal");

           cameraInputX = VirtualJoystick.GetAxis("Horizontal", 1);
            cameraInputY = VirtualJoystick.GetAxis("Vertical", 1);

            moveAmount = Mathf.Clamp01(Mathf.Abs(HorizontalInput) + Mathf.Abs(VerticalInput));
            animatorManager.UpdateAnimValue(0, moveAmount, playerMovement.isSprinting);
        }

        else
        {*/
            VerticalInput = MovementInput.y;
            HorizontalInput = MovementInput.x;

            cameraInputX = cameraInput.x;
            cameraInputY = cameraInput.y;

            moveAmount = Mathf.Clamp01(Mathf.Abs(HorizontalInput) + Mathf.Abs(VerticalInput));
            animatorManager.UpdateAnimValue(0, moveAmount, playerMovement.isSprinting);
        //}
    }

        void HandleSprintingInput()
        {
            if(sprintInput && moveAmount > 0.5f)
            {
              playerMovement .isSprinting= true;
            }
            else
            {
                playerMovement.isSprinting = false;

            }

        

        }
 

    private IEnumerator HandleJumpInput()
    {
        yield return new WaitForSeconds(0.2f);
        if(jumpInput)
        {
            jumpInput = false;
            
        }
    }

    private IEnumerator HandleCrouchInput()
    {
        yield return new WaitForSeconds(0.00001f);
        if (crouchInput)
        {
            crouchInput= false;
        }

    }

    private IEnumerator HandleCrawlInput()
    {
        yield return new WaitForSeconds(0.001f);
        if (crawlInput)
        {
            crawlInput = false;
        }
    }
}
