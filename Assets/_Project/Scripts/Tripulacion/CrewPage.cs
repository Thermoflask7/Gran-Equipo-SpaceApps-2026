using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Page 2 of the mission folder (task 5, P3): astronaut profiles and training.
// Left: list of candidates. Middle: profile of the selected candidate and buttons to give them a role.
// Right: the 3 crew slots (pilot, engineer, scientist), each with its training focus.
// Writes only to GameManager: pilot/engineer/scientist, their training and reserves (everyone not assigned).
// Skills are hidden: the player only reads the bio and decides.
// Slots are numbered 0 = pilot, 1 = engineer, 2 = scientist.
public class CrewPage : MonoBehaviour
{
    [Header("Candidates (empty = GameData.Instance.astronauts)")]
    public List<AstronautData> candidates = new List<AstronautData>();

    [Header("List")]
    public Transform listContent;
    public Button candidateTemplate; // inactive button that is copied for each candidate

    [Header("Profile")]
    public TMP_Text nameText;
    public TMP_Text bioText;
    public Image portraitImage;
    public Button[] assignButtons = new Button[3]; // "Make pilot / engineer / scientist"

    [Header("Crew slots")]
    public TMP_Text[] slotNames = new TMP_Text[3];
    public Button[] trainingButtons = new Button[9]; // slot * 3 + focus (0 Piloting, 1 Engineering, 2 Research)
    public Button[] removeButtons = new Button[3];   // "X": takes the astronaut out of the slot
    public TMP_Text tipText;

    [Header("Look (optional: the old prefab works without them)")]
    public TMP_Text initialsText;              // shown behind the portrait when there is no photo
    public TMP_Text specialtyText;             // "PILOT", "ENGINEER"... from the highest hidden skill
    public Image traitStamp;                   // colored stamp with the trait name
    public TMP_Text traitStampText;
    public TMP_Text pointsText;                // "TRAINING POINTS 4 / 6"
    public Image[] pointPips = new Image[6];   // one dot per point left
    public Image[] trainingPips = new Image[36]; // (slot * 3 + focus) * 4 + pip
    public Image[] readinessFills = new Image[3];
    public TMP_Text[] readinessTexts = new TMP_Text[3];
    public GameObject[] tiredStamps = new GameObject[3];
    public GameObject crewReadyStamp;

    public static readonly string[] RoleNames = { "Pilot", "Engineer", "Scientist" };
    public static readonly TrainingFocus[] Focuses = { TrainingFocus.Piloting, TrainingFocus.Engineering, TrainingFocus.Research };

    static readonly Color Normal = new Color(0.93f, 0.89f, 0.80f, 1f);
    static readonly Color Selected = new Color(1f, 0.78f, 0.45f, 1f);
    static readonly Color PipOn = new Color(0.95f, 0.55f, 0.10f, 1f);
    static readonly Color PipOff = new Color(0f, 0f, 0f, 0.15f);

    AstronautData selected;
    List<Button> candidateButtons = new List<Button>();
    List<AstronautData> shownList = new List<AstronautData>();
    bool wired;

    void OnEnable()
    {
        Wire();
        BuildList();
        if (selected == null && shownList.Count > 0) selected = shownList[0];
        UpdateReserves(GameManager.Instance, Candidates()); // reserves are right from the first opening
        Refresh();
    }

    // Buttons with a parameter are connected here (not in the prefab) to keep the builder simple
    void Wire()
    {
        if (wired) return;
        wired = true;

        for (int slot = 0; slot < 3; slot++)
        {
            int s = slot;
            if (assignButtons[slot] != null) assignButtons[slot].onClick.AddListener(() => AssignSelected(s));
            FitText(slotNames[slot], 26f);
            if (slot < removeButtons.Length && removeButtons[slot] != null) removeButtons[slot].onClick.AddListener(() => Remove(s));

            for (int f = 0; f < 3; f++)
            {
                Button b = trainingButtons[slot * 3 + f];
                int focusIndex = f;
                if (b != null) b.onClick.AddListener(() => TrainPoint(s, focusIndex));
                if (b != null) FitText(b.GetComponentInChildren<TMP_Text>(), 20f);
            }
        }
    }

    public List<AstronautData> Candidates()
    {
        if (candidates.Count > 0) return candidates;
        if (GameData.Instance != null) return GameData.Instance.astronauts;
        return candidates;
    }

