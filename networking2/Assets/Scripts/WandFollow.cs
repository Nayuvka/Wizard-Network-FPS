using Unity.Netcode;
using UnityEngine;

public class WandFollow : NetworkBehaviour
{
    [SerializeField] Transform wand;
    [SerializeField] Transform cameraTransform;

    [SerializeField] Vector3 positionOffset = new Vector3(0.3f, -0.3f, 0.6f);
    [SerializeField] Vector3 rotationOffset;

    void LateUpdate()
    {
        if (!IsOwner) return;

        wand.position = cameraTransform.position + cameraTransform.TransformDirection(positionOffset);
        wand.rotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);
    }


}
