using TMPro;
using UnityEngine;

public class RoundCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private int maxRounds = 6;

    private void OnEnable()
    {
        SpawnManager.OnRoundStarted +=
            UpdateRound;
    }

    private void OnDisable()
    {
        SpawnManager.OnRoundStarted -=
            UpdateRound;
    }

    private void Start()
    {
        if (SpawnManager.Instance != null)
        {
            UpdateRound(
                SpawnManager.Instance
                .currentRound.Value);
        }
    }

    private void UpdateRound(
        int round)
    {
        roundText.text =
            $"Wave {round}/{maxRounds}";
    }
}