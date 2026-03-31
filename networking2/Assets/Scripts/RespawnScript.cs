using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RespawnScript : NetworkBehaviour
{
    private NetworkVariable<Vector3> currentSpawnPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Quaternion> currentSpawnRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private GameObject playerHUD;
    public void SetRespawnPoint(Vector3 lastPosition, Quaternion lastRotation)
    {
        if (!IsServer) return;
        currentSpawnPosition.Value = lastPosition;
        currentSpawnRotation.Value = lastRotation;
    }

    public void RespawnPlayer()
    {
        if(!IsServer) return;

        //transform.position = currentSpawnPosition.Value;
        //controller.enabled = true;
        //meshRenderer.enabled=true;
        StartCoroutine(DelayRespawn());

    }

    private IEnumerator DelayRespawn()
    {
        var controller = GetComponent<NetworkPlayerController>();

        controller.ToggleControllerClientRpc(false);
        TogglePlayerVisibilityClientRpc(false);

        yield return new WaitForSeconds(5f);

        transform.position = currentSpawnPosition.Value;
        controller.ResetCameraRotationClientRpc(currentSpawnRotation.Value);

        controller.ToggleControllerClientRpc(true);
        TogglePlayerVisibilityClientRpc(true);
    }

    [ClientRpc]
    private void TogglePlayerVisibilityClientRpc(bool isVisible)
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.enabled = isVisible;

        foreach(MeshRenderer mesh in meshRenderers)
        {
            if (mesh != null) mesh.enabled = isVisible;
        }

        if (IsOwner && playerHUD != null)
        {
            playerHUD.SetActive(isVisible);
        }


    }
}
