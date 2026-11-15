using UnityEngine;

// Mission failure, abort and recovery rules (task 31, claude_c). Pure logic: no scene, no MonoBehaviour,
// so every rule is tested in edit mode (SpaceApps -> Mission failure -> Tests). FlightPhases applies it.
//
// Three levels:
//   A Minor    -> the flight continues; existing points, money and lesson apply.
//   B Serious  -> its minigame decides: a non-critical result resolves it; a critical result gets
//                 settings.seriousRetries more attempts; only if they are also critical the mission aborts.
//   C Critical -> a critical result ends the mission (departure abort, lunar orbit abort or failed return).
// Science is never a flight-safety matter: a weak research result never aborts; the SCIENCE and
// NO_DOWNLINK gates of MissionScoring decide whether the objectives were met.
// A minigame that does not exist yet (no handler) is skipped: it never causes a failure by itself.
public static class MissionFailureRules
{
    public const string DepartureFailure = "DEPARTURE_FAILURE";
    public const string LunarOrbitFailure = "LUNAR_ORBIT_FAILURE";
    public const string ReturnFailure = "RETURN_FAILURE";
    public const string AbortGate = "MISSION_ABORT";

    public enum Band { Critical, Poor, Acceptable, Good, Excellent }
    public enum Decision { Continue, Retry, Fail }

    // Result of judging one minigame
    public struct Outcome
    {
        public Decision decision;
        public Band band;
        public float effective;    // raw x skill, only for the threshold
        public string code;        // failure code when decision == Fail
        public string message;     // log / Results text ("" = nothing to say)
    }

    // ---------- Building blocks ----------

    // raw x skill, both clamped to 0..1. NaN counts as 0.
    public static float Effective(float raw, float skill)
    {
        return Clean(raw) * Clean(skill);
    }

    public static float Clean(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
        return Mathf.Clamp01(value);
    }

    public static Band BandOf(float effective, MissionFailureSettings s)
    {
        if (s == null) s = MissionFailureSettings.Default;
        effective = Clean(effective);
        if (effective < s.criticalBelow) return Band.Critical;
        if (effective < s.poorBelow) return Band.Poor;
        if (effective < s.goodFrom) return Band.Acceptable;
        if (effective < s.excellentFrom) return Band.Good;
        return Band.Excellent;
    }

    // Severity of an event. Auto keeps the old meaning of every existing asset: bad + minigame = Serious, else Minor.
    public static EventSeverity SeverityOf(EventData e)
    {
        if (e == null) return EventSeverity.Minor;
        if (e.severity != EventSeverity.Auto) return e.severity;
        return !e.isGood && e.minigameRole != TrainingFocus.None ? EventSeverity.Serious : EventSeverity.Minor;
    }

    public static string FailureCodeFor(EventPhase phase)
    {
        if (phase == EventPhase.Departure) return DepartureFailure;
        if (phase == EventPhase.LunarOrbit) return LunarOrbitFailure;
        return ReturnFailure;
    }

    // ---------- Judging a minigame ----------

