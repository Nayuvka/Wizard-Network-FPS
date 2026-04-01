using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("References")]
    [Space(5)]
    [SerializeField] NetworkShoot networkShoot;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private CharacterController charController;

    [Header("Player Movement Settings")]
    [Space(5)]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float sensitivity = 0.1f;
    
    //private PlayerHealthScript health;
    private float rotationX = 0f;
    private float verticalVelocity;


    private float scrollCooldown = 0.2f;
    private float lastScrollTime;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction switchProjectileAction;
    private InputAction scrollAction;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (playerCamera != null) playerCamera.SetActive(false);
            return;
        }

        networkShoot = GetComponent<NetworkShoot>();
        charController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        switchProjectileAction = playerInput.actions["SwitchProjectile"];
        scrollAction = playerInput.actions["Scroll"];

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        //health = GetComponent<PlayerHealthScript>();
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;
        //if (charController == null || !charController.enabled) return;

        HandleLook();
        HandleMovement();

        if (switchProjectileAction.triggered)
        {
            networkShoot.CycleProjectileForward();
        }

        HandleScroll();
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y) + (right * input.x);

        if (charController.isGrounded)
        {
            verticalVelocity = -2f;

            if (jumpAction.triggered)
            {
                verticalVelocity = jumpForce;
                //health.TakeDamage(20f);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalMove = (moveDirection * moveSpeed) + (Vector3.up * verticalVelocity);
        if (!charController.enabled) return;
        charController.Move(finalMove * Time.deltaTime);
    }

    void HandleLook()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>() * sensitivity;

        rotationX -= mouseDelta.y;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.Rotate(Vector3.up * mouseDelta.x);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    void HandleScroll()
    {
        if (Time.time < lastScrollTime + scrollCooldown) return;

        float scrollValue = scrollAction.ReadValue<float>();

        if (scrollValue > 0f)
        {
            networkShoot.CycleProjectileForward();
            lastScrollTime = Time.time;
        }
        else if (scrollValue < 0f)
        {
            networkShoot.CycleProjectileBackward();
            lastScrollTime = Time.time;
        }
    }

    [ClientRpc]
    public void ToggleControllerClientRpc(bool enabled)
    {
        this.enabled = enabled;
        if (charController != null) charController.enabled = enabled;
    }
    [ClientRpc]
    public void ResetCameraRotationClientRpc(Quaternion targetRotation)
    {
        if (!IsOwner) return;
        rotationX = 0f;

        playerCamera.transform.localRotation = Quaternion.identity;
        transform.rotation = targetRotation;
    }
}