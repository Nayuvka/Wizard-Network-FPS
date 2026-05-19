using UnityEngine;
using Unity.Netcode;

public class MinimapScript : NetworkBehaviour
{
    public Transform playerTransform;
    public Camera miniMapCamera;
    public float height = 20f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if(miniMapCamera != null)
            {
                miniMapCamera.enabled = false;
            }
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

        /*transform.rotation = Quaternion.Euler(
            90f,
            playerTransform.eulerAngles.y,
            0f
        );*/

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
