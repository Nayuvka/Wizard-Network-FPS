using UnityEngine;
using Unity.Netcode;

public class StatueInteractable : NetworkBehaviour, IInteractable
{
    public string promptMessage = "Interact with Statue";
    private bool interacted = false;

    public void Interact(NetworkPlayerController player)
    {
        InteractServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractServerRpc()
    {
        if (interacted) return;
        interacted = true;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                NetworkShoot shooter = client.PlayerObject.GetComponent<NetworkShoot>();
                if (shooter != null)
                {
                    int randomType = Random.Range(0, 4);
                    int damageBoost = SpawnManager.Instance.currentRound.Value * 5;

                    shooter.currentStaffTypeIndex = randomType;
                    shooter.baseStaffDamage += damageBoost;

                    ApplyStatChangeClientRpc(client.ClientId, randomType, shooter.baseStaffDamage);
                }

                PlayerHealth health = client.PlayerObject.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.ResetHealth();
                }
            }
        }

        SpawnManager.Instance.OnStatueInteracted();
    }


    [ClientRpc]
    private void ApplyStatChangeClientRpc(ulong targetClientId, int newType, int newTotalDamage)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null)
            {
                NetworkShoot shooter = localPlayer.GetComponent<NetworkShoot>();
                if (shooter != null)
                {
                    shooter.currentStaffTypeIndex = newType;
                    shooter.baseStaffDamage = newTotalDamage;
                }
            }
        }
    }
}