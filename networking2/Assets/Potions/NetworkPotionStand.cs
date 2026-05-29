using Unity.Netcode;
using UnityEngine;

public class NetworkPotionStand : NetworkBehaviour
{
    [SerializeField] private int potionCost = 2500;
    [SerializeField] private float upgradedHealthAmount = 150f;
    [SerializeField] private AudioClip buySound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out NetworkObject netObj) && netObj.IsOwner)
        {
            TryBuyPotionServerRpc(netObj.OwnerClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryBuyPotionServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerCombatStats stats))
            {
                bool success = stats.TryBuyHealthPerk(potionCost, upgradedHealthAmount);
                
                if (success)
                {
                    PlayEffectsClientRpc(transform.position);
                }
            }
        }
    }

    [ClientRpc]
    private void PlayEffectsClientRpc(Vector3 pos)
    {
        if (buySound != null)
        {
            AudioSource.PlayClipAtPoint(buySound, pos);
        }
    }
}