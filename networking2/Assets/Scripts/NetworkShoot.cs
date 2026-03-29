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
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject projectilePrefab;

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

        ShootServerRpc(wandMuzzle.position, targetPoint, cameraOrigin, cameraForward);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint, Vector3 cameraOrigin, Vector3 cameraForward)
    {
        if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange, enemyLayer))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                NetworkObject enemyNetObj = hit.collider.GetComponent<NetworkObject>();
                if (enemyNetObj != null)
                    enemyNetObj.Despawn();
            }
        }

        Vector3 moveDir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(moveDir));

        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();

        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        if (bulletNetObj != null)
            bulletNetObj.Spawn();

        if (projectile != null)
            projectile.Initialize(moveDir);

        ShootObserversClientRpc();
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