using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class NetworkPlayerAbilities : NetworkBehaviour
{
    [SerializeField] private NetworkShoot networkShoot;
    [SerializeField] private NetworkPlayerController playerController;
    
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pillarPrefab;

    [Header("Ability UI")]
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private TextMeshProUGUI cooldownText;

    [Header("Ability Icons")]
    [SerializeField] private Sprite wallIcon;
    [SerializeField] private Sprite pillarIcon;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite fireIcon;

    [Header("Staff Index Mapping")]
    public int normalStaffIndex = 3;
    public int iceStaffIndex = 1;
    public int lightningStaffIndex = 2;
    public int fireStaffIndex = 0;

    [Header("Wall Settings (Normal)")]
    public float wallSpawnDistance = 3f;
    public float wallLifetime = 5f;
    public float wallCooldown = 5f;

    [Header("Pillar Settings (Ice)")]
    public float pillarLiftHeight = 4.5f;
    public float pillarLifetime = 5f;
    public float pillarCooldown = 6f;

    [Header("Dash Settings (Lightning)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 3f;

    [Header("Launch Settings (Fire)")]
    public float fireLaunchMultiplier = 3.0f;
    public float glideFallSpeed = -2.5f;
    public float glideMoveSpeed = 12f;
    public float fireCooldown = 4f;

    private float currentWallCooldown = 0f;
    private float currentPillarCooldown = 0f;
    private float currentDashCooldown = 0f;
    private float currentFireCooldown = 0f;
    
    private int lastStaffIndex = -1;

    private void Start()
    {
        UpdateAbilityUI();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (currentWallCooldown > 0) currentWallCooldown -= Time.deltaTime;
        if (currentPillarCooldown > 0) currentPillarCooldown -= Time.deltaTime;
        if (currentDashCooldown > 0) currentDashCooldown -= Time.deltaTime;
        if (currentFireCooldown > 0) currentFireCooldown -= Time.deltaTime;

        if (networkShoot != null)
        {
            int currentStaff = networkShoot.currentStaffTypeIndex;

            if (currentStaff != lastStaffIndex)
            {
                if (lastStaffIndex != -1)
                {
                    if (currentStaff == normalStaffIndex) currentWallCooldown = wallCooldown;
                    else if (currentStaff == iceStaffIndex) currentPillarCooldown = pillarCooldown;
                    else if (currentStaff == lightningStaffIndex) currentDashCooldown = dashCooldown;
                    else if (currentStaff == fireStaffIndex) currentFireCooldown = fireCooldown;
                }
                lastStaffIndex = currentStaff;
            }

            UpdateAbilityUI();
        }
    }

    public void OnSecondaryAbility(InputValue value)
    {
        if (!IsOwner || playerController.IsInUIMode() || IsGameActuallyOver()) return;

        if (value.isPressed && networkShoot != null)
        {
            int staff = networkShoot.currentStaffTypeIndex;
            Quaternion flatRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            if (staff == normalStaffIndex && currentWallCooldown <= 0f)
            {
                Vector3 spawnPos = transform.position + (flatRotation * Vector3.forward * wallSpawnDistance);
                SpawnAbilityObjectServerRpc(0, spawnPos, flatRotation, wallLifetime);
                currentWallCooldown = wallCooldown;
            }
            else if (staff == iceStaffIndex && currentPillarCooldown <= 0f)
            {
                Vector3 spawnPos = transform.position;
                playerController.LiftPlayerForPillar(pillarLiftHeight);
                SpawnAbilityObjectServerRpc(1, spawnPos, flatRotation, pillarLifetime);
                currentPillarCooldown = pillarCooldown;
            }
            else if (staff == lightningStaffIndex && currentDashCooldown <= 0f)
            {
                if (!playerController.IsDashing())
                {
                    playerController.StartDash(dashDuration, dashSpeed);
                    currentDashCooldown = dashCooldown;
                }
            }
            else if (staff == fireStaffIndex && currentFireCooldown <= 0f)
            {
                playerController.LaunchPlayer(fireLaunchMultiplier);
                currentFireCooldown = fireCooldown;
            }
        }
    }

    [ServerRpc]
    private void SpawnAbilityObjectServerRpc(int type, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject prefab = type == 0 ? wallPrefab : pillarPrefab;
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, position, rotation);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
                StartCoroutine(DestroyAbilityObject(netObj, lifetime));
            }
        }
    }

    private IEnumerator DestroyAbilityObject(NetworkObject netObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (netObj != null && netObj.IsSpawned)
        {
            UnityEngine.AI.NavMeshObstacle obstacle = netObj.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle != null)
            {
                obstacle.carving = false;
                obstacle.enabled = false;
            }
            
            netObj.Despawn();
        }
    }

    private void UpdateAbilityUI()
    {
        if (networkShoot == null) return;

        int currentStaff = networkShoot.currentStaffTypeIndex;

        float currentCooldown = 0f;
        float maxCooldown = 1f;

        string abilityName = "";
        Sprite icon = null;

        if (currentStaff == normalStaffIndex)
        {
            currentCooldown = currentWallCooldown;
            maxCooldown = wallCooldown;
            abilityName = "Stone Wall";
            icon = wallIcon;
        }
        else if (currentStaff == iceStaffIndex)
        {
            currentCooldown = currentPillarCooldown;
            maxCooldown = pillarCooldown;
            abilityName = "Ice Pillar";
            icon = pillarIcon;
        }
        else if (currentStaff == lightningStaffIndex)
        {
            currentCooldown = currentDashCooldown;
            maxCooldown = dashCooldown;
            abilityName = "Lightning Dash";
            icon = dashIcon;
        }
        else if (currentStaff == fireStaffIndex)
        {
            currentCooldown = currentFireCooldown;
            maxCooldown = fireCooldown;
            abilityName = "Phoenix Leap";
            icon = fireIcon;
        }

        if (abilityIcon != null)
            abilityIcon.sprite = icon;

        if (abilityNameText != null)
            abilityNameText.text = abilityName;

        if (cooldownText != null)
        {
            cooldownText.text = currentCooldown > 0f
                ? $"{currentCooldown:F1}s"
                : "Ready";
        }

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = 1f - Mathf.Clamp01(
    currentCooldown / maxCooldown
);
        }
    }

    private bool IsGameActuallyOver()
    {
        try
        {
            return GameOverManager.IsGameOver;
        }
        catch
        {
            return false;
        }
    }
}