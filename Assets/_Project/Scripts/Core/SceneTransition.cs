using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Loading screen between scenes: fades to a space background, shows a title ("Loading mission Selene"),
// animated dots, a progress bar and a short tip, loads the scene in the background, waits a few
// seconds so the player can read it, and fades out in the new scene.
// Everything is built in code, so no scene or prefab has to be edited.
// GameManager.GoToScene / GoToNextScene already use it: call those, not this, from gameplay code.
public class SceneTransition : MonoBehaviour
{
    // Minimum time on screen (seconds). Starting a mission gets longer so it feels like a real load.
    public static float MissionLoadSeconds = 3f;
    public static float DefaultSeconds = 1.6f;
    public static float FadeSeconds = 0.35f;

    public static bool IsLoading { get; private set; }

    static SceneTransition instance;

    static readonly string[] Tips =
    {
        "Every kilogram you add needs more fuel to push it to the Moon.",
        "Delta-v is the budget of speed changes your spacecraft can make.",
        "On the far side of the Moon there is no radio contact with Earth.",
        "Solar panels give no power while the spacecraft is in shadow: batteries cover the gap.",
        "Bad weather on launch day makes problems during the flight more likely.",
        "A trained crew turns a small problem into a routine fix.",
        "Science only counts if the data reaches Earth: you need an antenna and time to send it.",
        "Water, oxygen and food are counted per person per day, plus a safety reserve.",
    };

    Canvas canvas;
    CanvasGroup group;
    TMP_Text title;
    TMP_Text subtitle;
    TMP_Text tip;
    RectTransform barFill;

    // Loads a scene behind the loading screen. Title and subtitle are optional (empty = default for that scene).
    public static void Load(string sceneName, string titleText = null, string subtitleText = null)
    {
        if (IsLoading) return; // a second click while loading does nothing
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("SceneTransition: scene '" + sceneName + "' is not in the build settings.");
            return;
        }

        if (instance == null)
        {
            GameObject go = new GameObject("SceneTransition");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneTransition>();
            instance.Build();
        }

        string defaultTitle, defaultSubtitle;
        DefaultTexts(sceneName, out defaultTitle, out defaultSubtitle);
        instance.baseTitle = string.IsNullOrEmpty(titleText) ? defaultTitle : titleText;
        instance.title.text = instance.baseTitle;
        instance.subtitle.text = string.IsNullOrEmpty(subtitleText) ? defaultSubtitle : subtitleText;
        instance.tip.text = "TIP: " + Tips[Random.Range(0, Tips.Length)];

        float seconds = sceneName == Scenes.Planning ? MissionLoadSeconds : DefaultSeconds;
        instance.StartCoroutine(instance.Run(sceneName, seconds));
    }

    // Text shown for each scene of the loop when the caller gives none.
    public static void DefaultTexts(string sceneName, out string titleText, out string subtitleText)
    {
        DestinationData mission = GameManager.Instance != null ? GameManager.Instance.mission : null;
        string missionName = "";
        if (mission != null) missionName = string.IsNullOrEmpty(mission.missionName) ? mission.destinationName : mission.missionName;

        switch (sceneName)
        {
            case Scenes.Planning:
                titleText = missionName == "" ? "Loading mission" : "Loading mission " + missionName;
                subtitleText = mission != null && !string.IsNullOrEmpty(mission.tagline)
                    ? mission.tagline : "Preparing your planning desk";
                break;
            case Scenes.Launch:
                titleText = "Launch sequence starting";
                subtitleText = "Crew aboard. All stations, report status.";
                break;
            case Scenes.Results:
                titleText = "Mission debrief";
                subtitleText = "Mission control is analyzing the flight data";
                break;
            case Scenes.Menu:
                titleText = "Returning to mission control";
                subtitleText = "";
                break;
            default:
                titleText = "Loading";
                subtitleText = "";
                break;
        }
    }

    IEnumerator Run(string sceneName, float minSeconds)
    {
        IsLoading = true;
        canvas.enabled = true;
        group.blocksRaycasts = true; // clicks on the old scene stop here
        SetProgress(0f);

        yield return Fade(0f, 1f);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        // Unscaled time: works even if a minigame left Time.timeScale at 0.
        float start = Time.unscaledTime;
        while (true)
        {
            float elapsed = Time.unscaledTime - start;
            float loaded = Mathf.Clamp01(load.progress / 0.9f); // Unity stops at 0.9 until activation
            SetProgress(Mathf.Min(loaded, elapsed / minSeconds));
            AnimateDots(elapsed);
            if (loaded >= 1f && elapsed >= minSeconds) break;
            yield return null;
        }

        SetProgress(1f);
        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;
        yield return null; // let the new scene run Awake/Start once before showing it

        yield return Fade(1f, 0f);
        group.blocksRaycasts = false;
        canvas.enabled = false;
        IsLoading = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / FadeSeconds);
            yield return null;
        }
        group.alpha = to;
    }

    string baseTitle = ""; // title without the animated dots

    void AnimateDots(float elapsed)
    {
        int dots = (int)(elapsed * 2.5f) % 4;
        title.text = baseTitle + new string('.', dots);
    }

    void SetProgress(float value)
    {
        barFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    // ---------- UI built in code ----------

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above every scene canvas
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // Background: drifting stars (same generator as the Results screen)
        RectTransform bg = NewRect("Background", transform);
        Stretch(bg);
        bg.gameObject.AddComponent<RawImage>();
        bg.gameObject.AddComponent<SpaceBackground>();

        title = NewText("Title", 64, FontStyles.Bold, new Color(1f, 1f, 1f));
        Place(title.rectTransform, 0.56f, 1400, 90);

        subtitle = NewText("Subtitle", 32, FontStyles.Normal, new Color(0.7f, 0.8f, 1f));
        Place(subtitle.rectTransform, 0.48f, 1400, 50);

        // Progress bar: dark track with an orange fill that grows from the left
        RectTransform track = NewRect("ProgressTrack", transform);
        Place(track, 0.38f, 720, 14);
        track.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        barFill = NewRect("Fill", track);
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.offsetMin = Vector2.zero;
        barFill.offsetMax = Vector2.zero;
        barFill.gameObject.AddComponent<Image>().color = new Color(1f, 0.55f, 0.15f);

        tip = NewText("Tip", 26, FontStyles.Italic, new Color(0.8f, 0.8f, 0.85f));
        Place(tip.rectTransform, 0.14f, 1300, 80);

        canvas.enabled = false;
    }

    TMP_Text NewText(string name, float size, FontStyles style, Color color)
    {
        RectTransform rt = NewRect(name, transform);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Centered horizontally, at a height given as a fraction of the screen.
    static void Place(RectTransform rt, float heightFraction, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, heightFraction);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
    }
}