    // role/phase: the minigame and when it was played. raw: its result (0..1). skill: SkillMultiplier of the crew member.
    // incident: severity of the event that asked for it (Minor for planned minigames). attempt: 0 = first try.
    public static Outcome Judge(TrainingFocus role, EventPhase phase, float raw, float skill,
                                EventSeverity incident, int attempt, MissionFailureSettings s)
    {
        if (s == null) s = MissionFailureSettings.Default;
        Outcome o = new Outcome();
        o.effective = Effective(raw, skill);
        o.band = BandOf(o.effective, s);
        o.decision = Decision.Continue;
        o.code = "";
        o.message = "";

        // Science never aborts the flight (scientific success is graded by the SCIENCE / NO_DOWNLINK gates)
        if (role == TrainingFocus.Research || role == TrainingFocus.None)
        {
            if (o.band == Band.Critical || o.band == Band.Poor)
                o.message = "Weak research result: less science data, but the spacecraft is safe.";
            return o;
        }

        if (o.band != Band.Critical)
        {
            if (incident == EventSeverity.Serious || incident == EventSeverity.Critical)
                o.message = RoleWord(role) + " problem resolved" + (o.band == Band.Poor ? " with difficulty." : ".");
            else if (o.band == Band.Poor)
                o.message = "Poor " + RoleWord(role).ToLower() + " performance: the flight continues with fewer points.";
            return o;
        }

        // Critical result: does this action decide the safety of the flight?
        int retries = -1; // -1 = not safety-critical
        if (incident == EventSeverity.Critical) retries = s.criticalRetries;
        else if (incident == EventSeverity.Serious) retries = s.seriousRetries;
        else if (role == TrainingFocus.Piloting && IsCriticalPilotingPhase(phase, s)) retries = 0;

        if (retries < 0)
        {
            o.message = "Very poor " + RoleWord(role).ToLower() + " performance, but it was not safety-critical: the flight continues.";
            return o;
        }
        if (attempt < retries)
        {
            o.decision = Decision.Retry;
            o.message = RoleWord(role) + " problem not resolved yet: the crew tries again.";
            return o;
        }

        o.decision = Decision.Fail;
        o.code = FailureCodeFor(phase);
        o.message = FailureMessage(role, phase);
        return o;
    }

    public static bool IsCriticalPilotingPhase(EventPhase phase, MissionFailureSettings s)
    {
        if (s == null) s = MissionFailureSettings.Default;
        if (phase == EventPhase.Departure) return s.departurePilotingIsCritical;
        if (phase == EventPhase.Return) return s.returnPilotingIsCritical;
        return false;
    }

    public static string FailureMessage(TrainingFocus role, EventPhase phase)
    {
        if (phase == EventPhase.Return)
            return "Return unsuccessful: the safe-return requirement was not satisfied.";
        if (role == TrainingFocus.Engineering)
            return "Critical maintenance issue: spacecraft systems were not restored in time. Mission aborted"
                + (phase == EventPhase.Departure ? " during departure." : " in lunar orbit.");
        if (phase == EventPhase.Departure)
            return "Departure aborted: the required flight-control performance was not achieved.";
        return "Lunar orbit aborted: a critical problem could not be handled.";
    }

    // Departure check before liftoff. "" = OK. Only used in a real game (flight.launched); a scene tested
    // on its own has test values and is not judged. Training cannot compensate for an invalid spacecraft.
    public static string PreflightProblem(GameManager gm)
    {
        if (gm == null || gm.flight == null || !gm.flight.launched) return "";
        if (gm.mission == null) return "Departure aborted: there is no mission loaded.";
        if (!RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission))
            return "Departure aborted: the spacecraft configuration is not valid for this mission ("
                + RocketPhysics.ValidationMessage(gm.rocketParts, gm.mission) + ").";
        return "";
    }

    // ---------- Recording ----------

    // Records a terminal failure ONCE. Returns false (and changes nothing) if the flight had already failed.
    // The failed phase and every later milestone are cleared; the flight is finished.
    // Departure / lunar orbit = abort (ended early). Return = failed return (not an abort).
    public static bool RecordFailure(MissionResults f, EventPhase phase, string code, string reason)
    {
        if (f == null || f.HasFailed) return false;
        if (string.IsNullOrEmpty(code)) code = FailureCodeFor(phase);
        f.failureCode = code;
        f.failureReason = reason ?? "";
        f.failurePhase = phase;
        f.aborted = phase == EventPhase.Departure || phase == EventPhase.LunarOrbit || phase == EventPhase.Planning;
        if (phase == EventPhase.Departure || phase == EventPhase.Planning) f.departureDone = false;
        if (phase != EventPhase.Return) f.lunarOrbitDone = false;
        f.safeReturnDone = false;
        f.flightFinished = true;
        return true;
    }

    static string RoleWord(TrainingFocus role)
    {
        if (role == TrainingFocus.Piloting) return "Piloting";
        if (role == TrainingFocus.Engineering) return "Maintenance";
        return "Research";
    }
}
