using UnityEngine;
using Unity.Netcode;

public class CheckpointScript : NetworkBehaviour
{
    private bool isActive;
    [SerializeField] RespawnScript respawn;


    private void OnTriggerEnter(Collider coli)
    {
        if (!IsServer) return;

        if (coli.gameObject.CompareTag("Player")){
            if (!isActive)
            {
                if(coli.TryGetComponent<RespawnScript>(out var respawn))
                {
                    respawn.SetRespawnPoint(transform.position);
                    isActive = true;
                    NotifyCheckpointReachedClientRpc();
                }

            }
        }
    }

    [ClientRpc]
    private void NotifyCheckpointReachedClientRpc()
    {

        Debug.Log("Checkpoint Activated");
    }
}
