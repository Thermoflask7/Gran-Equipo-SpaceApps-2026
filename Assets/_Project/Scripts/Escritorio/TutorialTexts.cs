// All the texts of the desk tutorials in one place (welcome message + one short screen per station).
// Student-friendly English. No real agency or program names (use launcher, capsule, service module,
// abort tower, spacecraft). Used by TutorialPopup and DeskTutorials.
public static class TutorialTexts
{
    // Station ids (also stored in GameManager.tutorialsSeen)
    public const string Folder = "folder";
    public const string SpaceMart = "spacemart";
    public const string Rocket = "rocket";
    public const string Planning = "planning";
    public const string Launch = "launch";

    public const string WelcomeTitle = "Welcome to mission planning, Agent!";
    public const string WelcomeButton = "Start planning";
    // Stamp picture on the welcome paper: Assets/_Project/Resources/Tutorials/TopSecretStamp.png
    // (public domain CC0 clip art from freesvg.org / OpenClipart, "Top Secret Stamp vector graphics", id 23749)
    public const string WelcomeStampPath = "Tutorials/TopSecretStamp";

    // Welcome body: the mission line + the 4 objectives of LaunchChecklist
    public static string WelcomeBody(GameManager gm)
    {
        string text = "";
        DestinationData mission = gm != null ? gm.mission : null;
        if (mission != null)
        {
            string name = string.IsNullOrEmpty(mission.missionName) ? mission.destinationName : mission.missionName;
            text += "Your mission: <b>" + name + "</b>";
            if (!string.IsNullOrEmpty(mission.missionName) && !string.IsNullOrEmpty(mission.destinationName))
                text += " (" + mission.destinationName + ")";
            text += ".";
            if (!string.IsNullOrEmpty(mission.tagline)) text += " " + mission.tagline + ".";
            text += "\n\n";
        }
        text += "Complete these 4 objectives to unlock the launch button:\n";
        text += "1. Read the mission folder.\n";
        text += "2. Choose and train the crew.\n";
        text += "3. Buy and assemble the rocket in SpaceMart.\n";
        text += "4. Pick the launch site and date.\n\n";
        text += "Click an object on the desk to use it. The <b>?</b> button shows its help again.\n\n";
        text += "<b>Good luck, Agent!</b>";
        return text;
    }

    public static string Title(string id)
    {
        switch (id)
        {
            case Folder: return "The mission folder";
            case SpaceMart: return "SpaceMart: the parts shop";
            case Rocket: return "The rocket model";
            case Planning: return "The planning wall";
            case Launch: return "The launch button";
        }
        return "Help";
    }

    public static string[] Bullets(string id)
    {
        switch (id)
        {
            case Folder: return new string[]
            {
                "Page 1 is your <b>briefing</b>: where you are going, what the mission needs and the latest mission updates.",
                "The <b>sticky notes</b> are your checklist: they show which objectives are done and what is still missing.",
                "Page 2 is the <b>crew</b>: pick a pilot, an engineer and a scientist, then share <b>6 training points</b> among them.",
                "Training someone too hard makes them <b>Tired</b> (worse in flight). Traits change the team, and in flight only the skill of each astronaut's <b>role</b> counts.",
            };
            case SpaceMart: return new string[]
            {
                "Buy parts for your rocket and spacecraft with your mission <b>budget</b> (your credits are shown in the shop).",
                "A small <b>red dot</b> marks a part type you still need: the rocket cannot fly without it.",
                "Changed your mind? <b>Sell</b> a part to get your credits back.",
                "Then click the rocket model on the desk to assemble what you bought.",
            };
            case Rocket: return new string[]
            {
                "Click a bought part to <b>mount</b> it on the rocket, or click a mounted part to take it off.",
                "<b>Delta-v</b> is how much your spacecraft can change its speed. More fuel gives more delta-v, but also more <b>mass</b> to carry.",
                "When every required part is on and the numbers are enough for the mission, you see <b>\"Ready to fly!\"</b>.",
            };
            case Planning: return new string[]
            {
                "Pick a <b>launch site</b> on the map. Sites near the <b>equator</b> get a free speed boost from Earth's spin, so the launcher needs less fuel.",
                "Then pick a <b>day</b> in the calendar. Each day shows its weather: choose the one with the <b>lowest risk</b>.",
                "Bad weather on launch day can cause delays and problems in flight.",
            };
            case Launch: return new string[]
            {
                "The launch button unlocks when all <b>4 objectives</b> are done: mission read, crew trained, rocket ready, site and date chosen.",
                "The checklist next to it tells you what is still missing.",
                "You can launch early, but it is risky: whatever is missing hurts the flight and your score.",
            };
        }
        return new string[0];
    }
}
