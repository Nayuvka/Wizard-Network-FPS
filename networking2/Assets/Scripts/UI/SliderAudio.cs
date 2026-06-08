using UnityEngine;
using UnityEngine.EventSystems;

public class SliderAudio : MonoBehaviour,
    IPointerEnterHandler,
    IBeginDragHandler,
    IEndDragHandler
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip releaseSFX;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySFX(hoverSFX);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PlaySFX(releaseSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}