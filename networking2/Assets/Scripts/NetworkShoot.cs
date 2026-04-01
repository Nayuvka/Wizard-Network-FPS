using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class NetworkShoot : NetworkBehaviour
{
    [SerializeField] private ParticleSystem wandSmoke;
    [SerializeField] private Animator wandAnimator;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float wandRange = 100f;
    [SerializeField] private Transform wandMuzzle;
    [SerializeField] private GameObject wandFlash;
    [SerializeField] private LayerMask enemyLayer;
    //[SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ProjectileData[] projectiles;
    private int currentProjectileIndex = 0;
    private ProjectileData CurrentProjectile => projectiles[currentProjectileIndex];

    private Camera playerCamera;
    private PlayerInput playerInput;
    private InputAction shootAction;
    private bool canShoot = true;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        playerCamera = GetComponentInParent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        playerInput = GetComponentInParent<PlayerInput>();
        shootAction = playerInput.actions["Shoot"];
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        if (shootAction.triggered && canShoot)
        {
            ProcessLocalShoot();
        }
    }

    void ProcessLocalShoot()
    {
        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");
        StartCoroutine(ShootTimer());

        Vector3 cameraOrigin = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 targetPoint = cameraOrigin + (cameraForward * wandRange);

        if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange, enemyLayer))
        {
            targetPoint = hit.point;
        }

        ShootServerRpc(wandMuzzle.position, targetPoint, cameraOrigin, cameraForward, currentProjectileIndex);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint, Vector3 cameraOrigin, Vector3 cameraForward, int projectileIndex)
    {
        if (projectileIndex < 0 || projectileIndex >= projectiles.Length)
            projectileIndex = 0;

        ProjectileData selectedProjectile = projectiles[projectileIndex];

        //Hitscan method
        /*if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange, enemyLayer))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyHealth health = hit.collider.GetComponent<EnemyHealth>();

                if(health != null)
                {
                    Vector3 hitDir = cameraForward;
                    health.TakeDamage(selectedProjectile.damage, hitDir);
                }
                
            }
        }*/

        Vector3 moveDir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(
            selectedProjectile.projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(moveDir)
        );

        if (wandFlash != null)
        {
            Instantiate(wandFlash, spawnPos, Quaternion.LookRotation(moveDir));
        }


        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();
        if (projectile != null)
        {
            projectile.projectileData = selectedProjectile;
        }

        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        if (bulletNetObj != null)
            bulletNetObj.Spawn();


        if (projectile != null)
            projectile.Initialize(moveDir);

        ShootObserversClientRpc();
    }

    public void CycleProjectileForward()
    {
        currentProjectileIndex++;

        if (currentProjectileIndex >= projectiles.Length)
            currentProjectileIndex = 0;

        Debug.Log("Switched to: " + CurrentProjectile.name);
    }

    public void CycleProjectileBackward()
    {
        currentProjectileIndex--;

        if (currentProjectileIndex < 0)
            currentProjectileIndex = projectiles.Length - 1;

        Debug.Log("Switched to: " + CurrentProjectile.name);
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