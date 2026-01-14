using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource backgroundMusic; // Drag your Music GameObject here
    public AudioSource sfxSource;       // Drag your SFX GameObject here

    [Header("UI Controls")]
    public Slider musicVolumeSlider;    // Large slider for volume
    public Slider sfxVolumeSlider;      // Large slider for volume
    public Toggle musicMuteSwitch;      // Your "small slider" toggle

    private void Start()
    {
        // 1. Load Volume (Default to 1.0)
        float musicVol = PlayerPrefs.GetFloat("MusicVol", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);

        // 2. Load Mute State (Default to 1 = Sound On)
        // We use 1 for True (Sound On) and 0 for False (Muted)
        bool isSoundOn = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;

        // 3. Apply settings to Audio Sources
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = musicVol;
            backgroundMusic.mute = !isSoundOn; // If Sound is On, Mute is False
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVol;
        }

        // 4. Update UI visuals
        if (musicVolumeSlider) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider) sfxVolumeSlider.value = sfxVol;
        if (musicMuteSwitch) musicMuteSwitch.isOn = isSoundOn;

        // 5. Connect the UI to the code
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (musicMuteSwitch) musicMuteSwitch.onValueChanged.AddListener(SetMusicMute);
    }

    // --- Volume Logic ---

    public void SetMusicVolume(float value)
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = value;
        }
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = value;
        }
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    // --- Mute Switch Logic ---

    public void SetMusicMute(bool isSoundOn)
    {
        // If the switch is ON, we are NOT muted. 
        // If the switch is OFF, we ARE muted.
        if (backgroundMusic != null)
        {
            backgroundMusic.mute = !isSoundOn;
        }

        // Save 1 for On, 0 for Off
        PlayerPrefs.SetInt("MusicEnabled", isSoundOn ? 1 : 0);
    }
}