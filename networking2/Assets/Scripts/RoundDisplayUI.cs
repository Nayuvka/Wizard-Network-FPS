using UnityEngine;
using TMPro;

public class RoundDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;

    void Update()
    {
        if (SpawnManager.Instance != null)
        {
            roundText.text = "WAVE: " + SpawnManager.Instance.currentRound.Value;
        }
    }
}