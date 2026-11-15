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

    public static readonly string[] RoleNames = { "Pilot", "Engineer", "Scientist" };
    public static readonly TrainingFocus[] Focuses = { TrainingFocus.Piloting, TrainingFocus.Engineering, TrainingFocus.Research };

    static readonly Color Normal = new Color(0.74f, 0.58f, 0.34f, 1f);
    static readonly Color Selected = new Color(1f, 0.6f, 0f, 1f);

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
            if (slot < removeButtons.Length && removeButtons[slot] != null) removeButtons[slot].onClick.AddListener(() => Remove(s));

            for (int f = 0; f < 3; f++)
            {
                Button b = trainingButtons[slot * 3 + f];
                TrainingFocus focus = Focuses[f];
                if (b != null) b.onClick.AddListener(() => SetTraining(s, focus));
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

    // ---------- Texts and colors ----------

    public void Refresh()
    {
        GameManager gm = GameManager.Instance;

        for (int i = 0; i < candidateButtons.Count; i++)
        {
            AstronautData a = shownList[i];
            Button b = candidateButtons[i];
            int slot = SlotOf(gm, a);
            string label = a != null ? a.astronautName : "?";
            if (slot >= 0) label += "  <b>[" + RoleNames[slot] + "]</b>";
            b.GetComponentInChildren<TMP_Text>().text = label;
            b.image.color = a == selected ? Selected : Normal;
        }

        if (nameText != null) nameText.text = selected != null ? selected.astronautName : "No candidates. Run SpaceApps > Import P5 data.";
        if (bioText != null) bioText.text = selected != null ? selected.bio : "";
        if (portraitImage != null)
        {
            portraitImage.sprite = selected != null ? selected.portrait : null;
            portraitImage.gameObject.SetActive(portraitImage.sprite != null);
        }

        for (int slot = 0; slot < 3; slot++)
        {
            if (assignButtons[slot] != null) assignButtons[slot].interactable = selected != null && GetCrew(gm, slot) != selected;

            AstronautData crew = GetCrew(gm, slot);
            if (slotNames[slot] != null) slotNames[slot].text = crew != null ? crew.astronautName : "<i>empty</i>";

            if (slot < removeButtons.Length && removeButtons[slot] != null) removeButtons[slot].interactable = crew != null;

            TrainingFocus training = GetTraining(gm, slot);
            for (int f = 0; f < 3; f++)
            {
                Button b = trainingButtons[slot * 3 + f];
                if (b != null) b.interactable = crew != null;
                if (b != null) b.image.color = training == Focuses[f] ? Selected : Normal;
            }
        }

        if (tipText != null) tipText.text = BuildTip(gm);
    }

    public static string BuildTip(GameManager gm)
    {
        if (gm.pilot == null || gm.engineer == null || gm.scientist == null)
            return "Read the profiles and choose a pilot, an engineer and a scientist.";
        if (gm.pilotTraining == TrainingFocus.None || gm.engineerTraining == TrainingFocus.None || gm.scientistTraining == TrainingFocus.None)
            return "Choose what each astronaut trains before the flight.";
        return "Crew ready. Training improves that skill during the flight minigames, so it helps most when it matches the role.";
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
