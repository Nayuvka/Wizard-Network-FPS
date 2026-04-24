using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HighlightBtn : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [Header("References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Normal Colours")]
    [SerializeField] private Color normalBackgroundColour = Color.white;
    [SerializeField] private Color normalTextColour = Color.black;

    [Header("Highlight Colours")]
    [SerializeField] private Color highlightBackgroundColour = Color.black;
    [SerializeField] private Color highlightTextColour = Color.white;

    [Header("UI SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;

    private bool isHovered;
    private bool isSelected;

    private void Reset()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ApplyNormalColours();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyHighlightColours();
        PlaySFX(hoverSFX);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!isSelected)
        {
            ApplyNormalColours();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySFX(clickSFX);
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyHighlightColours();
        PlaySFX(hoverSFX);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;

        if (!isHovered)
        {
            ApplyNormalColours();
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlaySFX(clickSFX);
    }

    private void ApplyNormalColours()
    {
        if (buttonImage != null)
            buttonImage.color = normalBackgroundColour;

        if (buttonText != null)
            buttonText.color = normalTextColour;
    }

    private void ApplyHighlightColours()
    {
        if (buttonImage != null)
            buttonImage.color = highlightBackgroundColour;

        if (buttonText != null)
            buttonText.color = highlightTextColour;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}