using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HighlightBtn : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [Header("References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Selectable selectable;

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

    [Header("Input Icon")]
    public bool hasInputIcon;
    public Image inputIcon;

    private bool isSelected;

    private void Reset()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        selectable = GetComponent<Selectable>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (selectable == null)
            selectable = GetComponent<Selectable>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        }
    }

    private void OnDisable()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
    }

    private void Start()
    {
        ApplyNormalColours();

        if (hasInputIcon && inputIcon != null)
        {
            inputIcon.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;

        if (EventSystem.current == null)
            return;

        if (EventSystem.current.currentSelectedGameObject != gameObject)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
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

        if (hasInputIcon && inputIcon != null)
        {
            if (
                InputDeviceDetector.Instance != null
                && InputDeviceDetector.Instance.CurrentDevice
                    != InputDeviceType.KeyboardMouse
            )
            {
                inputIcon.gameObject.SetActive(true);
            }
            else
            {
                inputIcon.gameObject.SetActive(false);
            }
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;

        ApplyNormalColours();

        if (hasInputIcon && inputIcon != null)
        {
            inputIcon.gameObject.SetActive(false);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlaySFX(clickSFX);
    }

    private void HandleDeviceChanged(InputDeviceType deviceType)
    {
        if (!hasInputIcon || inputIcon == null)
            return;

        if (deviceType == InputDeviceType.KeyboardMouse)
        {
            inputIcon.gameObject.SetActive(false);
        }
        else
        {
            inputIcon.gameObject.SetActive(isSelected);
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

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}