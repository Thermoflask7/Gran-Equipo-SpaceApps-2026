using System;
using UnityEngine;

// Every balance number and every step text of the piloting minigame (task 12), in one place.
// Asset: Assets/_Project/Data/Minigames/PilotingSettings.asset (the menu
// SpaceApps > Piloting minigame > Create prefab creates it if missing). You can also make one with
// right click > Create > SpaceApps > Piloting settings.
//
// The minigame is a sequence of timed commands (QTE). In each step a marker sweeps across a gauge once
// and the pilot presses FIRE (Space or the button) inside the green window.
[CreateAssetMenu(fileName = "PilotingSettings", menuName = "SpaceApps/Piloting settings")]
public class PilotingSettings : ScriptableObject
{
    [Serializable]
    public class PilotingStep
    {
        public string stepName = "Command";
        [TextArea] public string description = "";
    }

    [Header("Timing")]
    [Tooltip("Seconds the marker takes to cross the gauge in the first step")]
    public float sweepTime = 2.2f;
    [Tooltip("Each step the sweep is this much faster (0.08 = 8 % faster per step)")]
    public float speedUpPerStep = 0.08f;
    [Tooltip("Return is faster than departure (GDD): sweep speed multiplier for the Return phase")]
    public float returnSpeedMultiplier = 1.2f;
    [Tooltip("Seconds the step name is shown before the marker starts moving")]
    public float readyTime = 1.0f;
    [Tooltip("Seconds the step result (PERFECT, LATE...) is shown before the next step")]
    public float feedbackTime = 1.2f;

    [Header("Window (fractions of the gauge width)")]
    [Tooltip("Width of the green window in the first step")]
    [Range(0.05f, 0.6f)] public float windowWidth = 0.24f;
    [Tooltip("The window shrinks by this much each step")]
    [Range(0f, 0.1f)] public float shrinkPerStep = 0.025f;
    [Tooltip("The window never gets narrower than this")]
    [Range(0.03f, 0.5f)] public float minWindowWidth = 0.1f;
    [Tooltip("Inner part of the window (0 to 1) that counts as PERFECT")]
    [Range(0f, 1f)] public float perfectFraction = 0.35f;
    [Tooltip("Lowest and highest center of the window, so the pilot always has some lead time")]
    [Range(0f, 1f)] public float minWindowCenter = 0.35f;
    [Range(0f, 1f)] public float maxWindowCenter = 0.85f;

    [Header("Score per step (0 to 1)")]
    [Tooltip("Score at the edge of the window (PERFECT = 1, edge = this)")]
    [Range(0f, 1f)] public float edgeScore = 0.6f;
    [Tooltip("Score for a near miss: up to half a window outside it")]
    [Range(0f, 1f)] public float nearMissScore = 0.25f;

    [Header("Command sequences (real lunar mission milestones)")]
    public PilotingStep[] departureSteps =
    {
        new PilotingStep { stepName = "Throttle down for Max Q",
            description = "About 1 minute after liftoff the air pressure on the rocket peaks (Max Q). The engines throttle down to protect the structure." },
        new PilotingStep { stepName = "Booster separation",
            description = "About 2 minutes in, the two solid rocket boosters burn out and are released." },
        new PilotingStep { stepName = "Core stage cutoff (MECO)",
            description = "About 8 minutes in, the four core stage engines shut down: the rocket is almost in orbit." },
        new PilotingStep { stepName = "Core stage separation",
            description = "The empty core stage is released so the upper stage does not carry its dead weight." },
        new PilotingStep { stepName = "Translunar injection burn",
            description = "The upper stage fires to raise the spacecraft's speed to about 10 km/s and send it toward the Moon." },
    };

    public PilotingStep[] lunarOrbitSteps =
    {
        new PilotingStep { stepName = "Lunar orbit insertion",
            description = "The service module fires against the direction of travel so the Moon's gravity can capture it." },
        new PilotingStep { stepName = "Orbit trim burn",
            description = "A short burn fixes small errors so the orbit stays stable and safe." },
        new PilotingStep { stepName = "Trans-Earth injection",
            description = "The main engine fires again to leave lunar orbit and head home." },
    };

    public PilotingStep[] returnSteps =
    {
        new PilotingStep { stepName = "Service module separation",
            description = "Before reentry the crew module releases the service module, which burns up in the atmosphere." },
        new PilotingStep { stepName = "Entry interface",
            description = "The capsule hits the atmosphere at about 11 km/s. The heat shield faces forward and reaches about 2,800 °C." },
        new PilotingStep { stepName = "Drogue parachutes",
            description = "At about 7.6 km altitude, two drogue parachutes stabilize and slow the capsule." },
        new PilotingStep { stepName = "Main parachutes",
            description = "At about 2.9 km, three main parachutes slow the capsule to about 27 km/h for splashdown in the ocean." },
    };

    // The sequence for a flight phase
    public PilotingStep[] StepsFor(EventPhase phase)
    {
        switch (phase)
        {
            case EventPhase.LunarOrbit: return lunarOrbitSteps;
            case EventPhase.Return: return returnSteps;
            default: return departureSteps;
        }
    }

    // Window width for a step (0 = first step)
    public float WindowFor(int step)
    {
        return Mathf.Max(minWindowWidth, windowWidth - shrinkPerStep * step);
    }

    // Seconds the marker takes to cross the gauge in a step
    public float SweepFor(int step, EventPhase phase)
    {
        float speed = 1f + speedUpPerStep * step;
        if (phase == EventPhase.Return) speed *= returnSpeedMultiplier;
        return sweepTime / Mathf.Max(0.1f, speed);
    }

    // Score (0 to 1) of pressing at marker position "marker" for a window centered at "center" with width "width"
    public float ScoreFor(float marker, float center, float width)
    {
        float half = Mathf.Max(0.001f, width / 2f);
        float d = Mathf.Abs(marker - center) / half; // 0 = center, 1 = edge of the window
        if (d <= perfectFraction) return 1f;
        if (d <= 1f) return Mathf.Lerp(1f, edgeScore, (d - perfectFraction) / Mathf.Max(0.001f, 1f - perfectFraction));
        if (d <= 1.5f) return nearMissScore;
        return 0f;
    }
}
