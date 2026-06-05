using UnityEngine;
using Unity.Netcode;

public class StatueInteractable : NetworkBehaviour
{
    private bool interacted = false;

    private void OnTriggerEnter(Collider other)
    {
        // Must run on Server to modify NetworkVariables and iterate clients
        if (!IsServer || interacted) return;

        // Check if a player touched it (doesn't matter if it's P1 or P2)
        if (other.CompareTag("Player"))
        {
            interacted = true;

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    NetworkShoot shooter = client.PlayerObject.GetComponent<NetworkShoot>();
                    if (shooter != null)
                    {
                        // Generate individual random stats
                        int randomType = Random.Range(0, 4);
                        int damageBoost = SpawnManager.Instance.currentRound.Value * 5;

                        // Apply to the shooter on the server
                        // This updates the NetworkVariables in NetworkShoot
                        shooter.currentStaffTypeIndex = randomType;
                        shooter.baseStaffDamage += damageBoost;

                        // Force the client to update their local variables immediately
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
            
            // Optional: Despawn or disable statue visual here
            // GetComponent<NetworkObject>().Despawn(); 
        }
    }

    [ClientRpc]
    private void ApplyStatChangeClientRpc(ulong targetClientId, int newType, int newTotalDamage)
    {
        // Each player checks if this message is for them
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