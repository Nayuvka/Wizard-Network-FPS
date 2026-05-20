using UnityEngine;
using Unity.Netcode;

public class PlayerMinimapMarker : NetworkBehaviour
{
    [Header("World Minimap Marker")]
    public GameObject worldMinimapMarker;

    [Header("Local Player Minimap")]
    public Camera miniMapCamera;
    //public GameObject localPlayerIcon;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Local Player
            if (worldMinimapMarker != null)
            {
                worldMinimapMarker.SetActive(false);
            }

            /*if (localPlayerIcon != null)
            {
                localPlayerIcon.SetActive(true);
            }*/
        }
        else
        {
            // Other Player
            if (worldMinimapMarker != null)
            {
                worldMinimapMarker.SetActive(true);
            }
            /*if (localPlayerIcon != null)
            {
                localPlayerIcon.SetActive(false);
            }*/
        }
    }
}