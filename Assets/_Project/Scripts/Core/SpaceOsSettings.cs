using System;
using UnityEngine;

/// <summary>
/// Persistent game settings (PlayerPrefs, valid on WebGL).
/// Loaded and applied on startup; other systems read the properties or subscribe to Changed.
/// Renamed from GameSettings so it does not collide with the menu GameSettings MonoBehaviour.
/// PlayerPrefs keys keep the GameSettings.* prefix so existing saves still work.
/// </summary>
public static class SpaceOsSettings
{
    public const float DefaultMasterVolume = 1f;
    public const float DefaultMusicVolume = 0.8f;
    public const float DefaultSfxVolume = 1f;
    public const bool DefaultCrtFilter = true;
    public const bool DefaultVSync = true;
    public const int DefaultTargetFrameRate = 60;
    public const float DefaultCameraSensitivity = 1f;

    const string KeyMaster = "GameSettings.MasterVolume";
    const string KeyMusic = "GameSettings.MusicVolume";
    const string KeySfx = "GameSettings.SfxVolume";
    const string KeyQuality = "GameSettings.QualityLevel";
    const string KeyFullscreen = "GameSettings.Fullscreen";
    const string KeyCrt = "GameSettings.CrtFilter";
    const string KeyVSync = "GameSettings.VSync";
    const string KeyFrameRate = "GameSettings.TargetFrameRate";
    const string KeySensitivity = "GameSettings.CameraSensitivity";

    static bool loaded;

    /// <summary>Fired after Load, Apply, Save or a value change.</summary>
    public static event Action Changed;

    public static float MasterVolume { get; set; } = DefaultMasterVolume;
    public static float MusicVolume { get; set; } = DefaultMusicVolume;
    public static float SfxVolume { get; set; } = DefaultSfxVolume;
    public static int QualityLevel { get; set; }
    public static bool Fullscreen { get; set; }
    public static bool CrtFilter { get; set; } = DefaultCrtFilter;
    public static bool VSync { get; set; } = DefaultVSync;
    public static int TargetFrameRate { get; set; } = DefaultTargetFrameRate;
    public static float CameraSensitivity { get; set; } = DefaultCameraSensitivity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Load();
        Apply();
    }

    public static void Load()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMaster, DefaultMasterVolume));
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, DefaultMusicVolume));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, DefaultSfxVolume));
        QualityLevel = PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel());
        Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) != 0;
        CrtFilter = PlayerPrefs.GetInt(KeyCrt, DefaultCrtFilter ? 1 : 0) != 0;
        VSync = PlayerPrefs.GetInt(KeyVSync, DefaultVSync ? 1 : 0) != 0;
        TargetFrameRate = PlayerPrefs.GetInt(KeyFrameRate, DefaultTargetFrameRate);
        CameraSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, DefaultCameraSensitivity), 0.2f, 2f);
        ClampQuality();
        loaded = true;
        Changed?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
        PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
        PlayerPrefs.SetFloat(KeySfx, SfxVolume);
        PlayerPrefs.SetInt(KeyQuality, QualityLevel);
        PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyCrt, CrtFilter ? 1 : 0);
        PlayerPrefs.SetInt(KeyVSync, VSync ? 1 : 0);
        PlayerPrefs.SetInt(KeyFrameRate, TargetFrameRate);
        PlayerPrefs.SetFloat(KeySensitivity, CameraSensitivity);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    /// <summary>Applies the current values to audio, quality, screen, CRT and sensitivity.</summary>
    public static void Apply()
    {
        if (!loaded) Load();
        AudioListener.volume = MasterVolume;
        ClampQuality();
        if (QualitySettings.GetQualityLevel() != QualityLevel)
            QualitySettings.SetQualityLevel(QualityLevel, true);
        if (Screen.fullScreen != Fullscreen)
            Screen.fullScreen = Fullscreen;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : TargetFrameRate;

        var screens = UnityEngine.Object.FindObjectsByType<SpaceOsScreen>(FindObjectsInactive.Include);
        for (int i = 0; i < screens.Length; i++)
            screens[i].SetCrtEnabled(CrtFilter);

        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        MasterVolume = DefaultMasterVolume;
        MusicVolume = DefaultMusicVolume;
        SfxVolume = DefaultSfxVolume;
        QualityLevel = Mathf.Max(0, QualitySettings.names.Length - 1);
        Fullscreen = false;
        CrtFilter = DefaultCrtFilter;
        VSync = DefaultVSync;
        TargetFrameRate = DefaultTargetFrameRate;
        CameraSensitivity = DefaultCameraSensitivity;
        Apply();
        Save();
    }

    static void ClampQuality()
    {
        int count = QualitySettings.names != null ? QualitySettings.names.Length : 0;
        if (count <= 0) { QualityLevel = 0; return; }
        QualityLevel = Mathf.Clamp(QualityLevel, 0, count - 1);
    }
}