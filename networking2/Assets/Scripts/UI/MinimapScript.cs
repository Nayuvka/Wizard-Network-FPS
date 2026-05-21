using UnityEngine;
using Unity.Netcode;

public class MinimapScript : NetworkBehaviour
{
    public Transform playerTransform;
    public Camera miniMapCamera;
    public float height = 20f;

    [Header("Player Icon UI")]
    public GameObject miniMapCanvas;
    public RectTransform playerIcon;   
    public RectTransform arrowPivot;   

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (miniMapCamera != null)
            {
                miniMapCamera.enabled = false;
            }

            if(miniMapCanvas != null)
            {
                miniMapCanvas.SetActive(false);
            }

            if (playerIcon != null)
            {
                playerIcon.gameObject.SetActive(false);
            }

            enabled = false;
            return;
        }

        if (miniMapCanvas != null)
        {
            miniMapCanvas.SetActive(true);
        }

        if (miniMapCamera != null)
        {
            miniMapCamera.enabled = true;
        }

        if (playerIcon != null)
        {
            playerIcon.gameObject.SetActive(true);
            playerIcon.anchoredPosition = Vector2.zero;
        }
    }

    public void LateUpdate()
    {
        if (!IsOwner || playerTransform == null) return;

        transform.position = new Vector3(
            playerTransform.position.x,
            playerTransform.position.y + height,
            playerTransform.position.z
        );


        transform.rotation = Quaternion.Euler(90f, 0f, 0f);


        if (playerIcon != null)
        {
            playerIcon.anchoredPosition = Vector2.zero;
            playerIcon.localRotation = Quaternion.identity;
        }

        if (arrowPivot != null)
        {
            arrowPivot.localRotation = Quaternion.Euler(
                0f,
                0f,
                -playerTransform.eulerAngles.y
            );
        }
    }
}