using Unity.Netcode;
using UnityEngine;

public class RespawnScript : NetworkBehaviour
{
    private NetworkVariable<Vector3> currentSpawnPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //Vector3 currentSpawnPosition;
    

    public void SetRespawnPoint(Vector3 lastPosition)
    {
        if (!IsServer) return;
        currentSpawnPosition.Value = lastPosition;
    }

    public void RespawnPlayer()
    {
        if(!IsServer) return;

        CharacterController controller = GetComponent<CharacterController>();
        if(controller != null)
        {
            controller.enabled = false;
        }
        transform.position = currentSpawnPosition.Value;
        controller.enabled = true;

    }
}
