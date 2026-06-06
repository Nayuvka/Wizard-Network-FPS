using UnityEngine;
using Unity.Netcode;
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
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isRespawning = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    private NetworkPlayerController controller;
    private PlayerHealth playerHealth;
    private CharacterController charController;
    private PlayerCombatStats combatStats;

    private Vector3 lastDeathPosition;

    private void Awake()
    {
        controller = GetComponent<NetworkPlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        charController = GetComponent<CharacterController>();
        combatStats = GetComponent<PlayerCombatStats>();
    }

    public override void OnNetworkSpawn()
    {
        if (playerHUD != null) playerHUD.SetActive(IsOwner);

        if (IsServer)
        {
            isRespawning.Value = false;
            respawnTimer.Value = 0f;
        }

        ToggleDeadStateClientRpc(true);
        SwitchDeathCameraClientRpc(false);

        if (controller != null) controller.ToggleControllerClientRpc(true);
        if (playerHealth != null && IsServer) playerHealth.ResetHealth();
    }

    public void RespawnPlayer()
    {
        if (!IsServer || isRespawning.Value) return;

        // Check for Phoenix Phial
        if (combatStats != null && combatStats.hasPhoenixPhial.Value)
        {
            combatStats.ConsumePhoenixPhial();
            PerformInstantRespawn();
            return;
        }

        lastDeathPosition = transform.position;
        StartCoroutine(DelayRespawnRoutine());
    }

    private void PerformInstantRespawn()
    {
        if (playerHealth != null) playerHealth.ResetHealth();
        
        // Instant visual/gameplay reset
        SpawnDeathEffectClientRpc(transform.position);
    }

    private IEnumerator DelayRespawnRoutine()
    {
        isRespawning.Value = true;
        respawnTimer.Value = respawnTime;

        ToggleDeadStateClientRpc(false);
        SwitchDeathCameraClientRpc(true);

        if (controller != null) controller.ToggleControllerClientRpc(false);
        SpawnDeathEffectClientRpc(transform.position);

        while (respawnTimer.Value > 0)
        {
            if (GameOverManager.IsGameOver) yield break;
            yield return new WaitForSeconds(1f);
            respawnTimer.Value -= 1f;
        }

        Transform chosenSpawn = GetRespawnPosition();
        if (chosenSpawn != null)
        {
            TeleportPlayer(chosenSpawn.position, chosenSpawn.rotation);
            if (controller != null) controller.ResetCameraRotationClientRpc(chosenSpawn.rotation);
        }

        if (playerHealth != null) playerHealth.ResetHealth();

        ToggleDeadStateClientRpc(true);
        SwitchDeathCameraClientRpc(false);
        if (controller != null) controller.ToggleControllerClientRpc(true);

        isRespawning.Value = false;
    }

    private Transform GetRespawnPosition()
    {
        if (PlayerSpawnManager.Instance == null) return null;
        return PlayerSpawnManager.Instance.GetBestSpawnPoint(lastDeathPosition);
    }

    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (charController != null) charController.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (charController != null) charController.enabled = true;
    }

    [ClientRpc]
    private void ToggleDeadStateClientRpc(bool isAlive)
    {
        foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.enabled = isAlive;
        foreach (var r in GetComponentsInChildren<MeshRenderer>(true)) r.enabled = isAlive;
        foreach (var a in GetComponentsInChildren<Animator>(true)) a.enabled = isAlive;
        
        NetworkShoot shoot = GetComponent<NetworkShoot>();
        if (shoot != null) shoot.enabled = isAlive;
        
        if (IsOwner && playerHUD != null) playerHUD.SetActive(isAlive);
    }

    [ClientRpc]
    private void SwitchDeathCameraClientRpc(bool showDeathCamera)
    {
        if (!IsOwner) return;
        normalCamera.Priority = showDeathCamera ? inactiveCameraPriority : activeCameraPriority;
        deathCamera.Priority = showDeathCamera ? activeCameraPriority : inactiveCameraPriority;
    }

    [ClientRpc]
    private void SpawnDeathEffectClientRpc(Vector3 deathPosition)
    {
        if (deathParticlePrefab != null) Instantiate(deathParticlePrefab, deathPosition, Quaternion.identity);
    }
}