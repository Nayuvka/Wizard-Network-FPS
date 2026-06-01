using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Header("Initial Player Spawns")]
    public Transform[] startingSpawnPoints;

    [Header("Respawn Points")]
    public Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetStartingSpawnPoint(ulong clientId)
    {
        if (startingSpawnPoints == null || startingSpawnPoints.Length == 0)
            return null;

        int index = (int)(clientId % (ulong)startingSpawnPoints.Length);

        return startingSpawnPoints[index];
    }

    public Transform GetBestSpawnPoint(Vector3 deathPosition)
    {
        Transform best = null;
        float maxDistance = -1f;

        foreach (var spawn in spawnPoints)
        {
            float dist = Vector3.Distance(deathPosition, spawn.position);

            if (dist > maxDistance)
            {
                maxDistance = dist;
                best = spawn;
            }
        }

        return best;
    }
}