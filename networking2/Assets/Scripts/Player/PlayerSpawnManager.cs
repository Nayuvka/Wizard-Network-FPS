using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    public Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
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