    // One button per candidate, copied from the template
    void BuildList()
    {
        List<AstronautData> list = Candidates();
        if (candidateTemplate == null || listContent == null) return;
        if (SameList(list)) return;

        foreach (Button b in candidateButtons) Destroy(b.gameObject);
        candidateButtons.Clear();
        shownList = new List<AstronautData>(list);

        foreach (AstronautData a in shownList)
        {
            Button b = Instantiate(candidateTemplate, listContent);
            b.gameObject.SetActive(true);
            b.name = "Candidate " + (a != null ? a.astronautName : "?");
            AstronautData astronaut = a;
            b.onClick.AddListener(() => Select(astronaut));
            candidateButtons.Add(b);
        }
    }

    // The slot and training texts now have two parts, so they shrink to fit the prefab boxes
    static void FitText(TMP_Text text, float maxSize)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = maxSize;
    }

    bool SameList(List<AstronautData> list)
    {
        if (list.Count != shownList.Count) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != shownList[i]) return false;
        }
        return true;
    }

    // ---------- Actions ----------

    public void Select(AstronautData astronaut)
    {
        selected = astronaut;
        Refresh();
    }

    public void AssignSelected(int slot)
    {
        Assign(selected, slot);
    }

    // Puts the astronaut in the slot. If they had another role, that role is left empty (and its training is cleared).
    // The training stays with the slot, so the player decides again only if they want.
    public void Assign(AstronautData astronaut, int slot)
    {
        if (astronaut == null || slot < 0 || slot > 2) return;
        GameManager gm = GameManager.Instance;

        for (int i = 0; i < 3; i++)
        {
            if (i != slot && GetCrew(gm, i) == astronaut) ClearSlot(gm, i);
        }
        SetCrew(gm, slot, astronaut);
        UpdateReserves(gm, Candidates());
        Refresh();
    }

    // Takes the astronaut out of the slot (back to the reserves) and clears its training
    public void Remove(int slot)
    {
        if (slot < 0 || slot > 2) return;
        GameManager gm = GameManager.Instance;
        ClearSlot(gm, slot);
        UpdateReserves(gm, Candidates());
        Refresh();
    }

    // Only a slot with an astronaut can choose its training
    public void SetTraining(int slot, TrainingFocus focus)
    {
        GameManager gm = GameManager.Instance;
        if (GetCrew(gm, slot) == null) return;
        SetTrainingOf(gm, slot, focus);
        Refresh();
    }

    // One click = +1 training point; past the max (or with no points left) it goes back to 0
    public void TrainPoint(int slot, int focusIndex)
    {
        if (GetCrew(GameManager.Instance, slot) == null) return;
        CrewTraining.CyclePoint(GameManager.Instance, slot, focusIndex);
        Refresh();
    }

    // ---------- Texts and colors ----------

    public void Refresh()
    {
        GameManager gm = GameManager.Instance;

        for (int i = 0; i < candidateButtons.Count; i++)
        {
            AstronautData a = shownList[i];
            Button b = candidateButtons[i];
            b.GetComponentInChildren<TMP_Text>().text = CandidateLabel(gm, a);
            b.image.color = a == selected ? Selected : Normal;
            Transform strip = b.transform.Find("Strip");
            if (strip != null) strip.GetComponent<Image>().color = SpecialtyColor(a);
        }

        RefreshProfile();

        for (int slot = 0; slot < 3; slot++)
        {
            if (assignButtons[slot] != null) assignButtons[slot].interactable = selected != null && GetCrew(gm, slot) != selected;

            AstronautData crew = GetCrew(gm, slot);
            if (slotNames[slot] != null) slotNames[slot].text = SlotLabel(gm, slot);

            if (slot < removeButtons.Length && removeButtons[slot] != null) removeButtons[slot].interactable = crew != null;

            for (int f = 0; f < 3; f++)
            {
                Button b = trainingButtons[slot * 3 + f];
                if (b == null) continue;
                b.interactable = crew != null;
                int points = CrewTraining.Points(gm, slot, f);
                b.image.color = points > 0 ? Selected : Normal;
                TMP_Text label = b.GetComponentInChildren<TMP_Text>();
                bool hasPips = trainingPips.Length >= 36 && trainingPips[(slot * 3 + f) * 4] != null;
                if (label != null) label.text = hasPips ? Focuses[f].ToString() : Focuses[f] + " <b>" + points + "</b>";
                for (int p = 0; p < 4 && hasPips; p++)
                    trainingPips[(slot * 3 + f) * 4 + p].color = p < points ? PipOn : PipOff;
            }

            RefreshReadiness(gm, slot, crew);
        }

        int left = CrewTraining.PointsLeft(gm);
        if (pointsText != null) pointsText.text = "TRAINING POINTS  <b>" + left + " / " + CrewTraining.PointPool + "</b>";
        for (int p = 0; p < pointPips.Length; p++)
            if (pointPips[p] != null) pointPips[p].color = p < left ? PipOn : PipOff;

        if (crewReadyStamp != null) crewReadyStamp.SetActive(gm.IsCrewReady());
        if (tipText != null) tipText.text = BuildTip(gm);
    }

    // Name, photo (or initials), specialty, trait stamp and bio of the selected candidate
    void RefreshProfile()
    {
        if (nameText != null) nameText.text = selected != null ? selected.astronautName : "No candidates. Run SpaceApps > Import P5 data.";
        if (specialtyText != null) specialtyText.text = selected != null ? Specialty(selected).ToUpper() : "";
        if (initialsText != null) initialsText.text = Initials(selected);

        bool hasStamp = traitStamp != null;
        if (hasStamp)
        {
            bool show = selected != null && selected.trait != CrewTrait.None;
            traitStamp.gameObject.SetActive(show);
            if (show)
            {
                traitStamp.color = TraitColor(selected.trait);
                if (traitStampText != null) traitStampText.text = CrewTraining.TraitName(selected.trait).ToUpper();
            }
        }

        if (bioText != null)
        {
            string bio = selected != null ? selected.bio : "";
            if (selected != null && selected.trait != CrewTrait.None)
            {
                if (hasStamp) bio += "\n\n<i>" + CrewTraining.TraitDescription(selected.trait) + "</i>";
                else bio += "\n\n<b>Trait: " + CrewTraining.TraitName(selected.trait) + "</b>\n" + CrewTraining.TraitDescription(selected.trait);
            }
            bioText.text = bio;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = selected != null ? selected.portrait : null;
            portraitImage.gameObject.SetActive(portraitImage.sprite != null);
        }
    }

    // Readiness bar (filled 0 to 1, colored by level) and the TIRED stamp
    void RefreshReadiness(GameManager gm, int slot, AstronautData crew)
    {
        float m = crew != null ? CrewTraining.Multiplier(gm, crew, Focuses[slot]) : 0f;
        Image fill = slot < readinessFills.Length ? readinessFills[slot] : null;
        if (fill != null)
        {
            RectTransform rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(m), 1f);
            fill.color = ReadinessColor(m);
        }
        TMP_Text text = slot < readinessTexts.Length ? readinessTexts[slot] : null;
        if (text != null) text.text = crew != null ? CrewTraining.Readiness(gm, slot) : "";
        GameObject tired = slot < tiredStamps.Length ? tiredStamps[slot] : null;
        if (tired != null) tired.SetActive(crew != null && CrewTraining.IsTired(gm, crew));
    }

    string CandidateLabel(GameManager gm, AstronautData a)
    {
        if (a == null) return "?";
        string label = "<b>" + a.astronautName + "</b>";
        int slot = SlotOf(gm, a);
        if (slot >= 0) label += "  <color=#1A3F80>[" + RoleNames[slot] + "]</color>";
        label += "\n<size=75%>" + Specialty(a);
        if (a.trait != CrewTrait.None) label += " - " + CrewTraining.TraitName(a.trait);
        return label + "</size>";
    }

    // Name and trait; with the new prefab the readiness and fatigue have their own bar and stamp
    string SlotLabel(GameManager gm, int slot)
    {
        AstronautData crew = GetCrew(gm, slot);
        if (crew == null) return "<i>empty: pick a candidate and assign them</i>";
        string text = "<b>" + crew.astronautName + "</b>";
        if (crew.trait != CrewTrait.None) text += "  <size=70%>(" + CrewTraining.TraitName(crew.trait) + ")</size>";
        bool newLook = slot < readinessFills.Length && readinessFills[slot] != null;
        if (newLook) return text;
        text += "\n<size=70%>Flight readiness: <b>" + CrewTraining.Readiness(gm, slot) + "</b>";
        if (CrewTraining.IsTired(gm, crew)) text += "  <color=#B03020>Tired: -15 % in flight</color>";
        return text + "</size>";
    }

    public static string BuildTip(GameManager gm)
    {
        if (gm.pilot == null || gm.engineer == null || gm.scientist == null)
            return "Read the profiles and choose a pilot, an engineer and a scientist.";
        if (gm.pilotTraining == TrainingFocus.None || gm.engineerTraining == TrainingFocus.None || gm.scientistTraining == TrainingFocus.None)
            return "Click a skill to give it a training point (past " + CrewTraining.MaxPerSkill + " it resets). Every astronaut needs at least one, and only the skill of their role counts in flight.";
        for (int slot = 0; slot < 3; slot++)
        {
            AstronautData a = GetCrew(gm, slot);
            if (CrewTraining.IsTired(gm, a)) return a.astronautName + " is tired from too much training: take a point away.";
            if (CrewTraining.LonerPenalty(gm, a) > 0) return "A Loner is hurting the crew. A Team player on board would fix it.";
        }
        return "Crew ready! Check the readiness bars: you can still move points around.";
    }

    // ---------- Look helpers ----------

    // What the astronaut is best at (no numbers, the skills stay hidden)
    public static string Specialty(AstronautData a)
    {
        if (a == null) return "";
        if (a.piloting > a.engineering && a.piloting > a.research) return "Pilot";
        if (a.engineering > a.piloting && a.engineering > a.research) return "Engineer";
        if (a.research > a.piloting && a.research > a.engineering) return "Scientist";
        return "All-rounder";
    }

    static Color SpecialtyColor(AstronautData a)
    {
        string s = Specialty(a);
        if (s == "Pilot") return new Color(0.16f, 0.36f, 0.70f, 1f);
        if (s == "Engineer") return new Color(0.85f, 0.45f, 0.10f, 1f);
        if (s == "Scientist") return new Color(0.20f, 0.55f, 0.30f, 1f);
        return new Color(0.45f, 0.40f, 0.50f, 1f);
    }

    public static Color TraitColor(CrewTrait trait)
    {
        if (trait == CrewTrait.Veteran) return new Color(0.16f, 0.25f, 0.50f, 1f);
        if (trait == CrewTrait.HardWorker) return new Color(0.65f, 0.35f, 0.10f, 1f);
        if (trait == CrewTrait.TeamPlayer) return new Color(0.18f, 0.50f, 0.25f, 1f);
        if (trait == CrewTrait.Loner) return new Color(0.45f, 0.20f, 0.50f, 1f);
        if (trait == CrewTrait.Rookie) return new Color(0.10f, 0.50f, 0.55f, 1f);
        return Color.gray;
    }

    static Color ReadinessColor(float m)
    {
        if (m >= 0.95f) return new Color(0.20f, 0.60f, 0.30f, 1f);
        if (m >= 0.8f) return new Color(0.45f, 0.70f, 0.30f, 1f);
        if (m >= 0.6f) return new Color(0.90f, 0.65f, 0.15f, 1f);
        return new Color(0.80f, 0.25f, 0.20f, 1f);
    }

    static string Initials(AstronautData a)
    {
        if (a == null || string.IsNullOrEmpty(a.astronautName)) return "?";
        string[] words = a.astronautName.Split(' ');
        string text = "";
        foreach (string w in words)
            if (w.Length > 0 && text.Length < 2) text += char.ToUpper(w[0]);
        return text;
    }

    // ---------- Crew helpers (slot 0 pilot, 1 engineer, 2 scientist) ----------

    public static AstronautData GetCrew(GameManager gm, int slot)
    {
        if (slot == 0) return gm.pilot;
        if (slot == 1) return gm.engineer;
        if (slot == 2) return gm.scientist;
        return null;
    }

    public static void SetCrew(GameManager gm, int slot, AstronautData astronaut)
    {
        if (slot == 0) gm.pilot = astronaut;
        if (slot == 1) gm.engineer = astronaut;
        if (slot == 2) gm.scientist = astronaut;
    }

    public static void SetTrainingOf(GameManager gm, int slot, TrainingFocus focus)
    {
        if (slot == 0) gm.pilotTraining = focus;
        if (slot == 1) gm.engineerTraining = focus;
        if (slot == 2) gm.scientistTraining = focus;
    }

    // Empty slot without training
    public static void ClearSlot(GameManager gm, int slot)
    {
        SetCrew(gm, slot, null);
        CrewTraining.ClearSlotPoints(gm, slot);
        SetTrainingOf(gm, slot, TrainingFocus.None);
    }

    public static TrainingFocus GetTraining(GameManager gm, int slot)
    {
        if (slot == 0) return gm.pilotTraining;
        if (slot == 1) return gm.engineerTraining;
        if (slot == 2) return gm.scientistTraining;
        return TrainingFocus.None;
    }

    // -1 if the astronaut has no role
    public static int SlotOf(GameManager gm, AstronautData astronaut)
    {
        if (astronaut == null) return -1;
        for (int i = 0; i < 3; i++)
        {
            if (GetCrew(gm, i) == astronaut) return i;
        }
        return -1;
    }

    // Reserves = every candidate that is not in the crew
    public static void UpdateReserves(GameManager gm, List<AstronautData> all)
    {
        gm.reserves.Clear();
        foreach (AstronautData a in all)
        {
            if (a != null && SlotOf(gm, a) < 0) gm.reserves.Add(a);
        }
    }
}
