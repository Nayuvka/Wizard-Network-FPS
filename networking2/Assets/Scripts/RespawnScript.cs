using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class RespawnScript : NetworkBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 5f;

    [Header("References")]
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject deathParticlePrefab;

    [Header("Death Camera")]
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private CinemachineCamera deathCamera;
    [SerializeField] private int activeCameraPriority = 20;
    [SerializeField] private int inactiveCameraPriority = 5;

    public NetworkVariable<float> respawnTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isRespawning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkPlayerController controller;
    private PlayerHealth playerHealth;
    private CharacterController charController;

    private Vector3 lastDeathPosition;

    private void Awake()
    {
        controller = GetComponent<NetworkPlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        charController = GetComponent<CharacterController>();
    }

    public void RespawnPlayer()
    {
        if (!IsServer || isRespawning.Value) return;

        lastDeathPosition = transform.position;
        StartCoroutine(DelayRespawnRoutine());
    }

    private IEnumerator DelayRespawnRoutine()
    {
        isRespawning.Value = true;
        respawnTimer.Value = respawnTime;

        ToggleDeadStateClientRpc(false);
        SwitchDeathCameraClientRpc(true);

        if (controller != null)
        {
            controller.ToggleControllerClientRpc(false);
        }

        SpawnDeathEffectClientRpc(transform.position);

        while (respawnTimer.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            respawnTimer.Value -= 1f;
        }

        Transform chosenSpawn = GetRespawnPosition();

        if (chosenSpawn != null)
        {
            TeleportPlayer(chosenSpawn.position, chosenSpawn.rotation);

            if (controller != null)
            {
                controller.ResetCameraRotationClientRpc(chosenSpawn.rotation);
            }
        }

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        ToggleDeadStateClientRpc(true);
        SwitchDeathCameraClientRpc(false);

        if (controller != null)
        {
            controller.ToggleControllerClientRpc(true);
        }

        isRespawning.Value = false;
    }

    private Transform GetRespawnPosition()
    {
        if (PlayerSpawnManager.Instance == null)
        {
            Debug.LogWarning("No PlayerSpawnManager found in the scene.");
            return null;
        }

        return PlayerSpawnManager.Instance.GetBestSpawnPoint(lastDeathPosition);
    }

    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (charController != null)
        {
            charController.enabled = false;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (charController != null)
        {
            charController.enabled = true;
        }
    }

    [ClientRpc]
    private void ToggleDeadStateClientRpc(bool isAlive)
    {
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            skinnedRenderer.enabled = isAlive;
        }

        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.enabled = isAlive;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator anim in animators)
        {
            anim.enabled = isAlive;
        }

        NetworkShoot shoot = GetComponent<NetworkShoot>();

        if (shoot != null)
        {
            shoot.enabled = isAlive;
        }

        if (IsOwner && playerHUD != null)
        {
            playerHUD.SetActive(isAlive);
        }
    }

    [ClientRpc]
    private void SwitchDeathCameraClientRpc(bool showDeathCamera)
    {
        if (!IsOwner) return;

        if (normalCamera == null || deathCamera == null)
        {
            Debug.LogWarning("RespawnScript: normalCamera or deathCamera is not assigned.");
            return;
        }

        normalCamera.Priority = showDeathCamera ? inactiveCameraPriority : activeCameraPriority;
        deathCamera.Priority = showDeathCamera ? activeCameraPriority : inactiveCameraPriority;
    }

    [ClientRpc]
    private void SpawnDeathEffectClientRpc(Vector3 deathPosition)
    {
        if (deathParticlePrefab != null)
        {
            Instantiate(deathParticlePrefab, deathPosition, Quaternion.identity);
        }
    }
}