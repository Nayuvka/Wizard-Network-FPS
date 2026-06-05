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

    private int dashCharges = 3;
    private float dashChargeTimer = 0f;

    private void Update()
    {
        if (!IsOwner) return;

        if (dashCharges < 3)
        {
            dashChargeTimer += Time.deltaTime;
            if (dashChargeTimer >= 3f)
            {
                dashCharges++;
                dashChargeTimer = 0f;
            }
        }
    }

    public void OnSecondaryAbility(InputValue value)
    {
        if (!IsOwner || playerController.IsInUIMode() || GameOverManager.IsGameOver) return;

        if (value.isPressed && networkShoot != null)
        {
            int staff = networkShoot.currentStaffTypeIndex;

            if (staff == 0)
            {
                Vector3 spawnPos = transform.position + transform.forward * 3f;
                SpawnAbilityObjectServerRpc(0, spawnPos, transform.rotation);
            }
            else if (staff == 1)
            {
                Vector3 spawnPos = transform.position - new Vector3(0, 1.2f, 0);
                SpawnAbilityObjectServerRpc(1, spawnPos, transform.rotation);
            }
            else if (staff == 2)
            {
                if (dashCharges > 0 && !playerController.IsDashing())
                {
                    dashCharges--;
                    playerController.StartDash(0.2f);
                }
            }
        }
    }

    [ServerRpc]
    private void SpawnAbilityObjectServerRpc(int type, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = type == 0 ? wallPrefab : pillarPrefab;
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, position, rotation);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
                StartCoroutine(DestroyAbilityObject(netObj, 5f));
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