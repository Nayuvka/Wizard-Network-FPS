using TMPro;
using UnityEngine;

public class RoundDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private Animator animator;

    public void ShowRound(int round)
    {
        if (roundText != null)
        {
            roundText.text = $"Wave {round}";
        }

        if (animator != null)
        {
            animator.ResetTrigger("waveStart");
            animator.SetTrigger("waveStart");
        }
    }
}