using Unity.Netcode;
using UnityEngine;

public enum PotionType
{
    VitalityVial,
    SwiftSyrup,
    FrenzyFlask,
    PhoenixPhial
}

public class NetworkPotionStand : NetworkBehaviour, IInteractable
{
    [Header("Potion Settings")]
    [SerializeField] private PotionType potionToSell;

    [SerializeField]
    private int potionCost = 2500;

    [Header("Audio")]
    [SerializeField]
    private AudioClip buySound;

    public string promptMessage =>
        $"Buy {potionToSell} [{potionCost}]";

    public void Interact(NetworkPlayerController player)
    {
        if (!NetworkManager.Singleton.IsClient)
            return;

        TryBuyPotionServerRpc(
            NetworkManager.Singleton.LocalClientId
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryBuyPotionServerRpc(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            return;

        if (client.PlayerObject == null)
            return;

        if (!client.PlayerObject.TryGetComponent(
                out PlayerCombatStats stats))
            return;

        bool success = stats.TryBuyPotion(potionCost,potionToSell);

        if (!success)
            return;

        PlayEffectsClientRpc(transform.position);
    }

    [ClientRpc]
    private void PlayEffectsClientRpc(Vector3 position)
    {
        if (buySound != null)
        {
            AudioSource.PlayClipAtPoint(buySound,position);
        }
    }
}