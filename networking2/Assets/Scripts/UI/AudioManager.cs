using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer masterMixer;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider uiVolumeSlider;

    private const string MasterVolume = "MasterVolume";
    private const string MusicVolume = "MusicVolume";
    private const string SFXVolume = "SFXVolume";
    private const string UIVolume = "UIVolume";

    private void Start()
    {
        SetupSlider(masterVolumeSlider, MasterVolume, SetMasterVolume);
        SetupSlider(musicVolumeSlider, MusicVolume, SetMusicVolume);
        SetupSlider(sfxVolumeSlider, SFXVolume, SetSfxVolume);
        SetupSlider(uiVolumeSlider, UIVolume, SetUIVolume);
    }

    private void SetupSlider(Slider slider, string exposedParameter, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null || masterMixer == null) return;

        float savedValue = PlayerPrefs.GetFloat(exposedParameter, 1f);

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(savedValue);

        SetMixerVolume(exposedParameter, savedValue);

        slider.onValueChanged.AddListener(callback);
    }

    public void SetMasterVolume(float sliderValue)
    {
        SetMixerVolume(MasterVolume, sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        SetMixerVolume(MusicVolume, sliderValue);
    }

    public void SetSfxVolume(float sliderValue)
    {
        SetMixerVolume(SFXVolume, sliderValue);
    }

    public void SetUIVolume(float sliderValue)
    {
        SetMixerVolume(UIVolume, sliderValue);
    }

    private void SetMixerVolume(string exposedParameter, float sliderValue)
    {
        if (masterMixer == null) return;

        float volumeDB = Mathf.Log10(sliderValue) * 20f;

        masterMixer.SetFloat(exposedParameter, volumeDB);
        PlayerPrefs.SetFloat(exposedParameter, sliderValue);
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (uiVolumeSlider != null) uiVolumeSlider.onValueChanged.RemoveListener(SetUIVolume);
    }
}



