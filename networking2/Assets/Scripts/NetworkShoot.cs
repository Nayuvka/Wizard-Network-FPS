using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class NetworkShoot : NetworkBehaviour
{
    [System.Serializable]
    public struct StaffData
    {
        public string staffType;
        public Material gemMaterial;
        public int projectileIndex;

        [Header("VFX")]
        public ParticleSystem staffParticle;
    }

    [Header("Debug / Testing")]
    [Space(5)]
    public bool normalState = false;

    [Header("References")]
    [Space(5)]
    [SerializeField] private CameraShakeManager cameraShakeManager;
    private NetworkPlayerController playerController;
    private PlayerInput playerInput;
    private InputAction switchProjectileAction;
    private InputAction scrollProjectileAction;

    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Shoot Settings")]
    [Space(5)]
    [SerializeField] private Animator wandAnimator;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float wandRange = 100f;
    [SerializeField] private Transform wandFirePoint;
    [SerializeField] private LayerMask shootMask;
    [SerializeField] private float shakeForce = 1.0f;

    [Header("Crosshair UI")]
    [Space(5)]
    [SerializeField] private Image crosshair;
    [SerializeField] private Image killCrosshair;

    [SerializeField] private float killCrosshairDuration = 0.25f;

    public Color normalColour = Color.white;
    public Color enemyTargetColour = Color.red;

    [Header("Projectile Library")]
    [Space(5)]
    [SerializeField] private GameObject[] projectilePrefabs;
    [SerializeField] private StaffData[] staffDefinitions;

    [Header("Current Stats")]
    [Space(5)]
    public int currentStaffTypeIndex = 0;
    public int baseStaffDamage = 0;

    private NetworkVariable<int> netStaffTypeIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> netStaffDamage = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool canShoot = true;
    private Coroutine killMarkerRoutine;

    [Header("Visuals")]
    [SerializeField] private Renderer staffRenderer;
    [SerializeField] private int gemMaterialIndex = 1;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<NetworkPlayerController>();
        playerInput = GetComponent<PlayerInput>();
        cameraShakeManager = FindAnyObjectByType<CameraShakeManager>();

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        if (playerInput != null && IsOwner)
        {
            switchProjectileAction = playerInput.actions["SwitchProjectile"];
            scrollProjectileAction = playerInput.actions["Scroll"];

            switchProjectileAction.Enable();
            scrollProjectileAction.Enable();
        }

        netStaffTypeIndex.OnValueChanged += OnStaffTypeChanged;

        if (IsServer)
        {
            if (currentStaffTypeIndex >= staffDefinitions.Length)
                currentStaffTypeIndex = 0;

            netStaffTypeIndex.Value = currentStaffTypeIndex;
            netStaffDamage.Value = baseStaffDamage;
        }

        UpdateStaffVisuals(netStaffTypeIndex.Value);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (GameOverManager.IsGameOver) return;

        HandleDebugProjectileCycling();

        if (currentStaffTypeIndex >= staffDefinitions.Length)
            currentStaffTypeIndex = staffDefinitions.Length - 1;

        if (currentStaffTypeIndex != netStaffTypeIndex.Value || baseStaffDamage != netStaffDamage.Value)
        {
            SyncStaffSettingsServerRpc(currentStaffTypeIndex, baseStaffDamage);
        }

        DetectTarget();
    }

    public void DetectTarget()
    {
        Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * wandRange, Color.red);

        if(Physics.Raycast(ray, out RaycastHit hit, wandRange, shootMask))
        {
            if(hit.collider.TryGetComponent<NetworkEnemy>(out NetworkEnemy networkEnemy) || hit.collider.CompareTag("TestEnemy"))
            {
                crosshair.color = enemyTargetColour;
            }
            else
            {
                crosshair.color = normalColour;
            }
        }
    }

    private void HandleDebugProjectileCycling()
    {
        if (!normalState) return;
        if (staffDefinitions == null || staffDefinitions.Length == 0) return;

        if (switchProjectileAction != null && switchProjectileAction.triggered)
        {
            CycleProjectile(1);
        }

        if (scrollProjectileAction != null)
        {
            float scrollValue = scrollProjectileAction.ReadValue<float>();

            if (scrollValue > 0)
            {
                CycleProjectile(1);
            }
            else if (scrollValue < 0)
            {
                CycleProjectile(-1);
            }
        }
    }

    public void CycleProjectile(int direction)
    {
        currentStaffTypeIndex += direction;

        if (currentStaffTypeIndex >= staffDefinitions.Length)
            currentStaffTypeIndex = 0;

        if (currentStaffTypeIndex < 0)
            currentStaffTypeIndex = staffDefinitions.Length - 1;

        SyncStaffSettingsServerRpc(currentStaffTypeIndex, baseStaffDamage);
    }

    private void OnStaffTypeChanged(int oldVal, int newVal)
    {
        UpdateStaffVisuals(newVal);
    }

    public override void OnNetworkDespawn()
    {
        netStaffTypeIndex.OnValueChanged -= OnStaffTypeChanged;

        if (switchProjectileAction != null)
            switchProjectileAction.Disable();

        if (scrollProjectileAction != null)
            scrollProjectileAction.Disable();
    }

    void UpdateStaffVisuals(int index)
    {
        if (staffRenderer == null) return;
        if (staffDefinitions == null || staffDefinitions.Length == 0) return;
        if (index < 0 || index >= staffDefinitions.Length) return;

        Material[] mats = staffRenderer.materials;

        if (gemMaterialIndex < 0 || gemMaterialIndex >= mats.Length) return;

        mats[gemMaterialIndex] = staffDefinitions[index].gemMaterial;
        staffRenderer.materials = mats;

        currentStaffTypeIndex = index;
    }

    public void ProcessLocalShoot()
    {
        if (!IsOwner) return;
        if (GameOverManager.IsGameOver) return;
        if (!canShoot) return;

        if (cameraShakeManager != null)
        {
            cameraShakeManager.CameraShake(impulseSource, shakeForce);
        }

        ParticleSystem currentSmoke = staffDefinitions[currentStaffTypeIndex].staffParticle;

        if (currentSmoke != null)
        {
            currentSmoke.Play();
        }

        if (wandAnimator != null)
            wandAnimator.SetTrigger("Shoot");

        StartCoroutine(ShootTimer());

        Vector3 cameraOrigin = playerController.playerCamera.transform.position;
        Vector3 cameraForward = playerController.playerCamera.transform.forward;

        Vector3 targetPoint = cameraOrigin + (cameraForward * wandRange);
        ulong hitNetworkObjectId = 999999;

        if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange, shootMask))
        {
            targetPoint = hit.point;

            if (hit.collider.TryGetComponent(out NetworkObject netObj))
            {
                hitNetworkObjectId = netObj.NetworkObjectId;
            }
        }

        ShootServerRpc(
            wandFirePoint.position,
            targetPoint,
            hitNetworkObjectId,
            netStaffTypeIndex.Value,
            netStaffDamage.Value
        );
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint, ulong targetId, int index, int extraDamage)
    {
        if (index < 0 || index >= staffDefinitions.Length)
            index = 0;

        int prefabIdx = staffDefinitions[index].projectileIndex;

        if (prefabIdx < 0 || prefabIdx >= projectilePrefabs.Length)
            return;

        GameObject bullet = Instantiate(
            projectilePrefabs[prefabIdx],
            spawnPos,
            Quaternion.LookRotation(targetPoint - spawnPos)
        );

        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        bulletNetObj.Spawn();

        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();

        if (projectile != null)
            projectile.Initialize(targetPoint, targetId, extraDamage, OwnerClientId);

        ShootObserversClientRpc(spawnPos, netStaffTypeIndex.Value);
    }

    [ServerRpc]
    void SyncStaffSettingsServerRpc(int typeIndex, int damage)
    {
        if (staffDefinitions == null || staffDefinitions.Length == 0) return;

        if (typeIndex < 0)
            typeIndex = 0;

        if (typeIndex >= staffDefinitions.Length)
            typeIndex = staffDefinitions.Length - 1;

        netStaffTypeIndex.Value = typeIndex;
        netStaffDamage.Value = damage;
    }

    [ClientRpc]
    void ShootObserversClientRpc(Vector3 spawnPos, int staffIndex)
    {
        if (IsOwner) return;

        if (staffIndex < 0 || staffIndex >= staffDefinitions.Length)
            return;

        ParticleSystem currentSmoke = staffDefinitions[currentStaffTypeIndex].staffParticle;

        if (currentSmoke != null)
        {
            currentSmoke.Play();
        }

        if (wandAnimator != null)
            wandAnimator.SetTrigger("Shoot");
    }

    public void ShowKillMarker()
    {
        if (!IsServer)
            return;

        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };

        TriggerKillMarkerClientRpc(rpcParams);
    }

    IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    [ClientRpc]
    public void TriggerKillMarkerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
            return;

        if (killMarkerRoutine != null)
        {
            StopCoroutine(killMarkerRoutine);
        }

        killMarkerRoutine = StartCoroutine(FlashKillMarkerRoutine());
    }

    private IEnumerator FlashKillMarkerRoutine()
    {
        killCrosshair.gameObject.SetActive(true);
        yield return new WaitForSeconds(killCrosshairDuration);
        killCrosshair.gameObject.SetActive(false);
    }

    [ClientRpc]
    public void ApplyFireRateUpgradeClientRpc()
    {
        if (!IsOwner) return; 
        
        shootCooldown *= 0.6f; // Faster fire rate
    }
}