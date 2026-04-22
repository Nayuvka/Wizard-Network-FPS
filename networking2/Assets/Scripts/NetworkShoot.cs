using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class NetworkShoot : NetworkBehaviour
{
    [Header("References")]
    private NetworkPlayerController playerController;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Shoot Settings")]
    [SerializeField] private ParticleSystem wandSmoke;
    [SerializeField] private Animator wandAnimator;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float wandRange = 100f;
    [SerializeField] private Transform wandFirePoint;
    
    [Header("Projectile Inventory")]
    [SerializeField] private GameObject[] projectilePrefabs; 
    private int currentProjectileIndex = 0;
    private bool canShoot = true;

    [Header("Visuals")]
    [SerializeField] private Renderer staffRenderer;
    [SerializeField] private int gemMaterialIndex = 1;
    [SerializeField] private Material[] gemMaterials;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<NetworkPlayerController>();
        
        if (impulseSource == null) 
            impulseSource = GetComponent<CinemachineImpulseSource>();

        if (!IsOwner)
        {
            if (playerController != null && playerController.playerCamera != null)
            {
                playerController.playerCamera.GetComponent<Camera>().enabled = false;
            }
            
            if (impulseSource != null) 
                impulseSource.enabled = false;
            
            this.enabled = false;
            return;
        }

        UpdateGemMaterial();
    }

    void UpdateGemMaterial()
    {
        if (staffRenderer == null || gemMaterials.Length == 0) return;
        
        Material[] mats = staffRenderer.materials;
        mats[gemMaterialIndex] = gemMaterials[currentProjectileIndex];
        staffRenderer.materials = mats;
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

        ShootServerRpc(wandFirePoint.position, targetPoint, currentProjectileIndex);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint, int index)
    {
        if (index < 0 || index >= projectilePrefabs.Length) index = 0;

        Vector3 moveDir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(projectilePrefabs[index], spawnPos, Quaternion.LookRotation(moveDir));
        
        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        bulletNetObj.Spawn();

        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(moveDir, index);
        }

        ShootObserversClientRpc();
    }

    [ClientRpc]
    void ShootObserversClientRpc()
    {
        if (IsOwner) return;
        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");
    }

    public void CycleProjectileForward()
    {
        currentProjectileIndex = (currentProjectileIndex + 1) % projectilePrefabs.Length;
        UpdateGemMaterial();
    }

    public void CycleProjectileBackward()
    {
        currentProjectileIndex--;
        if (currentProjectileIndex < 0) 
            currentProjectileIndex = projectilePrefabs.Length - 1;
            
        UpdateGemMaterial();
    }

    IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }
}