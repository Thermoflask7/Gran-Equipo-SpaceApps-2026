using System.Collections.Generic;
using UnityEngine;

// Sound effects (task 27). Signed: Grok.
// The clips live in Assets/_Project/Resources/Audio and load by file name the first time they play.
// Usage: Sfx.Play(Sfx.Purchase);
// One persistent 2D AudioSource plays them, so the master volume (AudioListener.volume, GameSettings) applies.
// Each clip has a short cooldown so sliders and fast clicks do not stack the same sound.
public static class Sfx
{
    // Desk and PC
    public const string SelectStation = "selectmainelement";
    public const string PcClick = "clickpc";
    public const string PcShutDown = "apagarcomputadora";
    public const string FolderPage = "switchpagefolder";
    public const string TutorialOpen = "tutorialmenuopen";
    public const string Purchase = "purchase";
    public const string CantClick = "clickonsomethingyoucant";

    // Flight
    public const string Launch = "audio_launch_cohete";
    public const string MinigamePopup = "minigamepopupp";

    // Research minigame: Photo Safari clicks (ice / radiation and other targets) and the Signal Catcher beam
    public const string ZooClickIce = "zoominigameclickice";
    public const string RadiationClick = "clickradiationminigame";
    public const string ZooLaserRay = "zoolaserminigameray";

    // Piloting minigame (timing bar during the launch)
    public const string BarClickGood = "barclickminigamegood";
    public const string BarClickBad = "errorbarrminigamebadsound";

    // Maintenance minigame
    public const string MaintenanceClick = "maintenanceminigameclick";
    public const string MaintenanceSwitch = "mantenimientominijuegoswitch";
    public const string MaintenanceScroll = "maintenanceminigamescroll";

    // Spare switch sound, not used yet (for any new switch that needs a different sound)
    public const string AlternativeSwitch = "alternativeswitch";

    const string Folder = "Audio/";
    const float DefaultCooldown = 0.05f;

    static AudioSource source;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

    /// <summary>Plays a clip from Resources/Audio once. cooldown = minimum seconds between two plays of the same clip.</summary>
    public static void Play(string clipName, float volume = 1f, float cooldown = DefaultCooldown)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        float now = Time.unscaledTime;
        float last;
        if (lastPlayed.TryGetValue(clipName, out last) && now - last < cooldown && now >= last) return;

        AudioClip clip = Load(clipName);
        if (clip == null) return;
        lastPlayed[clipName] = now;
        Source().PlayOneShot(clip, volume);
    }

    /// <summary>Stops every sound that is playing (for example a long clip when the scene changes).</summary>
    public static void StopAll()
    {
        if (source != null) source.Stop();
    }

    static AudioClip Load(string clipName)
    {
        AudioClip clip;
        if (clips.TryGetValue(clipName, out clip) && clip != null) return clip;
        clip = Resources.Load<AudioClip>(Folder + clipName);
        if (clip == null) Debug.LogWarning("Sfx: missing clip Resources/" + Folder + clipName);
        clips[clipName] = clip;
        return clip;
    }

    static AudioSource Source()
    {
        if (source != null) return source;
        GameObject go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }
}