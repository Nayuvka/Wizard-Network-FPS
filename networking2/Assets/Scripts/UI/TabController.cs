using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View
{
    public class TabController : MonoBehaviour
    {
        [Header("Tabs & Pages")]
        [SerializeField] private Image[] tabButtons;
        [SerializeField] private GameObject[] pages;

        [Header("Optional Tab Text")]
        [SerializeField] private TextMeshProUGUI[] tabTexts;

        [Header("Optional Tab Icons")]
        [SerializeField] private Image[] tabIcons;

        [Header("Colours")]
        [SerializeField] private Color selectedTabColour;
        [SerializeField] private Color deselectedTabColour;
        [SerializeField] private Color hoverTabColour;

        [SerializeField] private Color selectedTextIcon;
        [SerializeField] private Color deselectedTextIcon;
        [SerializeField] private Color hoverTextIcon;

        [Header("Settings")]
        [SerializeField] private bool buttonFill;
        [SerializeField] private bool startWithActiveTab = true;

        private int currentTab = -1;

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

            currentTab = tabNo;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool isSelected = i == tabNo;

                if (i < pages.Length && pages[i] != null)
                    pages[i].SetActive(isSelected);

                if (tabButtons[i] != null)
                {
                    tabButtons[i].color = isSelected ? selectedTabColour : deselectedTabColour;

                    if (buttonFill)
                        tabButtons[i].fillCenter = isSelected;
                }

                if (tabTexts != null && i < tabTexts.Length && tabTexts[i] != null)
                    tabTexts[i].color = isSelected ? selectedTextIcon : deselectedTextIcon;

                if (tabIcons != null && i < tabIcons.Length && tabIcons[i] != null)
                    tabIcons[i].color = isSelected ? selectedTextIcon : deselectedTextIcon;
            }
        }

        public void OnTabHover(int tabNo)
        {
            if (!IsValidTab(tabNo) || tabNo == currentTab) return;

            if (tabButtons[tabNo] != null)
                tabButtons[tabNo].color = hoverTabColour;

            if (tabTexts != null && tabNo < tabTexts.Length && tabTexts[tabNo] != null)
                tabTexts[tabNo].color = hoverTextIcon;

            if (tabIcons != null && tabNo < tabIcons.Length && tabIcons[tabNo] != null)
                tabIcons[tabNo].color = hoverTextIcon;
        }

        public void OnTabExit(int tabNo)
        {
            if (!IsValidTab(tabNo) || tabNo == currentTab) return;

            if (tabButtons[tabNo] != null)
                tabButtons[tabNo].color = deselectedTabColour;

            if (tabTexts != null && tabNo < tabTexts.Length && tabTexts[tabNo] != null)
                tabTexts[tabNo].color = deselectedTextIcon;

            if (tabIcons != null && tabNo < tabIcons.Length && tabIcons[tabNo] != null)
                tabIcons[tabNo].color = deselectedTextIcon;
        }

        private bool IsValidTab(int tabNo)
        {
            return tabNo >= 0 && tabNo < tabButtons.Length;
        }
    }
}