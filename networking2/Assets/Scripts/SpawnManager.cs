using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;

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

    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public int bossRound = 6;

    [Header("Round UI")]
    public NetworkVariable<int> currentRound = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Statue Settings")]
    public GameObject statuePrefab;
    public Transform[] statueSpawnPoints; 
    private GameObject activeStatue;

    [Header("Visuals")]
    public GameObject spawnIndicatorPrefab;
    public float spawnIndicatorTime = 1.5f;
    public float indicatorHeight = 3f;

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
    }

    public void PrepareNextRound()
    {
        if (!IsServer) return;

        currentRound.Value++;

        if (statuePrefab != null && statueSpawnPoints.Length >= currentRound.Value)
        {
            Vector3 spawnPos = statueSpawnPoints[currentRound.Value - 1].position;
            activeStatue = Instantiate(statuePrefab, spawnPos, statueSpawnPoints[currentRound.Value - 1].rotation);
            activeStatue.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            StartCoroutine(BeginEnemySpawning());
        }
    }

    public void OnStatueInteracted()
    {
        if (!IsServer) return;

        if (activeStatue != null)
        {
            activeStatue.GetComponent<NetworkObject>().Despawn();
            activeStatue = null;
        }

        StartCoroutine(BeginEnemySpawning());
    }

    IEnumerator BeginEnemySpawning()
    {
        if (currentRound.Value == bossRound)
        {
            int randomIndex = Random.Range(0, enemySpawnPoints.Length);
            StartCoroutine(SpawnEnemy(bossPrefab, enemySpawnPoints[randomIndex].transform.position, 0f));
            yield break;
        }

        int spawnAmount = baseSpawnAmount + (spawnIncrease * currentRound.Value);

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

    public void EnemyDeath(NetworkObject enemy)
    {
        if (!IsServer) return;

        if (spawnedList.Contains(enemy))
        {
            spawnedList.Remove(enemy);
        }

        if (spawnedList.Count == 0)
        {
            PrepareNextRound();
        }
    }
}