using System.Collections;
using UnityEngine;

public class MenuCharacterIdle : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float regularIdleTime = 10f;
    [SerializeField] private float happyIdleTime = 10f;
    [SerializeField] private float offensiveIdleTime = 10f;
    [SerializeField] private float lookAroundIdleTime = 10f;

    private readonly int nextIdleHash = Animator.StringToHash("NextIdle");

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        StartCoroutine(IdleLoopRoutine());
    }

    private IEnumerator IdleLoopRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regularIdleTime);
            animator.SetTrigger(nextIdleHash);

            yield return new WaitForSeconds(happyIdleTime);
            animator.SetTrigger(nextIdleHash);

            yield return new WaitForSeconds(offensiveIdleTime);
            animator.SetTrigger(nextIdleHash);

            yield return new WaitForSeconds(lookAroundIdleTime);
            animator.SetTrigger(nextIdleHash);
        }
    }
}