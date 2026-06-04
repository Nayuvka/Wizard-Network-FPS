using UnityEngine;
using Unity.Netcode;

public class BillboardUI : MonoBehaviour
{
    private Camera targetCamera;

    private void Start()
    {
        FindLocalPlayerCamera();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            FindLocalPlayerCamera();
            return;
        }

        Vector3 direction =
            targetCamera.transform.position -
            transform.position;


        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(-direction);
        }
    }

    private void FindLocalPlayerCamera()
    {
        NetworkPlayerController[] players =
            FindObjectsByType<NetworkPlayerController>(
                FindObjectsSortMode.None);

        foreach (NetworkPlayerController player in players)
        {
            if (player.IsOwner)
            {
                targetCamera = player.playerCamera;
                return;
            }
        }
    }
}