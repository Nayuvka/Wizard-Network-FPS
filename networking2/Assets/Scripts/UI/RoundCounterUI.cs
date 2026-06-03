using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private int maxRounds = 6;

    private void OnEnable()
    {
        SpawnManager.OnRoundStarted += UpdateRound;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SpawnManager.OnRoundStarted -= UpdateRound;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool hasSpawnManager =
            SpawnManager.Instance != null;

        roundText.enabled = hasSpawnManager;

        if (hasSpawnManager)
        {
            UpdateRound(
                SpawnManager.Instance.currentRound.Value);
        }
    }

    private void UpdateRound(int round)
    {
        roundText.text =
            $"Wave {round}/{maxRounds}";
    }
}