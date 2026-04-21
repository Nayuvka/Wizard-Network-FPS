using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerHeadTilt : NetworkBehaviour
{
    [SerializeField] private Rig rig;
    [SerializeField] private Transform headTarget;
    [SerializeField] private Transform playerCamera;

    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float smoothSpeed = 10f;

    [SerializeField] private float maxLookUp = 60f;
    [SerializeField] private float maxLookDown = 45f;

    private float currentWeight;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        rig = GetComponentInChildren<Rig>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleHeadLook();
        HandleRigWeight();
    }

    private void HandleHeadLook()
    {
        if (headTarget == null || playerCamera == null) return;

        Vector3 aimDirection = playerCamera.forward;

        // Create a world point in front of the camera
        headTarget.position = playerCamera.position + aimDirection * 2f;

        // Optional but safe: align rotation too
        headTarget.rotation = Quaternion.LookRotation(aimDirection);
    }

    private void HandleRigWeight()
    {
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * smoothSpeed);
        rig.weight = currentWeight;
    }
}