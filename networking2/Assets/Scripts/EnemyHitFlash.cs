using UnityEngine;
using System.Collections;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private Material material;
    private Color originalColor;

    void Awake()
    {
        if (meshRenderer != null)
        {
            material = meshRenderer.material;
            originalColor = material.color;
        }
    }

    public void PlayFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        material.color = originalColor;
    }
}