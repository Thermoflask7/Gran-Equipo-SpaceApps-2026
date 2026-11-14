using UnityEngine;
using UnityEngine.UI;

// Ajustes basicos: volumen general y pantalla completa. Se guardan en PlayerPrefs
// (en WebGL se guardan en el navegador).
public class GameSettings : MonoBehaviour
{
    const string VolumeKey = "volumen";

    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    // Aplica lo guardado. MainMenu lo llama al iniciar.
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