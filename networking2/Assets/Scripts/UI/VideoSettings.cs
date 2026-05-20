using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VideoSettingsManager : MonoBehaviour
{
    [Header("Display Mode UI")]
    [SerializeField] private TMP_Text displayModeText;
    [SerializeField] private Button displayModeLeftButton;
    [SerializeField] private Button displayModeRightButton;

    [Header("Resolution UI")]
    [SerializeField] private TMP_Text resolutionText;
    [SerializeField] private Button resolutionLeftButton;
    [SerializeField] private Button resolutionRightButton;

    [Header("V-Sync UI")]
    [SerializeField] private TMP_Text vSyncText;
    [SerializeField] private Button vSyncLeftButton;
    [SerializeField] private Button vSyncRightButton;

    [Header("Framerate UI")]
    [SerializeField] private TMP_Text framerateText;
    [SerializeField] private Button framerateLeftButton;
    [SerializeField] private Button framerateRightButton;

    [Header("Bottom Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button defaultButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 1.5f;

    private Resolution[] resolutions;

    private int currentDisplayModeIndex;
    private int currentResolutionIndex;
    private int currentVSyncIndex;
    private int currentFramerateIndex;

    private Coroutine feedbackCoroutine;

    private readonly List<string> displayModeOptions = new List<string>
    {
        "Windowed",
        "Fullscreen"
    };

    private readonly List<string> vSyncOptions = new List<string>
    {
        "Off",
        "On"
    };

    private readonly List<string> framerateOptions = new List<string>
    {
        "30 FPS",
        "60 FPS",
        "120 FPS",
        "Unlimited"
    };

    private readonly List<int> framerateValues = new List<int>
    {
        30,
        60,
        120,
        -1
    };

    private void Start()
    {
        SetupResolutionOptions();
        LoadSavedSettings();
        SetupButtons();
        UpdateAllUI();

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    private void SetupButtons()
    {
        displayModeLeftButton.onClick.AddListener(PreviousDisplayMode);
        displayModeRightButton.onClick.AddListener(NextDisplayMode);

        resolutionLeftButton.onClick.AddListener(PreviousResolution);
        resolutionRightButton.onClick.AddListener(NextResolution);

        vSyncLeftButton.onClick.AddListener(PreviousVSync);
        vSyncRightButton.onClick.AddListener(NextVSync);

        framerateLeftButton.onClick.AddListener(PreviousFramerate);
        framerateRightButton.onClick.AddListener(NextFramerate);

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }

        if (defaultButton != null)
        {
            defaultButton.onClick.AddListener(ResetToDefaults);
        }
    }

    private void SetupResolutionOptions()
    {
        resolutions = Screen.resolutions;
        currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }
    }

    private void LoadSavedSettings()
    {
        currentDisplayModeIndex = PlayerPrefs.GetInt("DisplayMode", Screen.fullScreen ? 1 : 0);
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        currentVSyncIndex = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount > 0 ? 1 : 0);

        int savedFramerate = PlayerPrefs.GetInt("Framerate", -1);

        if (savedFramerate == 30)
        {
            currentFramerateIndex = 0;
        }
        else if (savedFramerate == 60)
        {
            currentFramerateIndex = 1;
        }
        else if (savedFramerate == 120)
        {
            currentFramerateIndex = 2;
        }
        else
        {
            currentFramerateIndex = 3;
        }

        currentResolutionIndex = Mathf.Clamp(currentResolutionIndex, 0, resolutions.Length - 1);
        currentDisplayModeIndex = Mathf.Clamp(currentDisplayModeIndex, 0, displayModeOptions.Count - 1);
        currentVSyncIndex = Mathf.Clamp(currentVSyncIndex, 0, vSyncOptions.Count - 1);
        currentFramerateIndex = Mathf.Clamp(currentFramerateIndex, 0, framerateOptions.Count - 1);

        ApplySettingsWithoutFeedback();
    }

    private void PreviousDisplayMode()
    {
        currentDisplayModeIndex--;

        if (currentDisplayModeIndex < 0)
        {
            currentDisplayModeIndex = displayModeOptions.Count - 1;
        }

        UpdateDisplayModeUI();
    }

    private void NextDisplayMode()
    {
        currentDisplayModeIndex++;

        if (currentDisplayModeIndex >= displayModeOptions.Count)
        {
            currentDisplayModeIndex = 0;
        }

        UpdateDisplayModeUI();
    }

    private void PreviousResolution()
    {
        currentResolutionIndex--;

        if (currentResolutionIndex < 0)
        {
            currentResolutionIndex = resolutions.Length - 1;
        }

        UpdateResolutionUI();
    }

    private void NextResolution()
    {
        currentResolutionIndex++;

        if (currentResolutionIndex >= resolutions.Length)
        {
            currentResolutionIndex = 0;
        }

        UpdateResolutionUI();
    }

    private void PreviousVSync()
    {
        currentVSyncIndex--;

        if (currentVSyncIndex < 0)
        {
            currentVSyncIndex = vSyncOptions.Count - 1;
        }

        UpdateVSyncUI();
    }

    private void NextVSync()
    {
        currentVSyncIndex++;

        if (currentVSyncIndex >= vSyncOptions.Count)
        {
            currentVSyncIndex = 0;
        }

        UpdateVSyncUI();
    }

    private void PreviousFramerate()
    {
        currentFramerateIndex--;

        if (currentFramerateIndex < 0)
        {
            currentFramerateIndex = framerateOptions.Count - 1;
        }

        UpdateFramerateUI();
    }

    private void NextFramerate()
    {
        currentFramerateIndex++;

        if (currentFramerateIndex >= framerateOptions.Count)
        {
            currentFramerateIndex = 0;
        }

        UpdateFramerateUI();
    }

    public void ApplySettings()
    {
        ApplyDisplayMode();
        ApplyResolution();
        ApplyVSync();
        ApplyFramerate();

        PlayerPrefs.Save();

        ShowFeedback("Settings Applied");
    }

    private void ApplySettingsWithoutFeedback()
    {
        ApplyDisplayMode();
        ApplyResolution();
        ApplyVSync();
        ApplyFramerate();

        PlayerPrefs.Save();
    }

    private void ApplyDisplayMode()
    {
        bool isFullscreen = currentDisplayModeIndex == 1;

        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt("DisplayMode", currentDisplayModeIndex);
    }

    private void ApplyResolution()
    {
        Resolution resolution = resolutions[currentResolutionIndex];
        bool isFullscreen = currentDisplayModeIndex == 1;

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            isFullscreen
        );

        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);
    }

    private void ApplyVSync()
    {
        bool enabled = currentVSyncIndex == 1;

        QualitySettings.vSyncCount = enabled ? 1 : 0;

        PlayerPrefs.SetInt("VSync", currentVSyncIndex);
    }

    private void ApplyFramerate()
    {
        int framerate = framerateValues[currentFramerateIndex];

        Application.targetFrameRate = framerate;

        PlayerPrefs.SetInt("Framerate", framerate);
    }

    public void ResetToDefaults()
    {
        currentDisplayModeIndex = 1; // Fullscreen
        currentVSyncIndex = 0;       // Off
        currentFramerateIndex = 3;   // Unlimited

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }

        UpdateAllUI();

        ApplyDisplayMode();
        ApplyResolution();
        ApplyVSync();
        ApplyFramerate();

        PlayerPrefs.Save();

        ShowFeedback("Default Settings Restored");
    }

    private void UpdateAllUI()
    {
        UpdateDisplayModeUI();
        UpdateResolutionUI();
        UpdateVSyncUI();
        UpdateFramerateUI();
    }

    private void UpdateDisplayModeUI()
    {
        displayModeText.text = displayModeOptions[currentDisplayModeIndex];
    }

    private void UpdateResolutionUI()
    {
        Resolution resolution = resolutions[currentResolutionIndex];
        resolutionText.text = resolution.width + "x" + resolution.height;
    }

    private void UpdateVSyncUI()
    {
        vSyncText.text = vSyncOptions[currentVSyncIndex];
    }

    private void UpdateFramerateUI()
    {
        framerateText.text = framerateOptions[currentFramerateIndex];
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(FeedbackRoutine(message));
    }

    private IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(feedbackDuration);

        feedbackText.gameObject.SetActive(false);
    }
}