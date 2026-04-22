using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class RespawnScript : NetworkBehaviour
{
    private NetworkVariable<Vector3> currentSpawnPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Quaternion> currentSpawnRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<float> respawnTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isRespawning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject deathParticlePrefab;

    public void SetRespawnPoint(Vector3 lastPosition, Quaternion lastRotation)
    {
        if (!IsServer) return;
        currentSpawnPosition.Value = lastPosition;
        currentSpawnRotation.Value = lastRotation;
    }

    public void RespawnPlayer()
    {
        if (!IsServer || isRespawning.Value) return;
        StartCoroutine(DelayRespawnRoutine());
    }

    private IEnumerator DelayRespawnRoutine()
    {
        isRespawning.Value = true;
        respawnTimer.Value = respawnTime;

        var controller = GetComponent<NetworkPlayerController>();

        controller.ToggleControllerClientRpc(false);
        TogglePlayerVisibilityClientRpc(false);

        while (respawnTimer.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            respawnTimer.Value -= 1f;
        }

        transform.position = currentSpawnPosition.Value;
        controller.ResetCameraRotationClientRpc(currentSpawnRotation.Value);

        controller.ToggleControllerClientRpc(true);
        TogglePlayerVisibilityClientRpc(true);
        
        isRespawning.Value = false;
    }

    [ClientRpc]
    private void TogglePlayerVisibilityClientRpc(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            ren.enabled = isVisible;
        }

        if (IsOwner && playerHUD != null)
        {
            playerHUD.SetActive(isVisible);
        }

        if (!isVisible)
        {
            SpawnDeathEffect();
        }
    }

    private void SpawnDeathEffect()
    {
        if (deathParticlePrefab != null)
        {
            Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        }
    }
}