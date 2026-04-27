using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameOverManager : NetworkBehaviour
{
    public static GameOverManager Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    [Header("UI Screens")]
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private GameObject winScreen;

    [Header("First Selected Buttons")]
    [SerializeField] private Button loseFirstSelectedButton;
    [SerializeField] private Button winFirstSelectedButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "LevelScene";
    [SerializeField] private string mainMenuSceneName = "Main";

    private bool gameEnded;
    private bool isRestarting;

    private void Awake()
    {
        Instance = this;
        IsGameOver = false;
    }

    public override void OnNetworkSpawn()
    {
        IsGameOver = false;
        gameEnded = false;
        isRestarting = false;

        HideAllScreens();

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
        }
    }

    private void OnNetworkSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsServer) return;

        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            IsGameOver = false;
            gameEnded = false;
            isRestarting = false;

            SpawnFreshPlayers();

            ResetGameStateClientRpc();
        }
    }

    private void HideAllScreens()
    {
        if (loseScreen != null) loseScreen.SetActive(false);
        if (winScreen != null) winScreen.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void CheckForLose()
    {
        if (!IsServer || gameEnded || isRestarting) return;

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        if (players.Length == 0) return;

        foreach (PlayerHealth player in players)
        {
            RespawnScript respawn = player.GetComponent<RespawnScript>();

            bool playerDead =
                player.currentHealth.Value <= 0 ||
                (respawn != null && respawn.isRespawning.Value);

            if (!playerDead)
            {
                return;
            }
        }

        EndGame(false);
    }

    public void WinGame()
    {
        if (!IsServer || gameEnded || isRestarting) return;

        EndGame(true);
    }

    private void EndGame(bool won)
    {
        gameEnded = true;
        IsGameOver = true;

        ShowGameOverClientRpc(won);
    }

    [ClientRpc]
    private void ShowGameOverClientRpc(bool won)
    {
        IsGameOver = true;

        if (loseScreen != null) loseScreen.SetActive(!won);
        if (winScreen != null) winScreen.SetActive(won);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Button firstButton = won ? winFirstSelectedButton : loseFirstSelectedButton;

        if (EventSystem.current != null && firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    public void RestartButton()
    {
        if (IsServer)
        {
            RestartGame();
        }
        else
        {
            RestartGameRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RestartGameRpc()
    {
        RestartGame();
    }

    private void RestartGame()
    {
        if (!IsServer || isRestarting) return;

        isRestarting = true;
        IsGameOver = false;
        gameEnded = false;

        StopAllRespawnCoroutines();
        DespawnOldNetworkObjects();

        HideGameOverClientRpc();

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void StopAllRespawnCoroutines()
    {
        RespawnScript[] respawns = FindObjectsByType<RespawnScript>(FindObjectsSortMode.None);

        foreach (RespawnScript respawn in respawns)
        {
            respawn.StopAllCoroutines();
        }
    }

    private void DespawnOldNetworkObjects()
    {
        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj == null) continue;

            if (!netObj.IsSpawned) continue;

            if (netObj.GetComponent<NetworkManager>() != null) continue;

            if (netObj.GetComponent<GameOverManager>() != null) continue;

            netObj.Despawn(true);
        }
    }

    private void SpawnFreshPlayers()
    {
        GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            {
                continue;
            }

            GameObject playerInstance = Instantiate(playerPrefab);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

            networkObject.SpawnAsPlayerObject(clientId, true);
        }
    }

    [ClientRpc]
    private void HideGameOverClientRpc()
    {
        IsGameOver = false;
        HideAllScreens();
    }

    [ClientRpc]
    private void ResetGameStateClientRpc()
    {
        IsGameOver = false;
        HideAllScreens();
    }

    public void MainMenuButton()
    {
        if (IsServer)
        {
            GoToMainMenuClientRpc();
        }
        else
        {
            GoToMainMenuRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void GoToMainMenuRpc()
    {
        GoToMainMenuClientRpc();
    }

    [ClientRpc]
    private void GoToMainMenuClientRpc()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}