using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HoverButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform target;
    [SerializeField] private Image leftBorder;
    [SerializeField] private Selectable selectable;

    [Header("Movement")]
    [SerializeField] private float hoverOffset = 10f;
    [SerializeField] private float lerpSpeed = 12f;

    [Header("Border")]
    [SerializeField] private Color borderColour = Color.white;

    [Header("UI SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;

    private float startPosX;
    private float targetPosX;

    private Color hiddenColour;
    private bool isSelected;

    private void Reset()
    {
        target = transform as RectTransform;
        selectable = GetComponent<Selectable>();
        //audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        target ??= transform as RectTransform;
        selectable ??= GetComponent<Selectable>();
        //audioSource ??= GetComponent<AudioSource>();

        hiddenColour = borderColour;
        hiddenColour.a = 0f;

        if (leftBorder)
            leftBorder.color = hiddenColour;
    }

    private void Start()
    {
        startPosX = target.anchoredPosition.x;
        targetPosX = startPosX;
    }

    private void Update()
    {
        Vector2 currentPos = target.anchoredPosition;

        currentPos.x = Mathf.Lerp(
            currentPos.x,
            targetPosX,
            Time.unscaledDeltaTime * lerpSpeed
        );

        target.anchoredPosition = currentPos;

        if (leftBorder)
        {
            leftBorder.color = Color.Lerp(
                leftBorder.color,
                isSelected ? borderColour : hiddenColour,
                Time.unscaledDeltaTime * lerpSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!selectable || !selectable.interactable || !EventSystem.current)
            return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySFX(clickSFX);
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        targetPosX = startPosX + hoverOffset;

        PlaySFX(hoverSFX);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        targetPosX = startPosX;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlaySFX(clickSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }
}