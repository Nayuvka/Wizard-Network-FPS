using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkShoot networkShoot;
    public Transform playerCamera;
    [SerializeField] private CharacterController charController;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Player Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float sensitivity = 0.1f;

    private float rotationX = 0f;
    private float verticalVelocity;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    public InputAction shootAction;

    [SerializeField] private Animator animator;
    private bool hasAnimator;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

    private float animationBlend;
    [SerializeField] private float animationSmoothSpeed = 10f;

    [Header("Player SFX")]
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Misc")]
    [SerializeField] private GameObject playerHat;
    [SerializeField] private LayerMask hideLayerMask;
    [SerializeField] private Transform playerSpawn;

    public override void OnNetworkSpawn()
    {
        charController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        networkShoot = GetComponent<NetworkShoot>();
        hasAnimator = TryGetComponent(out animator);
        playerHealth = GetComponent<PlayerHealth>();

        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.GetComponent<Camera>().enabled = false;
            }

            if (playerInput != null)
                playerInput.enabled = false;

            enabled = false;
            return;
        }

        if (IsOwner && playerHat != null)
        {
            SetLayerRecursive(playerHat, MaskToLayer(hideLayerMask));
        }

        AssignAnimationIDs();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        shootAction = playerInput.actions["Shoot"];

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        shootAction.Enable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (IsOwner)
        {
            playerSpawn = GameObject.Find("PlayerSpawn").GetComponent<Transform>();
            charController.enabled = false;
            transform.position = playerSpawn.position;
            charController.enabled = true;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleLook();
        HandleMovement();

        if (shootAction.triggered)
        {
            networkShoot.ProcessLocalShoot();
        }
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 forward = playerCamera.forward;
        Vector3 right = playerCamera.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y) + (right * input.x);

        float inputMagnitude = input.magnitude;
        float targetSpeed = moveSpeed * inputMagnitude;

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * animationSmoothSpeed);

        if (hasAnimator)
        {
            animator.SetFloat(animIDSpeed, animationBlend);
            animator.SetFloat(animIDMotionSpeed, inputMagnitude);
        }

        if (charController.isGrounded)
        {
            verticalVelocity = -2f;

            if (hasAnimator)
            {
                animator.SetBool(animIDGrounded, true);
                animator.SetBool(animIDFreeFall, false);
            }

            if (jumpAction.triggered)
            {
                verticalVelocity = jumpForce;

                if (hasAnimator)
                {
                    animator.SetBool(animIDJump, true);
                }
            }
            else
            {
                if (hasAnimator)
                {
                    animator.SetBool(animIDJump, false);
                }
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;

            if (hasAnimator)
            {
                animator.SetBool(animIDGrounded, false);
                animator.SetBool(animIDJump, false);
                animator.SetBool(animIDFreeFall, true);
            }
        }

        Vector3 finalMove = (moveDirection * moveSpeed) + (Vector3.up * verticalVelocity);

        if (!charController.enabled) return;
        charController.Move(finalMove * Time.deltaTime);
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    void HandleLook()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>() * sensitivity;

        rotationX -= mouseDelta.y;
        rotationX = Mathf.Clamp(rotationX, -75f, 75f);

        transform.Rotate(Vector3.up * mouseDelta.x);
        playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
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

        playerCamera.localRotation = Quaternion.identity;
        transform.rotation = targetRotation;
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(charController.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(charController.center), FootstepAudioVolume);
        }
    }

    private int MaskToLayer(LayerMask mask)
    {
        int bitmask = mask.value;
        int layer = 0;
        while (bitmask > 1)
        {
            bitmask >>= 1;
            layer++;
        }
        return layer;
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }
}