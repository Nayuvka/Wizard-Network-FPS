using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatFeedbackUI : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private Image crosshair;

    [Header("Hitmarker")]
    [SerializeField] private Image hitMarker;

    [Header("Colours")]
    [SerializeField] private Color normalColour = Color.white;
    [SerializeField] private Color enemyColour = Color.red;

    [Header("Timing")]
    [SerializeField] private float markerDuration = 0.12f;

    private Coroutine markerRoutine;

    public void SetTargetingEnemy(bool targetingEnemy)
    {
        if (crosshair == null) return;

        crosshair.color = targetingEnemy
            ? enemyColour
            : normalColour;
    }

    public void ShowHitmarker()
    {
        if (markerRoutine != null)
        {
            StopCoroutine(markerRoutine);
        }

        markerRoutine = StartCoroutine(HitmarkerRoutine(false));
    }

    public void ShowKillmarker()
    {
        if (markerRoutine != null)
        {
            StopCoroutine(markerRoutine);
        }

        markerRoutine = StartCoroutine(HitmarkerRoutine(true));
    }

    private IEnumerator HitmarkerRoutine(bool kill)
    {
        if (hitMarker == null) yield break;

        hitMarker.gameObject.SetActive(true);

        hitMarker.color = kill
            ? Color.red
            : Color.white;

        yield return new WaitForSeconds(markerDuration);

        hitMarker.gameObject.SetActive(false);
    }
}