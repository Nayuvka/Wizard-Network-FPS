using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Color> originalColors = new List<Color>();

    private void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                materials.Add(mat);
                originalColors.Add(mat.color);
            }
        }
    }

    public void PlayFlash()
    {
        if (materials.Count == 0) return;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].color = originalColors[i];
        }
    }
}