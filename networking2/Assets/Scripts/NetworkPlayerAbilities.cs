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

    [Header("Ability Settings")]
    public float abilityCooldown = 2f;
    public float wallSpawnDistance = 3f;
    public float wallLifetime = 5f;
    public float pillarLiftHeight = 4.5f;
    public float pillarLifetime = 5f;
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashRechargeTime = 3f;
    public float fireLaunchMultiplier = 3.0f;

    private int dashCharges = 3;
    private float dashChargeTimer = 0f;
    private float currentCooldown = 0f;

    private void Update()
    {
        if (!IsOwner) return;

        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        if (dashCharges < 3)
        {
            dashChargeTimer += Time.deltaTime;
            if (dashChargeTimer >= dashRechargeTime)
            {
                dashCharges++;
                dashChargeTimer = 0f;
            }
        }
    }

    public void OnSecondaryAbility(InputValue value)
    {
        if (!IsOwner || playerController.IsInUIMode() || GameOverManager.IsGameOver) return;

        if (value.isPressed && networkShoot != null && currentCooldown <= 0f)
        {
            int staff = networkShoot.currentStaffTypeIndex;

            Quaternion flatRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            bool abilityUsed = false;

            if (staff == normalStaffIndex)
            {
                Vector3 spawnPos = transform.position + (flatRotation * Vector3.forward * wallSpawnDistance);
                SpawnAbilityObjectServerRpc(0, spawnPos, flatRotation, wallLifetime);
                abilityUsed = true;
            }
            else if (staff == iceStaffIndex)
            {
                Vector3 spawnPos = transform.position;
                playerController.LiftPlayerForPillar(pillarLiftHeight);
                SpawnAbilityObjectServerRpc(1, spawnPos, flatRotation, pillarLifetime);
                abilityUsed = true;
            }
            else if (staff == lightningStaffIndex)
            {
                if (dashCharges > 0 && !playerController.IsDashing())
                {
                    dashCharges--;
                    playerController.StartDash(dashDuration, dashSpeed);
                    abilityUsed = true;
                }
            }
            else if (staff == fireStaffIndex)
            {
                playerController.LaunchPlayer(fireLaunchMultiplier);
                abilityUsed = true;
            }

            if (abilityUsed)
            {
                currentCooldown = abilityCooldown;
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