using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using TMPro;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;
    public GameObject[] enemySpawnPoints;
    public List<NetworkObject> spawnedList = new List<NetworkObject>();
    public float enemySpawnDelay;
    public int baseSpawnAmount;
    public int spawnIncrease;
    public int difficultyInterval;
    private int currentDifficulty = 1;

    [Header("Boss 1 Settings")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public int bossRound = 6;

    [Header("Boss 2 Settings")]
    public GameObject boss2Prefab;
    public Transform boss2SpawnPoint;
    public int boss2Round = 12;

    [Header("UI References")]
    public NetworkVariable<int> currentRound = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public static System.Action<int> OnRoundStarted;
    public TMP_Text bossAnnouncementText;

    [Header("Statue Settings")]
    public GameObject statuePrefab;
    public Transform[] statueSpawnPoints; 
    private GameObject activeStatue;

    [Header("Visuals")]
    public GameObject spawnIndicatorPrefab;
    public float spawnIndicatorTime = 1.5f;
    public float indicatorHeight = 3f;

    [Header("Round Start")]
    [SerializeField] private float roundStartDelay = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PrepareNextRound();
        }

        if (bossAnnouncementText != null)
        {
            bossAnnouncementText.gameObject.SetActive(false);
        }
    }

    public void PrepareNextRound()
    {
        if (!IsServer)
            return;

        currentRound.Value++;

        OnRoundStarted?.Invoke(currentRound.Value);

        if (statuePrefab != null &&
            statueSpawnPoints.Length >= currentRound.Value)
        {
            Vector3 spawnPos =
                statueSpawnPoints[
                    currentRound.Value - 1]
                .position;

            activeStatue =
                Instantiate(
                    statuePrefab,
                    spawnPos,
                    statueSpawnPoints[
                        currentRound.Value - 1]
                    .rotation);

            activeStatue
                .GetComponent<NetworkObject>()
                .Spawn();
        }
        else
        {
            StartCoroutine(
                BeginEnemySpawning());
        }
    }

    public void OnStatueInteracted()
    {
        if (!IsServer)
            return;

        if (activeStatue != null)
        {
            activeStatue
                .GetComponent<NetworkObject>()
                .Despawn();

            activeStatue = null;
        }
        StartBattleMusicClientRpc();
        StartCoroutine(DelayedRoundStart());
    }

    IEnumerator BeginEnemySpawning()
    {
        int spawnAmount = baseSpawnAmount + (spawnIncrease * currentRound.Value);

        if (currentRound.Value == bossRound)
        {
            ToggleBossUIClientRpc(true, "Defeat the Boss!");
            
            Vector3 bPos = bossSpawnPoint != null ? bossSpawnPoint.position : enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)].transform.position;
            StartCoroutine(SpawnEnemy(bossPrefab, bPos, 0f));

            spawnAmount /= 2;
        }
        else if (currentRound.Value == boss2Round)
        {
            ToggleBossUIClientRpc(true, "Defeat the Wizard Boss!");
            
            Vector3 bPos = boss2SpawnPoint != null ? boss2SpawnPoint.position : enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)].transform.position;
            StartCoroutine(SpawnEnemy(boss2Prefab, bPos, 0f));

            spawnAmount /= 2;
        }

        if (currentRound.Value % difficultyInterval == 0)
        {
            if (currentDifficulty < enemyPrefabs.Length)
            {
                currentDifficulty++;
            }
        }

        for (int index = 0; index < spawnAmount; index++)
        {
            int randomEnemyIndex = Random.Range(0, currentDifficulty);
            GameObject prefabToSpawn = enemyPrefabs[randomEnemyIndex];
            int randomIndex = Random.Range(0, enemySpawnPoints.Length);
            float spawnTimeOffset = index * enemySpawnDelay;
            StartCoroutine(SpawnEnemy(prefabToSpawn, enemySpawnPoints[randomIndex].transform.position, spawnTimeOffset));
        }
        yield return null;
    }

    public void SkipToNextRound()
    {
        if (!IsServer)
            return;

        foreach (NetworkObject enemy in spawnedList.ToArray())
        {
            if (enemy != null && enemy.IsSpawned)
            {
                enemy.Despawn();
            }
        }

        spawnedList.Clear();

        if (activeStatue != null)
        {
            NetworkObject statueNetObj =
                activeStatue.GetComponent<NetworkObject>();

            if (statueNetObj != null &&
                statueNetObj.IsSpawned)
            {
                statueNetObj.Despawn();
            }

            activeStatue = null;
        }

        StopAllCoroutines();

        PrepareNextRound();
    }

    public void ForceRound(int round)
    {
        if (!IsServer)
            return;

        foreach (NetworkObject enemy in spawnedList.ToArray())
        {
            if (enemy != null &&
                enemy.IsSpawned)
            {
                enemy.Despawn();
            }
        }

        spawnedList.Clear();

        if (activeStatue != null)
        {
            NetworkObject statueNetObj =
                activeStatue.GetComponent<NetworkObject>();

            if (statueNetObj != null &&
                statueNetObj.IsSpawned)
            {
                statueNetObj.Despawn();
            }

            activeStatue = null;
        }

        StopAllCoroutines();

        currentRound.Value = round - 1;

        PrepareNextRound();
    }

    IEnumerator SpawnEnemy(GameObject enemyPrefab, Vector3 spawnPosition, float spawnTimeOffset)
    {
        yield return new WaitForSeconds(spawnTimeOffset);

        Vector3 indicatorPos = spawnPosition + Vector3.up * indicatorHeight;
        GameObject indicator = Instantiate(spawnIndicatorPrefab, indicatorPos, Quaternion.Euler(90, 0, 0));
        NetworkObject indicatorNet = indicator.GetComponent<NetworkObject>();
        indicatorNet.Spawn();

        yield return new WaitForSeconds(spawnIndicatorTime);

        if (indicatorNet != null && indicatorNet.IsSpawned) indicatorNet.Despawn();

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = spawnedEnemy.GetComponent<NetworkObject>();
        netObj.Spawn();

        spawnedList.Add(netObj);
    }

    private IEnumerator DelayedRoundStart()
    {
        ShowRoundUIClientRpc(currentRound.Value);

        yield return new WaitForSeconds(roundStartDelay);

        StartCoroutine(BeginEnemySpawning());
    }

    public void EnemyDeath(NetworkObject enemy)
    {
        if (!IsServer) return;

        if (spawnedList.Contains(enemy))
        {
            spawnedList.Remove(enemy);
        }

        if (spawnedList.Count == 0)
        {
            StopBattleMusicClientRpc();
            if (currentRound.Value == bossRound || currentRound.Value == boss2Round)
            {
                ToggleBossUIClientRpc(false, "");
            }
            PrepareNextRound();
        }
    }

    [ClientRpc]
    private void ToggleBossUIClientRpc(bool show, string text)
    {
        if (bossAnnouncementText != null)
        {
            bossAnnouncementText.text = text;
            bossAnnouncementText.gameObject.SetActive(show);
        }
    }

    [ClientRpc]
    private void ShowRoundUIClientRpc(int round)
    {
        RoundDisplayUI roundUI =
            FindFirstObjectByType<RoundDisplayUI>();

        if (roundUI != null)
        {
            roundUI.ShowRound(round);
        }
    }

    [ClientRpc]
    private void StartBattleMusicClientRpc()
    {
        if (BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.StartBattleMusic();
        }
    }

    [ClientRpc]
    private void StopBattleMusicClientRpc()
    {
        if (BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.StopBattleMusic();
        }
    }
}