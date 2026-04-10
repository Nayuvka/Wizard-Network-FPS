using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public GameObject[] enemyPrefabs;
    public GameObject[] enemySpawnPoints;
    public List<NetworkObject> spawnedList = new List<NetworkObject>();
    public float enemySpawnDelay;
    public int baseSpawnAmount;
    public int spawnIncrease;
    public int currentRound = 0; 

    //Use this to decide the interval at which a new type of enemy will appear e.g. a value of 5 will introduce a new enemy on round 5, 10, 15 etc.
    public int difficultyInterval;
    //starts at 1 to avoid confusing logic
    private int currentDifficulty = 1;

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
            RoundStart();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void RoundStart()
    {
        if (!IsServer) return;

        currentRound++;
        int spawnAmount = baseSpawnAmount + (spawnIncrease * currentRound);
        if (currentRound % difficultyInterval == 0)
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

        // Raise indicator slightly above ground
        Vector3 indicatorPos = spawnPosition + Vector3.up * indicatorHeight;

        // Spawn indicator
        GameObject indicator = Instantiate(spawnIndicatorPrefab, indicatorPos, Quaternion.Euler(90, 0, 0));
        NetworkObject indicatorNet = indicator.GetComponent<NetworkObject>();
        indicatorNet.Spawn();

        // Wait before spawning enemy
        yield return new WaitForSeconds(spawnIndicatorTime);

        // Remove indicator
        indicatorNet.Despawn(true);

        // Spawn enemy
        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = spawnedEnemy.GetComponent<NetworkObject>();
        netObj.Spawn();

        spawnedList.Add(netObj);
    }

    //IMPORTANT: Make Sure the enemyScript calls this function with itself as the input variable
    public void EnemyDeath(NetworkObject enemy)
    {
        if (!IsServer) return;

        if (spawnedList.Contains(enemy))
        {
            spawnedList.Remove(enemy);
        }

        if (spawnedList.Count == 0)
        {
            RoundStart();
        }
    }
}