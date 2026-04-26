using Unity.Netcode;
using UnityEngine;
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
    }

    [Header("Debug / Testing")]
    public bool normalState = false;

    [Header("References")]
    [SerializeField] private CameraShakeManager cameraShakeManager;
    private NetworkPlayerController playerController;
    private PlayerInput playerInput;
    private InputAction switchProjectileAction;
    private InputAction scrollProjectileAction;

    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Shoot Settings")]
    [SerializeField] private ParticleSystem wandSmoke;
    [SerializeField] private Animator wandAnimator;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float wandRange = 100f;
    [SerializeField] private Transform wandFirePoint;
    [SerializeField] private LayerMask shootMask;
    [SerializeField] private float shakeForce = 1.0f;

    [Header("Projectile Library")]
    [SerializeField] private GameObject[] projectilePrefabs;
    [SerializeField] private StaffData[] staffDefinitions;

    [Header("Current Stats")]
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

        HandleDebugProjectileCycling();

        if (currentStaffTypeIndex >= staffDefinitions.Length)
            currentStaffTypeIndex = staffDefinitions.Length - 1;

        if (currentStaffTypeIndex != netStaffTypeIndex.Value || baseStaffDamage != netStaffDamage.Value)
        {
            SyncStaffSettingsServerRpc(currentStaffTypeIndex, baseStaffDamage);
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

    private void CycleProjectile(int direction)
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
        if (!canShoot) return;

        if(cameraShakeManager != null)
        {
            cameraShakeManager.CameraShake(impulseSource, shakeForce);
        }

        if (wandSmoke != null)
            wandSmoke.Play();

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
            projectile.Initialize(targetPoint, targetId, extraDamage);

        ShootObserversClientRpc();
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
    void ShootObserversClientRpc()
    {
        if (IsOwner) return;

        if (wandSmoke != null)
            wandSmoke.Play();

        if (wandAnimator != null)
            wandAnimator.SetTrigger("Shoot");
    }

    IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }
}