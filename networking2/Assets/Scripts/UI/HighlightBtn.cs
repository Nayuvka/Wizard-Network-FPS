using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HighlightBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
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

    private bool isHovered;
    private bool isSelected;

    private void Reset()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        ApplyNormalColours();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyHighlightColours();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!isSelected)
        {
            ApplyNormalColours();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyHighlightColours();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;

        if (!isHovered)
        {
            ApplyNormalColours();
        }
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
}