using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Unity menus for the mission folder (task 4):
//   SpaceApps > Mission folder > Create prefab
//       Creates Assets/_Project/Prefabs/MissionFolder.prefab: a manila folder with page 1
//       (briefing, launch checklist and mission updates), page 2 (crew: astronaut profiles
//       and training, task 5) and buttons to change page.
//   SpaceApps > Mission folder > Add to open scene
//       Puts the prefab inside the Canvas of the open scene (creates a Canvas and an EventSystem if missing).
//       It is added ACTIVE so it can be tested; on the desk the station must leave it inactive
//       and call Open() when the folder is clicked (opening it marks "Mission read").
//   SpaceApps > Mission folder > Test: open folder / Test: roll planning event / Test: reset game  (Play mode only)
public static class MissionFolderBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/MissionFolder.prefab";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color FolderColor = new Color(0.82f, 0.66f, 0.4f, 1f);
    static readonly Color TabColor = new Color(0.74f, 0.58f, 0.34f, 1f);
    static readonly Color PaperColor = new Color(0.98f, 0.96f, 0.9f, 1f);
    static readonly Color InkColor = new Color(0.12f, 0.12f, 0.16f, 1f);
    static readonly Color HeadingColor = new Color(0.1f, 0.25f, 0.5f, 1f);
    static readonly Color CardColor = new Color(1f, 0.99f, 0.96f, 1f);
    static readonly Color AccentColor = new Color(0.95f, 0.55f, 0.1f, 1f);
    static readonly Color StampRed = new Color(0.75f, 0.15f, 0.12f, 1f);
    static readonly Color StampGreen = new Color(0.18f, 0.5f, 0.25f, 1f);
    static readonly Color NoteColor = new Color(1f, 0.94f, 0.6f, 1f);
    static readonly Color TraitPlaceholder = new Color(0.16f, 0.25f, 0.5f, 1f);

    [MenuItem("SpaceApps/Mission folder/Create prefab")]
    static void CreatePrefab()
    {
        // Root: fills its parent and darkens what is behind (also blocks clicks on the desk)
        GameObject root = NewUI("MissionFolder", null, typeof(Image));
        Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.GetComponent<Image>().color = BackdropColor;
        MissionFolder folder = root.AddComponent<MissionFolder>();

        // The folder (manila)
        GameObject body = NewUI("Folder", root.transform, typeof(Image));
        Stretch((RectTransform)body.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
        body.GetComponent<Image>().color = FolderColor;

        // Tab on top of the folder, with the title
        GameObject tab = NewUI("Tab", body.transform, typeof(Image));
        Stretch((RectTransform)tab.transform, new Vector2(0, 1), new Vector2(0.35f, 1), new Vector2(0, 0), new Vector2(0, 70));
        tab.GetComponent<Image>().color = TabColor;
        TextMeshProUGUI title = CreateText(tab.transform, "Title", "MISSION FOLDER", 38, InkColor);
        title.fontStyle = FontStyles.Bold;
        Stretch(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Button close = CreateButton(body.transform, "Close", "X", new Color(0.8f, 0.2f, 0.15f, 1f), Color.white);
        Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-15, -15), new Vector2(70, 70));
        UnityEventTools.AddPersistentListener(close.onClick, folder.Close);

        // Footer: previous page, page number, next page
        Button previous = CreateButton(body.transform, "Previous", "<", TabColor, InkColor);
        Place((RectTransform)previous.transform, new Vector2(0.4f, 0), new Vector2(0, 15), new Vector2(80, 60));
        UnityEventTools.AddPersistentListener(previous.onClick, folder.PreviousPage);
        folder.previousButton = previous;

        TextMeshProUGUI pageLabel = CreateText(body.transform, "PageLabel", "Page 1 / 1", 28, InkColor);
        Place(pageLabel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(220, 60));
        folder.pageLabel = pageLabel;

        Button next = CreateButton(body.transform, "Next", ">", TabColor, InkColor);
        Place((RectTransform)next.transform, new Vector2(0.6f, 0), new Vector2(0, 15), new Vector2(80, 60));
        UnityEventTools.AddPersistentListener(next.onClick, folder.NextPage);
        folder.nextButton = next;

        // The paper sheet. Every page goes inside "Pages" (task 5 adds its own page here)
        GameObject paperShadow = NewUI("PaperShadow", body.transform, typeof(Image));
        Stretch((RectTransform)paperShadow.transform, Vector2.zero, Vector2.one, new Vector2(38, 82), new Vector2(-92, -38));
        paperShadow.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        GameObject paper = NewUI("Pages", body.transform, typeof(Image));
        Stretch((RectTransform)paper.transform, Vector2.zero, Vector2.one, new Vector2(30, 90), new Vector2(-100, -30));
        paper.GetComponent<Image>().color = PaperColor;

        GameObject page1 = NewUI("Page 1 - Mission", paper.transform);
        Stretch((RectTransform)page1.transform, Vector2.zero, Vector2.one, new Vector2(30, 25), new Vector2(-30, -25));
        folder.pages.Add(page1);

        // Left column: briefing (title and description), 4 fact tiles, details and checklist
        TextMeshProUGUI briefingHeading = CreateHeading(page1.transform, "BriefingHeading", "Mission briefing");
        Stretch(briefingHeading.rectTransform, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(0, -50), Vector2.zero);

        TextMeshProUGUI briefing = CreateText(page1.transform, "Briefing", "", 24, InkColor);
        briefing.alignment = TextAlignmentOptions.TopLeft;
        Stretch(briefing.rectTransform, new Vector2(0, 0.74f), new Vector2(0.55f, 1), Vector2.zero, new Vector2(0, -62));
        folder.briefingText = briefing;

        for (int i = 0; i < 4; i++)
        {
            GameObject tile = CreateCard(page1.transform, "Fact " + MissionFolder.FactLabels[i], CardColor);
            Stretch((RectTransform)tile.transform, new Vector2(i * 0.1375f, 0.6f), new Vector2((i + 1) * 0.1375f, 0.72f), new Vector2(4, 0), new Vector2(-4, 0));
            GameObject strip = NewUI("Strip", tile.transform, typeof(Image));
            Stretch((RectTransform)strip.transform, new Vector2(0, 1), Vector2.one, new Vector2(0, -5), Vector2.zero);
            strip.GetComponent<Image>().color = AccentColor;

            TextMeshProUGUI label = CreateText(tile.transform, "Label", MissionFolder.FactLabels[i], 14, HeadingColor);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2;
            Stretch(label.rectTransform, new Vector2(0, 0.55f), Vector2.one, new Vector2(6, 0), new Vector2(-6, -6));
            FitSize(label, 9, 14);

            TextMeshProUGUI value = CreateText(tile.transform, "Value", "-", 26, InkColor);
            value.fontStyle = FontStyles.Bold;
            Stretch(value.rectTransform, Vector2.zero, new Vector2(1, 0.58f), new Vector2(6, 4), new Vector2(-6, 0));
            FitSize(value, 14, 26);
            folder.factValues[i] = value;
        }

        TextMeshProUGUI details = CreateText(page1.transform, "Details", "", 20, InkColor);
        details.alignment = TextAlignmentOptions.TopLeft;
        Stretch(details.rectTransform, new Vector2(0, 0.40f), new Vector2(0.55f, 0.585f), Vector2.zero, Vector2.zero);
        FitSize(details, 12, 20);
        folder.detailsText = details;

        // Checklist as sticky notes (yellow = to do, green = done), 2 x 2
        TextMeshProUGUI checklistHeading = CreateHeading(page1.transform, "ChecklistHeading", "Checklist");
        Stretch(checklistHeading.rectTransform, new Vector2(0, 0.38f), new Vector2(0.55f, 0.38f), new Vector2(0, -50), Vector2.zero);

        GameObject notes = NewUI("ChecklistNotes", page1.transform);
        Stretch((RectTransform)notes.transform, Vector2.zero, new Vector2(0.55f, 0.38f), Vector2.zero, new Vector2(0, -58));
        float[] angles = { -2f, 1.5f, 1f, -1.5f };
        for (int i = 0; i < 4; i++)
        {
            int col = i % 2;
            float rowTop = 1f - (i / 2) * 0.5f;
            GameObject note = CreateCard(notes.transform, "Note " + (i + 1), NoteColor);
            Stretch((RectTransform)note.transform, new Vector2(col * 0.5f, rowTop - 0.5f), new Vector2((col + 1) * 0.5f, rowTop), new Vector2(8, 8), new Vector2(-8, -8));
            note.transform.localRotation = Quaternion.Euler(0, 0, angles[i]);

            GameObject tape = NewUI("Tape", note.transform, typeof(Image));
            Place((RectTransform)tape.transform, new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(70, 18));
            tape.transform.localRotation = Quaternion.Euler(0, 0, -3);
            tape.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.55f);
            tape.GetComponent<Image>().raycastTarget = false;

            TextMeshProUGUI noteText = CreateText(note.transform, "Text", "", 18, InkColor);
            noteText.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(noteText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -12));
            FitSize(noteText, 10, 18);
            folder.checklistNotes[i] = note.GetComponent<Image>();
            folder.checklistNoteTexts[i] = noteText;
        }

        // Right column: mission updates (random events), in a scroll list
        TextMeshProUGUI updatesHeading = CreateHeading(page1.transform, "UpdatesHeading", "Mission updates");
        Stretch(updatesHeading.rectTransform, new Vector2(0.58f, 1), Vector2.one, new Vector2(0, -50), Vector2.zero);

        GameObject scroll = NewUI("Updates", page1.transform, typeof(Image), typeof(ScrollRect));
        Stretch((RectTransform)scroll.transform, new Vector2(0.58f, 0), Vector2.one, Vector2.zero, new Vector2(0, -60));
        scroll.GetComponent<Image>().color = CardColor;
        scroll.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.18f);

        GameObject viewport = NewUI("Viewport", scroll.transform, typeof(Image), typeof(RectMask2D));
        Stretch((RectTransform)viewport.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // almost invisible, but catches the mouse wheel

        TextMeshProUGUI updates = CreateText(viewport.transform, "Content", "", 22, InkColor);
        updates.alignment = TextAlignmentOptions.TopLeft;
        updates.margin = new Vector4(15, 10, 15, 10);
        RectTransform updatesRt = updates.rectTransform;
        updatesRt.anchorMin = new Vector2(0, 1);
        updatesRt.anchorMax = Vector2.one;
        updatesRt.pivot = new Vector2(0.5f, 1);
        updatesRt.sizeDelta = Vector2.zero;
        updates.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        folder.updatesText = updates;

        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.content = updatesRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30;

        // Page 2: crew (task 5)
        folder.pages.Add(CreateCrewPage(paper.transform));

        // Data
        folder.mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (folder.mission == null)
            Debug.LogWarning("Mission folder: mission not found at " + MissionPath + ". Run SpaceApps > Import Artemis catalog.");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    [MenuItem("SpaceApps/Mission folder/Add to open scene")]
    static void AddToScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            CreatePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // Overlay canvas only: never the PC monitor canvas (PC_Desktop) of 01_Planeacion (audit F02)
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add mission folder");
        // On the desk the folder starts closed: the folder station calls Open() when it is clicked
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Scenes.Planning)
        {
            instance.SetActive(false);
            Debug.Log("Mission folder added INACTIVE to " + Scenes.Planning + ": the folder station must call MissionFolder.Open().");
        }
        Selection.activeGameObject = instance;
    }

    // ---------- Play mode tests ----------

    [MenuItem("SpaceApps/Mission folder/Test: open folder")]
    static void TestOpen()
    {
        MissionFolder folder = FindFolder();
        if (folder != null) folder.Open();
    }

    [MenuItem("SpaceApps/Mission folder/Test: roll planning event")]
    static void TestRoll()
    {
        MissionFolder folder = FindFolder();
        if (folder == null) return;
        EventData e = folder.RollPlanningUpdate();
        Debug.Log(e != null ? "Planning event: " + e.eventName : "No planning event this time (or none left).");
    }

    [MenuItem("SpaceApps/Mission folder/Test: reset game")]
    static void TestReset()
    {
        GameManager.Instance.ResetGame();
        MissionFolder folder = FindFolder();
        if (folder != null) folder.Refresh();
    }

    [MenuItem("SpaceApps/Mission folder/Test: open folder", true)]
    [MenuItem("SpaceApps/Mission folder/Test: roll planning event", true)]
    [MenuItem("SpaceApps/Mission folder/Test: reset game", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
    }

    // Also finds the folder when it is closed (inactive)
    static MissionFolder FindFolder()
    {
        MissionFolder folder = Object.FindAnyObjectByType<MissionFolder>(FindObjectsInactive.Include);
        if (folder == null) Debug.LogWarning("Mission folder: there is no MissionFolder in the scene. Use SpaceApps > Mission folder > Add to open scene.");
        return folder;
    }

    // ---------- Page 2: crew (task 5) ----------

    static GameObject CreateCrewPage(Transform pages)
    {
        GameObject page = NewUI("Page 2 - Crew", pages);
        Stretch((RectTransform)page.transform, Vector2.zero, Vector2.one, new Vector2(30, 25), new Vector2(-30, -25));
        CrewPage crew = page.AddComponent<CrewPage>();

        // ----- Left column: candidates as small cards (made at runtime from the template) -----
        TextMeshProUGUI listHeading = CreateHeading(page.transform, "CandidatesHeading", "Candidates");
        Stretch(listHeading.rectTransform, new Vector2(0, 1), new Vector2(0.28f, 1), new Vector2(0, -50), Vector2.zero);

        GameObject scroll = NewUI("Candidates", page.transform, typeof(Image), typeof(ScrollRect));
        Stretch((RectTransform)scroll.transform, Vector2.zero, new Vector2(0.28f, 1), Vector2.zero, new Vector2(0, -62));
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.04f);

        GameObject viewport = NewUI("Viewport", scroll.transform, typeof(Image), typeof(RectMask2D));
        Stretch((RectTransform)viewport.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

        GameObject content = NewUI("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRt = (RectTransform)content.transform;
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = Vector2.one;
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button template = CreateButton(content.transform, "CandidateTemplate", "Name", CardColor, InkColor);
        ((RectTransform)template.transform).sizeDelta = new Vector2(0, 64);
        template.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.15f);
        GameObject strip = NewUI("Strip", template.transform, typeof(Image));
        Stretch((RectTransform)strip.transform, Vector2.zero, new Vector2(0, 1), new Vector2(0, 4), new Vector2(8, -4));
        strip.GetComponent<Image>().raycastTarget = false;
        TextMeshProUGUI templateText = template.GetComponentInChildren<TextMeshProUGUI>();
        templateText.fontSize = 22;
        templateText.fontStyle = FontStyles.Normal;
        templateText.alignment = TextAlignmentOptions.MidlineLeft;
        templateText.margin = new Vector4(20, 0, 8, 0);
        FitSize(templateText, 12, 22);
        template.gameObject.SetActive(false);
        crew.listContent = content.transform;
        crew.candidateTemplate = template;

        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30;

        // ----- Middle column: the candidate's file (photo, name, specialty, trait stamp, bio) -----
        const float midLeft = 0.31f;
        const float midRight = 0.63f;
        TextMeshProUGUI profileHeading = CreateHeading(page.transform, "ProfileHeading", "Profile");
        Stretch(profileHeading.rectTransform, new Vector2(midLeft, 1), new Vector2(midRight, 1), new Vector2(0, -50), Vector2.zero);

        GameObject frame = CreateCard(page.transform, "PhotoFrame", CardColor);
        Stretch((RectTransform)frame.transform, new Vector2(midLeft, 1), new Vector2(midLeft, 1), new Vector2(4, -280), new Vector2(170, -80));
        frame.transform.localRotation = Quaternion.Euler(0, 0, 2);
        GameObject photoBack = NewUI("PhotoBack", frame.transform, typeof(Image));
        Stretch((RectTransform)photoBack.transform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
        photoBack.GetComponent<Image>().color = new Color(0.80f, 0.84f, 0.90f, 1f);
        TextMeshProUGUI initials = CreateText(frame.transform, "Initials", "?", 64, HeadingColor);
        initials.fontStyle = FontStyles.Bold;
        Stretch(initials.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        crew.initialsText = initials;

        GameObject portrait = NewUI("Portrait", frame.transform, typeof(Image));
        Stretch((RectTransform)portrait.transform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
        portrait.GetComponent<Image>().preserveAspect = true;
        portrait.SetActive(false); // shown only when the astronaut has a portrait
        crew.portraitImage = portrait.GetComponent<Image>();

        GameObject clip = NewUI("PaperClip", frame.transform, typeof(Image));
        Place((RectTransform)clip.transform, new Vector2(0.5f, 1), new Vector2(0, 22), new Vector2(16, 60));
        clip.transform.localRotation = Quaternion.Euler(0, 0, -8);
        clip.GetComponent<Image>().sprite = UiSprite();
        clip.GetComponent<Image>().type = Image.Type.Sliced;
        clip.GetComponent<Image>().color = new Color(0.62f, 0.64f, 0.68f, 1f);

        TextMeshProUGUI astronautName = CreateText(page.transform, "Name", "", 30, InkColor);
        astronautName.fontStyle = FontStyles.Bold;
        astronautName.alignment = TextAlignmentOptions.BottomLeft;
        Stretch(astronautName.rectTransform, new Vector2(midLeft, 1), new Vector2(midRight, 1), new Vector2(190, -140), new Vector2(0, -75));
        FitSize(astronautName, 16, 30);
        crew.nameText = astronautName;

        TextMeshProUGUI specialty = CreateText(page.transform, "Specialty", "", 18, HeadingColor);
        specialty.fontStyle = FontStyles.Bold;
        specialty.characterSpacing = 4;
        specialty.alignment = TextAlignmentOptions.TopLeft;
        Stretch(specialty.rectTransform, new Vector2(midLeft, 1), new Vector2(midRight, 1), new Vector2(190, -172), new Vector2(0, -144));
        crew.specialtyText = specialty;

        GameObject stamp = CreateCard(page.transform, "TraitStamp", TraitPlaceholder);
        RectTransform stampRt = (RectTransform)stamp.transform;
        stampRt.anchorMin = stampRt.anchorMax = new Vector2(midLeft, 1);
        stampRt.pivot = new Vector2(0, 1);
        stampRt.anchoredPosition = new Vector2(192, -186);
        stampRt.sizeDelta = new Vector2(190, 42);
        stamp.transform.localRotation = Quaternion.Euler(0, 0, -4);
        TextMeshProUGUI stampText = CreateText(stamp.transform, "Text", "TRAIT", 20, Color.white);
        stampText.fontStyle = FontStyles.Bold;
        stampText.characterSpacing = 3;
        Stretch(stampText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));
        FitSize(stampText, 12, 20);
        crew.traitStamp = stamp.GetComponent<Image>();
        crew.traitStampText = stampText;

        TextMeshProUGUI bio = CreateText(page.transform, "Bio", "", 22, InkColor);
        bio.alignment = TextAlignmentOptions.TopLeft;
        Stretch(bio.rectTransform, new Vector2(midLeft, 0.2f), new Vector2(midRight, 1), Vector2.zero, new Vector2(0, -300));
        FitSize(bio, 14, 22);
        crew.bioText = bio;

        TextMeshProUGUI assignLabel = CreateText(page.transform, "AssignLabel", "ASSIGN AS", 16, HeadingColor);
        assignLabel.fontStyle = FontStyles.Bold;
        assignLabel.characterSpacing = 4;
        assignLabel.alignment = TextAlignmentOptions.BottomLeft;
        Stretch(assignLabel.rectTransform, new Vector2(midLeft, 0.12f), new Vector2(midRight, 0.18f), Vector2.zero, Vector2.zero);

        float third = (midRight - midLeft) / 3f;
        for (int slot = 0; slot < 3; slot++)
        {
            Button assign = CreateButton(page.transform, "Make " + CrewPage.RoleNames[slot], CrewPage.RoleNames[slot], HeadingColor, Color.white);
            Stretch((RectTransform)assign.transform, new Vector2(midLeft + slot * third, 0.02f), new Vector2(midLeft + (slot + 1) * third, 0.115f), new Vector2(slot == 0 ? 0 : 4, 0), new Vector2(slot == 2 ? 0 : -4, 0));
            assign.GetComponentInChildren<TextMeshProUGUI>().fontSize = 22;
            assign.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.2f);
            crew.assignButtons[slot] = assign;
        }

        // ----- Right column: training points and the 3 crew badges -----
        TextMeshProUGUI crewHeading = CreateHeading(page.transform, "CrewHeading", "Crew and training");
        Stretch(crewHeading.rectTransform, new Vector2(0.66f, 1), Vector2.one, new Vector2(0, -50), Vector2.zero);

        GameObject pointsBox = NewUI("TrainingPoints", page.transform, typeof(Image));
        Stretch((RectTransform)pointsBox.transform, new Vector2(0.66f, 1), Vector2.one, new Vector2(0, -100), new Vector2(0, -62));
        pointsBox.GetComponent<Image>().color = new Color(HeadingColor.r, HeadingColor.g, HeadingColor.b, 0.1f);
        TextMeshProUGUI pointsText = CreateText(pointsBox.transform, "Text", "TRAINING POINTS", 18, HeadingColor);
        pointsText.characterSpacing = 2;
        pointsText.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(pointsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-150, 0));
        FitSize(pointsText, 10, 18);
        crew.pointsText = pointsText;
        crew.pointPips = new Image[CrewTraining.PointPool];
        for (int p = 0; p < CrewTraining.PointPool; p++)
        {
            Image pip = CreatePip(pointsBox.transform, "Point " + (p + 1), 18);
            Place(pip.rectTransform, new Vector2(1, 0.5f), new Vector2(-12 - (CrewTraining.PointPool - 1 - p) * 23, 0), new Vector2(18, 18));
            crew.pointPips[p] = pip;
        }

        string[] focusLabels = { "Piloting", "Engineering", "Research" };
        crew.trainingPips = new Image[36];
        for (int slot = 0; slot < 3; slot++)
        {
            float top = 0.855f - slot * 0.237f;
            GameObject card = CreateCard(page.transform, CrewPage.RoleNames[slot] + " slot", CardColor);
            Stretch((RectTransform)card.transform, new Vector2(0.66f, top - 0.226f), new Vector2(1, top), Vector2.zero, Vector2.zero);

            GameObject band = NewUI("Band", card.transform, typeof(Image));
            Stretch((RectTransform)band.transform, new Vector2(0, 0.78f), Vector2.one, Vector2.zero, Vector2.zero);
            band.GetComponent<Image>().color = HeadingColor;

            TextMeshProUGUI role = CreateText(band.transform, "Role", CrewPage.RoleNames[slot].ToUpper(), 20, Color.white);
            role.fontStyle = FontStyles.Bold;
            role.characterSpacing = 4;
            role.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(role.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-50, 0));

            // "X" takes the astronaut out of the slot
            Button remove = CreateButton(band.transform, "Remove", "X", StampRed, Color.white);
            Place((RectTransform)remove.transform, new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(34, 34));
            remove.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;
            crew.removeButtons[slot] = remove;

            TextMeshProUGUI slotName = CreateText(card.transform, "Astronaut", "", 22, InkColor);
            slotName.alignment = TextAlignmentOptions.MidlineLeft;
            FitSize(slotName, 12, 22);
            Stretch(slotName.rectTransform, new Vector2(0, 0.56f), new Vector2(1, 0.78f), new Vector2(12, 0), new Vector2(-12, 0));
            crew.slotNames[slot] = slotName;

            TextMeshProUGUI readinessLabel = CreateText(card.transform, "ReadinessLabel", "READINESS", 13, HeadingColor);
            readinessLabel.fontStyle = FontStyles.Bold;
            readinessLabel.characterSpacing = 2;
            readinessLabel.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(readinessLabel.rectTransform, new Vector2(0, 0.41f), new Vector2(0, 0.55f), new Vector2(12, 0), new Vector2(112, 0));

            GameObject bar = NewUI("ReadinessBar", card.transform, typeof(Image));
            Stretch((RectTransform)bar.transform, new Vector2(0, 0.42f), new Vector2(1, 0.54f), new Vector2(116, 0), new Vector2(-12, 0));
            bar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.1f);
            GameObject fill = NewUI("Fill", bar.transform, typeof(Image));
            Stretch((RectTransform)fill.transform, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            fill.GetComponent<Image>().raycastTarget = false;
            crew.readinessFills[slot] = fill.GetComponent<Image>();
            TextMeshProUGUI readiness = CreateText(bar.transform, "Text", "", 14, InkColor);
            readiness.fontStyle = FontStyles.Bold;
            readiness.alignment = TextAlignmentOptions.MidlineRight;
            Stretch(readiness.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-6, 0));
            crew.readinessTexts[slot] = readiness;

            for (int f = 0; f < 3; f++)
            {
                Button train = CreateButton(card.transform, "Train " + focusLabels[f], focusLabels[f], CardColor, InkColor);
                Stretch((RectTransform)train.transform, new Vector2(f / 3f, 0.04f), new Vector2((f + 1) / 3f, 0.38f), new Vector2(8, 0), new Vector2(-8, 0));
                TextMeshProUGUI trainText = train.GetComponentInChildren<TextMeshProUGUI>();
                trainText.fontSize = 16;
                Stretch(trainText.rectTransform, new Vector2(0, 0.45f), Vector2.one, new Vector2(2, 0), new Vector2(-2, -2));
                FitSize(trainText, 10, 16);
                crew.trainingButtons[slot * 3 + f] = train;
                for (int p = 0; p < 4; p++)
                {
                    Image pip = CreatePip(train.transform, "Pip " + (p + 1), 11);
                    Place(pip.rectTransform, new Vector2(0.5f, 0), new Vector2((p - 1.5f) * 15, 7), new Vector2(11, 11));
                    crew.trainingPips[(slot * 3 + f) * 4 + p] = pip;
                }
            }

            GameObject tired = CreateStamp(card.transform, "TiredStamp", "TIRED", StampRed, 24);
            RectTransform tiredRt = (RectTransform)tired.transform;
            tiredRt.anchorMin = tiredRt.anchorMax = new Vector2(0.8f, 0.67f);
            tiredRt.pivot = new Vector2(0.5f, 0.5f);
            tiredRt.sizeDelta = new Vector2(120, 40);
            tired.transform.localRotation = Quaternion.Euler(0, 0, 12);
            tired.SetActive(false);
            crew.tiredStamps[slot] = tired;
        }

        // Sticky note with the tip
        GameObject note = CreateCard(page.transform, "TipNote", NoteColor);
        Stretch((RectTransform)note.transform, new Vector2(0.66f, 0), new Vector2(1, 0.14f), new Vector2(4, 0), new Vector2(-4, 0));
        note.transform.localRotation = Quaternion.Euler(0, 0, -1);
        TextMeshProUGUI tip = CreateText(note.transform, "Tip", "", 18, InkColor);
        tip.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(tip.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
        FitSize(tip, 11, 18);
        crew.tipText = tip;

        // Big stamp over the badges when the crew is ready (clicks go through it)
        GameObject ready = CreateStamp(page.transform, "CrewReadyStamp", "CREW READY", StampGreen, 40);
        RectTransform readyRt = (RectTransform)ready.transform;
        readyRt.anchorMin = readyRt.anchorMax = new Vector2(0.83f, 0.52f);
        readyRt.pivot = new Vector2(0.5f, 0.5f);
        readyRt.sizeDelta = new Vector2(300, 70);
        ready.transform.localRotation = Quaternion.Euler(0, 0, -10);
        ready.SetActive(false);
        crew.crewReadyStamp = ready;

        return page;
    }

    // ---------- UI helpers ----------

    static Sprite UiSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    // Rounded paper card with a soft shadow
    static GameObject CreateCard(Transform parent, string name, Color color)
    {
        GameObject go = NewUI(name, parent, typeof(Image), typeof(Shadow));
        Image img = go.GetComponent<Image>();
        img.sprite = UiSprite();
        img.type = Image.Type.Sliced;
        img.color = color;
        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
        shadow.effectDistance = new Vector2(3, -3);
        return go;
    }

    // Small circle (training points)
    static Image CreatePip(Transform parent, string name, float size)
    {
        GameObject go = NewUI(name, parent, typeof(Image));
        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        img.color = new Color(0f, 0f, 0f, 0.15f);
        img.raycastTarget = false;
        ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
        return img;
    }

    // Rubber stamp: colored border, paper inside, bold colored text. Does not block clicks.
    static GameObject CreateStamp(Transform parent, string name, string text, Color color, float fontSize)
    {
        GameObject go = NewUI(name, parent, typeof(Image));
        Image border = go.GetComponent<Image>();
        border.sprite = UiSprite();
        border.type = Image.Type.Sliced;
        border.color = color;
        border.raycastTarget = false;

        GameObject inside = NewUI("Inside", go.transform, typeof(Image));
        Stretch((RectTransform)inside.transform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
        Image insideImage = inside.GetComponent<Image>();
        insideImage.sprite = UiSprite();
        insideImage.type = Image.Type.Sliced;
        insideImage.color = new Color(1f, 0.98f, 0.94f, 0.9f);
        insideImage.raycastTarget = false;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, fontSize, color);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 6;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));
        FitSize(label, 10, fontSize);
        return go;
    }

    static void FitSize(TextMeshProUGUI text, float min, float max)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
    }


    static GameObject NewUI(string name, Transform parent, params System.Type[] components)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        foreach (System.Type t in components) go.AddComponent(t);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = NewUI(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false; // so texts do not block clicks or the mouse wheel
        return t;
    }

    static TextMeshProUGUI CreateHeading(Transform parent, string name, string text)
    {
        TextMeshProUGUI t = CreateText(parent, name, text.ToUpper(), 26, HeadingColor);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 5;
        t.alignment = TextAlignmentOptions.TopLeft;

        // Thin rule under the heading
        GameObject rule = NewUI("Rule", t.transform, typeof(Image));
        Stretch((RectTransform)rule.transform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 8), new Vector2(0, 10));
        Image ruleImage = rule.GetComponent<Image>();
        ruleImage.color = new Color(HeadingColor.r, HeadingColor.g, HeadingColor.b, 0.5f);
        ruleImage.raycastTarget = false;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string text, Color color, Color textColor)
    {
        GameObject go = NewUI(name, parent, typeof(Image), typeof(Button));
        Image img = go.GetComponent<Image>();
        img.sprite = UiSprite();
        img.type = Image.Type.Sliced;
        img.color = color;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); // faded on the first and last page
        button.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, 32, textColor);
        label.fontStyle = FontStyles.Bold;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    // Fixed-size element anchored (and pivoted) at one point of its parent
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
