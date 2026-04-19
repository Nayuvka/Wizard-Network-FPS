using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NetworkCameraShake : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float recoilBack = 0.12f;
    [SerializeField] private float recoilUp = 0.05f;
    [SerializeField] private float noiseAmount = 0.02f;

    [SerializeField]
    private AnimationCurve recoilCurve =
        AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Vector3 originalLocalPos;
    private Coroutine recoilCoroutine;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        if (cameraTransform == null)
            cameraTransform = transform;

        originalLocalPos = cameraTransform.localPosition;
    }

    public void ShakeCamera()
    {
        if (recoilCoroutine != null)
            StopCoroutine(recoilCoroutine);

        recoilCoroutine = StartCoroutine(CameraShake());
    }

    private IEnumerator CameraShake()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float strength = recoilCurve.Evaluate(t);

            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * noiseAmount,
                recoilUp * strength,
                -recoilBack * strength
            );

            cameraTransform.localPosition = originalLocalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalLocalPos;
        recoilCoroutine = null;
    }
}