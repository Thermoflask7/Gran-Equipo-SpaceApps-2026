using UnityEngine;
using UnityEngine.UI;

// Basic settings: master volume and fullscreen. Saved in PlayerPrefs
// (in WebGL they are saved in the browser).
public class GameSettings : MonoBehaviour
{
    const string VolumeKey = "volume";

    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    // Applies the saved settings. MainMenu calls it at startup.
    public static void ApplySaved()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    }

    void OnEnable()
    {
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
    }
}