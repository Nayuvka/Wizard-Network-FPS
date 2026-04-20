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
        if (playerCamera == null) playerCamera = Camera.main;

        playerInput = GetComponentInParent<PlayerInput>();
        shootAction = playerInput.actions["Shoot"];
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;
        if (shootAction.triggered && canShoot) ProcessLocalShoot();
    }

    void ProcessLocalShoot()
    {
        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");
        StartCoroutine(ShootTimer());

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, wandRange))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(wandRange);
        }

        ShootServerRpc(wandMuzzle.position, targetPoint);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint)
    {
        Vector3 moveDir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(moveDir));
        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        bulletNetObj.Spawn();
        
        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();
        if (projectile != null) projectile.Initialize(moveDir);

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