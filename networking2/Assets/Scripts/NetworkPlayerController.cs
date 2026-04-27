using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkShoot networkShoot;
    [SerializeField] private CharacterController charController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject cinemachineCameraTarget;
    [SerializeField] private PauseScript pauseScript;
    public Camera playerCamera;

    [Header("Input Values")]
    private Vector2 move;
    private Vector2 look;
    private bool jump;
    private bool sprint;

    [Header("Input Settings")]
    [SerializeField] private bool analogMovement;
    [SerializeField] private bool cursorLocked = true;
    [SerializeField] private bool cursorInputForLook = true;

    [Header("Player Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float speedChangeRate = 10f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.07f;
    [SerializeField] private float gamepadSensitivity = 120f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpTimeout = 0.1f;
    [SerializeField] private float fallTimeout = 0.15f;

    [Header("Ground Check")]
    [SerializeField] private bool grounded = true;
    [SerializeField] private float groundedOffset = -0.14f;
    [SerializeField] private float groundedRadius = 0.5f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Cinemachine Settings")]
    [SerializeField] private float topClamp = 75f;
    [SerializeField] private float bottomClamp = -75f;

    private float cinemachineTargetPitch;
    private float speed;
    private float rotationVelocity;
    private float verticalVelocity;
    private float terminalVelocity = 53f;
    private float jumpTimeoutDelta;
    private float fallTimeoutDelta;

    [Header("Camera Movement Noise")]
    [SerializeField] private CinemachineBasicMultiChannelPerlin cameraNoise;
    [SerializeField] private float walkNoiseAmplitude = 0.15f;
    [SerializeField] private float walkNoiseFrequency = 0.25f;
    [SerializeField] private float sprintNoiseAmplitude = 0.35f;
    [SerializeField] private float sprintNoiseFrequency = 0.8f;
    [SerializeField] private float noiseBlendSpeed = 8f;

    private PlayerInput playerInput;

    [SerializeField] private Animator animator;
    private bool hasAnimator;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

    [Header("Player SFX")]
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Misc")]
    [SerializeField] private GameObject playerHat;
    [SerializeField] private LayerMask hideLayerMask;
    [SerializeField] private Transform playerSpawn;

    private const float threshold = 0.01f;

    private bool IsMouseInput()
    {
        return Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsOwner) return;

        TeleportToSpawn();
    }

    public override void OnNetworkSpawn()
    {
        charController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        networkShoot = GetComponent<NetworkShoot>();
        playerHealth = GetComponent<PlayerHealth>();
        hasAnimator = TryGetComponent(out animator);

        AssignAnimationIDs();

        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;

                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }

            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            enabled = false;
            return;
        }

        if (playerInput != null)
        {
            playerInput.enabled = true;

            playerInput.actions["Move"].Enable();
            playerInput.actions["Look"].Enable();
            playerInput.actions["Jump"].Enable();
            playerInput.actions["Sprint"].Enable();
            playerInput.actions["Shoot"].Enable();
            playerInput.actions["Pause"].Enable();
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = true;
            }
        }

        if (pauseScript == null)
        {
            pauseScript = FindFirstObjectByType<PauseScript>();
        }

        if (playerHat != null)
        {
            SetLayerRecursive(playerHat, MaskToLayer(hideLayerMask));
        }

        jumpTimeoutDelta = jumpTimeout;
        fallTimeoutDelta = fallTimeout;

        SetCursorState(cursorLocked);
        TeleportToSpawn();
    }

    private void TeleportToSpawn()
    {
        GameObject spawnObj = GameObject.Find("PlayerSpawn");

        if (spawnObj != null && charController != null)
        {
            playerSpawn = spawnObj.transform;

            charController.enabled = false;
            transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            charController.enabled = true;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (PauseScript.IsGamePaused)
        {
            move = Vector2.zero;
            look = Vector2.zero;
            jump = false;
            sprint = false;

            HandleCameraNoise();
            return;
        }

        GroundedCheck();
        JumpAndGravity();
        Move();
        HandleCameraNoise();
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        CameraRotation();
    }

    private string DebugName
    {
        get
        {
            if (NetworkManager.Singleton == null)
                return $"[No NetworkManager] {gameObject.name}";

            return $"Player: {gameObject.name} | Owner: {OwnerClientId} | Local: {NetworkManager.Singleton.LocalClientId} | IsOwner: {IsOwner} | IsServer: {IsServer}";
        }
    }

    public void OnMove(InputValue value)
    {
        Vector2 inputValue = value.Get<Vector2>();

        Debug.Log($"[OnMove] {DebugName} | Input: {inputValue} | ScriptEnabled: {enabled} | PlayerInputEnabled: {(playerInput != null && playerInput.enabled)}");

        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        MoveInput(inputValue);
    }

    public void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        SprintInput(value.isPressed);
    }

    public void OnShoot(InputValue value)
    {
        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        if (value.isPressed && networkShoot != null)
        {
            networkShoot.ProcessLocalShoot();
        }
    }

    public void OnPause(InputValue value)
    {
        if (!IsOwner) return;
        if (!value.isPressed) return;

        if (pauseScript == null)
        {
            pauseScript = FindFirstObjectByType<PauseScript>();
        }

        if (pauseScript != null)
        {
            pauseScript.TogglePause();
        }
    }

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - groundedOffset,
            transform.position.z
        );

        grounded = Physics.CheckSphere(
            spherePosition,
            groundedRadius,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hasAnimator)
        {
            animator.SetBool(animIDGrounded, grounded);
        }
    }

    private void CameraRotation()
    {
        if (look.sqrMagnitude >= threshold)
        {
            bool isMouse = IsMouseInput();

            float sensitivity = isMouse ? mouseSensitivity : gamepadSensitivity;
            float deltaTimeMultiplier = isMouse ? 1.0f : Time.deltaTime;

            float lookX = look.x * sensitivity * deltaTimeMultiplier;
            float lookY = look.y * sensitivity * deltaTimeMultiplier;

            cinemachineTargetPitch -= lookY;
            rotationVelocity = lookX;

            cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, bottomClamp, topClamp);

            if (cinemachineCameraTarget != null)
            {
                cinemachineCameraTarget.transform.localRotation =
                    Quaternion.Euler(cinemachineTargetPitch, 0f, 0f);
            }

            transform.Rotate(Vector3.up * rotationVelocity);
        }
    }

    private void Move()
    {
        if (charController == null || !charController.enabled)
        {
            Debug.LogWarning($"[Move Blocked] {DebugName} | CharacterController missing or disabled.");
            return;
        }

        float targetSpeed = sprint ? sprintSpeed : moveSpeed;

        if (move == Vector2.zero)
        {
            targetSpeed = 0f;
        }

        float currentHorizontalSpeed = new Vector3(
            charController.velocity.x,
            0f,
            charController.velocity.z
        ).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = analogMovement ? move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(
                currentHorizontalSpeed,
                targetSpeed * inputMagnitude,
                Time.deltaTime * speedChangeRate
            );

            speed = Mathf.Round(speed * 1000f) / 1000f;
        }
        else
        {
            speed = targetSpeed;
        }

        Vector3 inputDirection = Vector3.zero;

        if (move != Vector2.zero)
        {
            inputDirection = transform.right * move.x + transform.forward * move.y;
        }

        if (hasAnimator)
        {
            animator.SetFloat(animIDSpeed, speed);
            animator.SetFloat(animIDMotionSpeed, inputMagnitude);
        }

        charController.Move(
            inputDirection.normalized * (speed * Time.deltaTime) +
            new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime
        );
    }

    private void JumpAndGravity()
    {
        if (grounded)
        {
            fallTimeoutDelta = fallTimeout;

            if (hasAnimator)
            {
                animator.SetBool(animIDJump, false);
                animator.SetBool(animIDFreeFall, false);
            }

            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (jump && jumpTimeoutDelta <= 0f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (hasAnimator)
                {
                    animator.SetBool(animIDJump, true);
                }

                jump = false;
            }

            if (jumpTimeoutDelta >= 0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            jumpTimeoutDelta = jumpTimeout;

            if (fallTimeoutDelta >= 0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else if (hasAnimator)
            {
                animator.SetBool(animIDFreeFall, true);
            }

            jump = false;
        }

        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    [ClientRpc]
    public void ToggleControllerClientRpc(bool enabled)
    {
        this.enabled = enabled;

        if (charController != null)
        {
            charController.enabled = enabled;
        }
    }

    [ClientRpc]
    public void ResetCameraRotationClientRpc(Quaternion targetRotation)
    {
        if (!IsOwner) return;

        cinemachineTargetPitch = 0f;

        if (cinemachineCameraTarget != null)
        {
            cinemachineCameraTarget.transform.localRotation = Quaternion.identity;
        }

        transform.rotation = targetRotation;
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0 && charController != null)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);

                AudioSource.PlayClipAtPoint(
                    FootstepAudioClips[index],
                    transform.TransformPoint(charController.center),
                    FootstepAudioVolume
                );
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (LandingAudioClip != null && charController != null)
            {
                AudioSource.PlayClipAtPoint(
                    LandingAudioClip,
                    transform.TransformPoint(charController.center),
                    FootstepAudioVolume
                );
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!IsOwner) return;
        if (PauseScript.IsGamePaused) return;

        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.visible = !newState;
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;

        return Mathf.Clamp(angle, min, max);
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

    private void HandleCameraNoise()
    {
        if (cameraNoise == null) return;

        bool isMoving = move.sqrMagnitude > 0.01f && grounded;
        bool isSprinting = sprint && isMoving;

        float targetAmplitude = 0f;
        float targetFrequency = 0f;

        if (isMoving)
        {
            targetAmplitude = isSprinting ? sprintNoiseAmplitude : walkNoiseAmplitude;
            targetFrequency = isSprinting ? sprintNoiseFrequency : walkNoiseFrequency;
        }

        cameraNoise.AmplitudeGain = Mathf.Lerp(
            cameraNoise.AmplitudeGain,
            targetAmplitude,
            Time.deltaTime * noiseBlendSpeed
        );

        cameraNoise.FrequencyGain = Mathf.Lerp(
            cameraNoise.FrequencyGain,
            targetFrequency,
            Time.deltaTime * noiseBlendSpeed
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - groundedOffset,
            transform.position.z
        );

        Gizmos.DrawWireSphere(spherePosition, groundedRadius);
    }
}