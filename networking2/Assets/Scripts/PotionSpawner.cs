using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PotionSpawner : NetworkBehaviour
{
    [Header("Potion Stand Prefabs")]
    public GameObject[] potionPrefabs;

    [Header("Spawn Locations")]
    public Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnPotionStands();
        }
    }

    public void SpawnPotionStands()
    {
        if (potionPrefabs.Length == 0)
        {
            return;
        }

        if (spawnPoints.Length < potionPrefabs.Length)
        {
            return;
        }

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < potionPrefabs.Length; i++)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            GameObject spawnedPotion = Instantiate(potionPrefabs[i], selectedPoint.position, selectedPoint.rotation);
            
            spawnedPotion.GetComponent<NetworkObject>().Spawn();

            availablePoints.RemoveAt(randomIndex);
        }
    }
}