using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class NetworkShoot : NetworkBehaviour
{
    [System.Serializable]
    public struct StaffData
    {
        public string staffType;
        public Material gemMaterial;
        public int projectileIndex;
    }

    [Header("References")]
    private NetworkPlayerController playerController;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Shoot Settings")]
    [SerializeField] private ParticleSystem wandSmoke;
    [SerializeField] private Animator wandAnimator;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float wandRange = 100f;
    [SerializeField] private Transform wandFirePoint;

    [Header("Projectile Library")]
    [SerializeField] private GameObject[] projectilePrefabs;
    [SerializeField] private StaffData[] staffDefinitions;

    [Header("Current Stats")]
    public int currentStaffTypeIndex = 0;
    public int baseStaffDamage = 0;

    private NetworkVariable<int> netStaffTypeIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> netStaffDamage = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool canShoot = true;

    [Header("Visuals")]
    [SerializeField] private Renderer staffRenderer;
    [SerializeField] private int gemMaterialIndex = 1;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<NetworkPlayerController>();

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        netStaffTypeIndex.OnValueChanged += (oldVal, newVal) => UpdateStaffVisuals(newVal);

        if (IsServer)
        {
            netStaffTypeIndex.Value = currentStaffTypeIndex;
            netStaffDamage.Value = baseStaffDamage;
        }

        UpdateStaffVisuals(netStaffTypeIndex.Value);

        if (!IsOwner)
        {
            this.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (currentStaffTypeIndex != netStaffTypeIndex.Value || baseStaffDamage != netStaffDamage.Value)
        {
            SyncStaffSettingsServerRpc(currentStaffTypeIndex, baseStaffDamage);
        }
    }

    public override void OnNetworkDespawn()
    {
        netStaffTypeIndex.OnValueChanged -= (oldVal, newVal) => UpdateStaffVisuals(newVal);
    }

    void UpdateStaffVisuals(int index)
    {
        if (staffRenderer == null || staffDefinitions.Length <= index || index < 0) return;

        Material[] mats = staffRenderer.materials;
        mats[gemMaterialIndex] = staffDefinitions[index].gemMaterial;
        staffRenderer.materials = mats;

        currentStaffTypeIndex = index;
    }

    public void ProcessLocalShoot()
    {
        if (!canShoot) return;

        if (impulseSource != null)
            impulseSource.GenerateImpulse();

        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");

        StartCoroutine(ShootTimer());

        Vector3 cameraOrigin = playerController.playerCamera.transform.position;
        Vector3 cameraForward = playerController.playerCamera.transform.forward;
        Vector3 targetPoint = cameraOrigin + (cameraForward * wandRange);

        if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange))
        {
            targetPoint = hit.point;
        }

        ShootServerRpc(wandFirePoint.position, targetPoint, netStaffTypeIndex.Value, netStaffDamage.Value);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint, int index, int extraDamage)
    {
        if (index < 0 || index >= staffDefinitions.Length) return;

        int prefabIdx = staffDefinitions[index].projectileIndex;
        Vector3 moveDir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(projectilePrefabs[prefabIdx], spawnPos, Quaternion.LookRotation(moveDir));

        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        bulletNetObj.Spawn();

        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();
        if (projectile != null)
            projectile.Initialize(moveDir, extraDamage);

        ShootObserversClientRpc();
    }

    [ServerRpc]
    void SyncStaffSettingsServerRpc(int typeIndex, int damage)
    {
        netStaffTypeIndex.Value = typeIndex;
        netStaffDamage.Value = damage;
    }

    [ClientRpc]
    void ShootObserversClientRpc()
    {
        if (IsOwner) return;
        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");
    }

    IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }
}