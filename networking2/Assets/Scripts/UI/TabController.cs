using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Header("Tabs & Pages")]
    [SerializeField] private Image[] tabButtons;
    [SerializeField] private GameObject[] pages;

    [Header("Tab Button Colours")]
    [SerializeField] private Color selectedTabColour;
    [SerializeField] private Color deselectedTabColour;
    [SerializeField] private Color hoverTabColour;

    [Header("Tab Text Colours")]
    [SerializeField] private Color selectedTextColour;
    [SerializeField] private Color deselectedTextColour;
    [SerializeField] private Color hoverTextColour;

    [Header("Settings")]
    [SerializeField] private bool buttonFill;
    [SerializeField] private bool startWithActiveTab = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;

    private TextMeshProUGUI[] tabTexts;
    private int currentTab = -1;

    private void Awake()
    {
        tabTexts = new TextMeshProUGUI[tabButtons.Length];

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                tabTexts[i] = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }

    private void Start()
    {
        if (startWithActiveTab)
        {
            ActivateTab(0);
        }
    }

    public void ActivateTab(int tabNo)
    {
        if (!IsValidTab(tabNo)) return;

        // Prevent click sound when clicking the already active tab
        if (tabNo != currentTab)
        {
            PlaySFX(clickSFX);
        }

        currentTab = tabNo;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool isSelected = i == tabNo;

            if (i < pages.Length && pages[i] != null)
            {
                pages[i].SetActive(isSelected);
            }

            if (tabButtons[i] != null)
            {
                tabButtons[i].color = isSelected
                    ? selectedTabColour
                    : deselectedTabColour;

                if (buttonFill)
                {
                    tabButtons[i].fillCenter = isSelected;
                }
            }

            if (tabTexts[i] != null)
            {
                tabTexts[i].color = isSelected
                    ? selectedTextColour
                    : deselectedTextColour;
            }
        }
    }

    public void OnTabHover(int tabNo)
    {
        if (!IsValidTab(tabNo) || tabNo == currentTab) return;

        PlaySFX(hoverSFX);

        if (tabButtons[tabNo] != null)
        {
            tabButtons[tabNo].color = hoverTabColour;
        }

        if (tabTexts[tabNo] != null)
        {
            tabTexts[tabNo].color = hoverTextColour;
        }
    }

    public void OnTabExit(int tabNo)
    {
        if (!IsValidTab(tabNo) || tabNo == currentTab) return;

        if (tabButtons[tabNo] != null)
        {
            tabButtons[tabNo].color = deselectedTabColour;
        }

        if (tabTexts[tabNo] != null)
        {
            tabTexts[tabNo].color = deselectedTextColour;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private bool IsValidTab(int tabNo)
    {
        return tabButtons != null &&
               tabNo >= 0 &&
               tabNo < tabButtons.Length;
    }
}