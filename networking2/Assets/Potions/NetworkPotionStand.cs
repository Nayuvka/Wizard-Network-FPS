using Unity.Netcode;
using UnityEngine;
using TMPro;

public enum PotionType
{
    VitalityVial,
    SwiftSyrup,
    FrenzyFlask,
    PhoenixPhial
}

public class NetworkPotionStand : NetworkBehaviour
{
    [Header("Potion Settings")]
    public PotionType potionToSell;
    [SerializeField] private int potionCost = 2500;
    
    [Header("Effects")]
    [SerializeField] private AudioClip buySound;

    [Header("UI Interaction")]
    [SerializeField] private GameObject interactCanvas;
    [SerializeField] private TMP_Text interactText;

    private void Start()
    {
        if (interactCanvas != null) 
            interactCanvas.SetActive(false);
        
        if (interactText != null)
        {
            interactText.text = $"Press 'E' to buy {potionToSell} [{potionCost}]";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out NetworkPlayerController player) && player.IsOwner)
        {
            if (interactCanvas != null) interactCanvas.SetActive(true);
            player.nearbyPotionStand = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out NetworkPlayerController player) && player.IsOwner)
        {
            if (interactCanvas != null) interactCanvas.SetActive(false);
            if (player.nearbyPotionStand == this) 
                player.nearbyPotionStand = null;
        }
    }

    public void OnPlayerInteract(NetworkPlayerController player)
    {
        TryBuyPotionServerRpc(player.OwnerClientId, potionToSell);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryBuyPotionServerRpc(ulong clientId, PotionType typeRequested)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerCombatStats stats))
            {
                bool success = stats.TryBuyPotion(potionCost, typeRequested);
                
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