using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerAbilities : NetworkBehaviour
{
    [SerializeField] private NetworkShoot networkShoot;
    [SerializeField] private NetworkPlayerController playerController;
    
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pillarPrefab;

    [Header("Staff Index Mapping")]
    public int normalStaffIndex = 3;
    public int iceStaffIndex = 1;
    public int lightningStaffIndex = 2;
    public int fireStaffIndex = 0;

    [Header("Wall Settings (Normal)")]
    public float wallSpawnDistance = 3f;
    public float wallLifetime = 5f;
    public float wallCooldown = 5f;

    [Header("Pillar Settings (Ice)")]
    public float pillarLiftHeight = 4.5f;
    public float pillarLifetime = 5f;
    public float pillarCooldown = 6f;

    [Header("Dash Settings (Lightning)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 3f;

    [Header("Launch Settings (Fire)")]
    public float fireLaunchMultiplier = 3.0f;
    public float glideFallSpeed = -2.5f;
    public float glideMoveSpeed = 12f;
    public float fireCooldown = 4f;

    private float currentWallCooldown = 0f;
    private float currentPillarCooldown = 0f;
    private float currentDashCooldown = 0f;
    private float currentFireCooldown = 0f;

    private void Update()
    {
        if (!IsOwner) return;

        if (currentWallCooldown > 0) currentWallCooldown -= Time.deltaTime;
        if (currentPillarCooldown > 0) currentPillarCooldown -= Time.deltaTime;
        if (currentDashCooldown > 0) currentDashCooldown -= Time.deltaTime;
        if (currentFireCooldown > 0) currentFireCooldown -= Time.deltaTime;
    }

    public void OnSecondaryAbility(InputValue value)
    {
        if (!IsOwner || playerController.IsInUIMode() || GameOverManager.IsGameOver) return;

        if (value.isPressed && networkShoot != null)
        {
            int staff = networkShoot.currentStaffTypeIndex;
            Quaternion flatRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            if (staff == normalStaffIndex && currentWallCooldown <= 0f)
            {
                Vector3 spawnPos = transform.position + (flatRotation * Vector3.forward * wallSpawnDistance);
                SpawnAbilityObjectServerRpc(0, spawnPos, flatRotation, wallLifetime);
                currentWallCooldown = wallCooldown;
            }
            else if (staff == iceStaffIndex && currentPillarCooldown <= 0f)
            {
                Vector3 spawnPos = transform.position;
                playerController.LiftPlayerForPillar(pillarLiftHeight);
                SpawnAbilityObjectServerRpc(1, spawnPos, flatRotation, pillarLifetime);
                currentPillarCooldown = pillarCooldown;
            }
            else if (staff == lightningStaffIndex && currentDashCooldown <= 0f)
            {
                if (!playerController.IsDashing())
                {
                    playerController.StartDash(dashDuration, dashSpeed);
                    currentDashCooldown = dashCooldown;
                }
            }
            else if (staff == fireStaffIndex && currentFireCooldown <= 0f)
            {
                playerController.LaunchPlayer(fireLaunchMultiplier);
                currentFireCooldown = fireCooldown;
            }
        }
    }

    [ServerRpc]
    private void SpawnAbilityObjectServerRpc(int type, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject prefab = type == 0 ? wallPrefab : pillarPrefab;
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, position, rotation);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
                StartCoroutine(DestroyAbilityObject(netObj, lifetime));
            }
        }
    }

    private IEnumerator DestroyAbilityObject(NetworkObject netObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }
}