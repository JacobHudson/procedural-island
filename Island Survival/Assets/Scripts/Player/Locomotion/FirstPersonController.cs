using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    private bool ShouldJump => characterController.isGrounded && Input.GetKeyDown(jumpKey) && currentStamina > 0f;
    private bool ShouldCrouch => !duringCrouchAnimation && characterController.isGrounded && Input.GetKey(crouchKey);

    [Header("Functionality Options")] 
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool headbob = true;
    [SerializeField] private bool slideDownSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool footsteps = true;
    [SerializeField] private bool useStamina = true;

    [Header("Controls")]
    [SerializeField] public KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] public KeyCode jumpKey = KeyCode.Space;
    [SerializeField] public KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] public KeyCode zoomKey = KeyCode.Mouse1;
    [SerializeField] public KeyCode interactKey = KeyCode.E;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Look")]
    [SerializeField, Range(0,10)] private float mouseSensitivityX = 5f;
    [SerializeField, Range(0,10)] private float mouseSensitivityY = 5f;
    [SerializeField, Range(0,180)] private float upperLookLimit = 80f;
    [SerializeField, Range(0,180)] private float lowerLookLimit = 80f;

    [Header("Health")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float timeBeforeHealthRegenStarts = 3f;
    [SerializeField] private float healthValueIncrement = 1f;
    [SerializeField] private float healthTimeIncrement = 0.1f;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

    [Header("Stamina")]
    [SerializeField] private float currentStamina;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaUseMultiplier = 5f;
    [SerializeField] private float timeBeforeStaminaRegenStarts = 5f;
    [SerializeField] private float staminaValueIncrement = 1f;
    [SerializeField] private float staminaTimeIncrement = 0.05f;
    private Coroutine regeneratingStamina;
    public static Action<float> OnStaminaChange;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float jumpStaminaCostPercent = 0.2f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Header("Headbob")]
    [SerializeField] private float walkHeadbobSpeed = 14f;
    [SerializeField] private float walkHeadbobFrequency = 0.05f;
    [SerializeField] private float sprintHeadbobSpeed = 18f;
    [SerializeField] private float sprintHeadbobFrequency = 0.1f;
    [SerializeField] private float crouchHeadbobSpeed = 8f;
    [SerializeField] private float crouchHeadbobFrequency = 0.025f;
    private float cameraDefaultYPos = 0;
    private float timer;

    [Header("Zoom")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFOV;
    private Coroutine zoomRoutine;

    [Header("Interaction")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactionLayerMask = default;
    private Interactable currentInteractable;

    [Header("Footsteps")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] woodClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    private float footstepTimer = 0;
    private float GetCurrecntOffset => isCrouching ? baseStepSpeed * crouchStepMultiplier : IsSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Respawn")]
    [SerializeField] private float timeToRespawn = 3f;
    [SerializeField] private Transform respawnPoint = default;

    //SLiding down slopes variables
    private Vector3 hitPointNormal;
    private bool isSliding
    {
        get
        {
            if(characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }
    
    private Camera playerCamera;
    private CharacterController characterController;
    private Vector3 moveDirection;
    private Vector2 currentInput;
    private float rotationX = 0;
    
    void OnEnable()
    {
        OnTakeDamage += ApplyDamage;
    }

    void OnDisale()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        cameraDefaultYPos = playerCamera.transform.localPosition.y;
        defaultFOV = playerCamera.fieldOfView;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if(!CanMove){return;}
        HandleMovementInput();
        HandleLookInput();
        if(canJump){HandleJumpInput();}
        if(canCrouch){HandleCrouchInput();}
        if(headbob){HandleHeadbob();}
        if(canZoom){HandleZoomInput();}
        if(canInteract){
            HandleInteractionCheck();
            HandleInteractionInput();
        }
        if(footsteps){HandleFootsteps();}
        if(useStamina){HandleStamina();}
        ApplyFinalMovement();
    }

    void HandleMovementInput()
    {
        currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x + (transform.TransformDirection(Vector3.right) * currentInput.y));
        moveDirection.y = moveDirectionY;
    }

    void HandleLookInput()
    {
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivityX, 0);
    }

    void HandleJumpInput()
    {
        if(ShouldJump)
        {
            moveDirection.y = jumpForce;
            
            currentStamina -= jumpStaminaCostPercent * maxStamina;

            if(regeneratingStamina != null)
            {
                StopCoroutine(regeneratingStamina);
                regeneratingStamina = null;
            }

            if(currentStamina < 0)
                currentStamina = 0;

            OnStaminaChange?.Invoke(currentStamina);

            if(currentStamina <= 0)
                canSprint = false;
        }
    }

    void HandleCrouchInput()
    {
        if(ShouldCrouch){StartCoroutine(CrouchStand());}
    }

    void HandleHeadbob()
    {
        if(!characterController.isGrounded){return;}

        if(Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f){
            timer += Time.deltaTime * (isCrouching ? crouchHeadbobSpeed : IsSprinting ? sprintHeadbobSpeed : walkHeadbobSpeed);
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x,
            cameraDefaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchHeadbobFrequency : IsSprinting ? sprintHeadbobFrequency : walkHeadbobFrequency),
            playerCamera.transform.localPosition.z);
        }
    }

    void HandleStamina()
    {
        if(IsSprinting && currentInput!= Vector2.zero)
        {
            if(regeneratingStamina != null)
            {
                StopCoroutine(regeneratingStamina);
                regeneratingStamina = null;
            }

            currentStamina -= staminaUseMultiplier * Time.deltaTime;

            if(currentStamina < 0)
                currentStamina = 0;

            OnStaminaChange?.Invoke(currentStamina);

            if(currentStamina <= 0)
                canSprint = false;
        }

        if(!ShouldJump && !IsSprinting && currentStamina < maxStamina && regeneratingStamina == null)
        {
            regeneratingStamina = StartCoroutine(RegenerateStamina());
        }
    }

    void HandleZoomInput()
    {
        if(Input.GetKeyDown(zoomKey))
        {
            if(zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }

        if(Input.GetKeyUp(zoomKey))
        {
            if(zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }
            
            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }

    void HandleInteractionCheck()
    {
        if(Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            if(hit.collider.gameObject.layer == 6 && (currentInteractable == null || hit.collider.gameObject.GetInstanceID() != currentInteractable.gameObject.GetInstanceID()))
            {
                hit.collider.TryGetComponent(out currentInteractable);

                if(currentInteractable)
                    currentInteractable.OnFocus();
                
            }
        }
        else if (currentInteractable)
        {
            currentInteractable.OnUnfocus();
            currentInteractable = null;
        }
    }

    void HandleInteractionInput()
    {
        if(Input.GetKeyDown(interactKey) && currentInteractable != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayerMask))
        {
            currentInteractable.OnInteract();
        }
    }
    
    void HandleFootsteps()
    {
        if(!characterController.isGrounded){return;}
        if(currentInput == Vector2.zero){return;}

        footstepTimer -= Time.deltaTime;

        int layerMask = 1 << 8;
        layerMask = ~layerMask;
        if(footstepTimer <= 0)
        {
            if(Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 8f, layerMask))
            {
                switch (hit.collider.tag)
                {
                    case "Footsteps/GRASS":
                        footstepAudioSource.PlayOneShot(grassClips[UnityEngine.Random.Range(0, grassClips.Length-1)]);
                        break;
                    case "Footsteps/WOOD":
                        footstepAudioSource.PlayOneShot(woodClips[UnityEngine.Random.Range(0, woodClips.Length-1)]);
                        break;
                    case "Footsteps/METAL":
                        footstepAudioSource.PlayOneShot(metalClips[UnityEngine.Random.Range(0, metalClips.Length-1)]);
                        break;
                    default:
                        //footstepAudioSource.PlayOneShot(grassClips[UnityEngine.Random.Range(0, grassClips.Length-1)]);
                        break;
                }
            }
            footstepTimer = GetCurrecntOffset;
        }
    }
    
    void ApplyDamage(float damage)
    {
        currentHealth -= damage;

        if(currentHealth <= 0)
        {
            KillPlayer();
        }
        else if(regeneratingHealth != null)
            StopCoroutine(regeneratingHealth);

        OnDamage?.Invoke(currentHealth);
        regeneratingHealth = StartCoroutine(RegenerateHealth());
    }

    void KillPlayer()
    {
        currentHealth = 0;

        if(regeneratingHealth != null)
            StopCoroutine(regeneratingHealth);

        print("Player has died");
        StartCoroutine(Respawn());
    }

    void ApplyFinalMovement()
    {
        if(!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;
        
        if(slideDownSlopes && isSliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private IEnumerator CrouchStand()
    {
        if(isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
            yield break;

        duringCrouchAnimation = true;

        float timeElaplsed = 0;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while(timeElaplsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElaplsed/timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElaplsed/timeToCrouch);
            timeElaplsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float statingFOV = playerCamera.fieldOfView;
        float timeElaplsed = 0;

        while(timeElaplsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(statingFOV, targetFOV, timeElaplsed/timeToZoom);
            timeElaplsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }

    private IEnumerator RegenerateHealth()
    {
        yield return new WaitForSeconds(timeBeforeHealthRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(healthTimeIncrement);
        while(currentHealth < maxHealth)
        {
            currentHealth += healthValueIncrement;

            if(currentHealth > maxHealth)
                currentHealth = maxHealth;

            OnHeal?.Invoke(currentHealth);
            yield return timeToWait;
        }

        regeneratingHealth = null;
    }

    private IEnumerator RegenerateStamina()
    {
        yield return new WaitForSeconds(timeBeforeStaminaRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(staminaTimeIncrement);
        while(currentStamina < maxStamina)
        {
            if(currentStamina > 0)
                canSprint = true;
            
            currentStamina += staminaValueIncrement;

            if(currentStamina > maxStamina)
                currentStamina = maxStamina;


            OnStaminaChange?.Invoke(currentStamina);
            yield return timeToWait;
        }

        regeneratingStamina = null;
    }

    private IEnumerator Respawn(){
        yield return new WaitForSeconds(timeToRespawn);
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        OnDamage?.Invoke(currentHealth);
        OnStaminaChange?.Invoke(currentStamina);

        currentInteractable = default;
        currentInput = Vector2.zero;
        transform.position = respawnPoint.position;
    }
}