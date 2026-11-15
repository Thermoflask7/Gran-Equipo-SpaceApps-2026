using System.Collections.Generic;

// Rocket model logic (task 9), without UI. Signed: claude_c.
// SpaceMart fills gm.boughtParts; the rocket model moves bought parts onto the rocket
// (gm.rocketParts) and back. After every change gm.rocketValid is recalculated with RocketPhysics.
// The rocket model prefab (P2) only calls these methods and redraws itself.
// Rules: only bought parts can be mounted, one unit of each part (RocketPhysics.MaxCopiesPerPart),
// and only one SLS: mounting a second launcher swaps the one on the rocket.
public static class RocketAssembly
{
    // Parts every rocket needs: one SLS and the whole Orion
    public static readonly PartType[] RequiredTypes =
    {
        PartType.LaunchService, PartType.Capsule, PartType.ServiceModule, PartType.AbortSystem
    };

    // ---------- Queries ----------

    public static bool IsMounted(GameManager gm, PartData part)
    {
        return part != null && gm.rocketParts.Contains(part);
    }

    // "" if the part can be mounted now, otherwise the reason (for the UI)
    public static string MountProblem(GameManager gm, PartData part)
    {
        if (part == null) return "No part selected.";
        if (!gm.boughtParts.Contains(part)) return part.partName + " has not been bought. Buy it in SpaceMart first.";
        if (IsMounted(gm, part)) return part.partName + " is already on the rocket.";
        return "";
    }

    public static bool CanMount(GameManager gm, PartData part)
    {
        return MountProblem(gm, part) == "";
    }

    // Bought parts that are not on the rocket yet (the model's "parts tray")
    public static List<PartData> UnmountedParts(GameManager gm)
    {
        List<PartData> list = new List<PartData>();
        foreach (PartData p in gm.boughtParts)
            if (p != null && !gm.rocketParts.Contains(p) && !list.Contains(p)) list.Add(p);
        return list;
    }

    // Required part types still missing on the rocket (empty = SLS and Orion complete)
    public static List<PartType> MissingRequired(GameManager gm)
    {
        List<PartType> missing = new List<PartType>();
        foreach (PartType type in RequiredTypes)
        {
            bool found = false;
            foreach (PartData p in gm.rocketParts)
                if (p != null && p.type == type) found = true;
            if (!found) missing.Add(type);
        }
        return missing;
    }

    // "" if the assembled rocket can fly the mission, otherwise the first problem
    public static string Status(GameManager gm)
    {
        return RocketPhysics.ValidationMessage(gm.rocketParts, gm.mission);
    }

    // ---------- Changes (each one recalculates gm.rocketValid) ----------

    // Puts a bought part on the rocket. A second SLS replaces the one already mounted.
    public static bool Mount(GameManager gm, PartData part)
    {
        if (!CanMount(gm, part)) return false;
        if (part.type == PartType.LaunchService)
        {
            PartData oldLauncher = RocketPhysics.LaunchService(gm.rocketParts);
            if (oldLauncher != null) gm.rocketParts.Remove(oldLauncher);
        }
        gm.rocketParts.Add(part);
        UpdateValid(gm);
        return true;
    }

    // Takes a part off the rocket (it stays bought)
    public static bool Unmount(GameManager gm, PartData part)
    {
        if (part == null || !gm.rocketParts.Remove(part)) return false;
        UpdateValid(gm);
        return true;
    }

    // Mounts every bought part that fits (for an "Assemble all" button). Returns how many were added.
    // With several SLS bought, only the first one is mounted.
    public static int MountAll(GameManager gm)
    {
        int added = 0;
        foreach (PartData p in UnmountedParts(gm))
        {
            if (p.type == PartType.LaunchService && RocketPhysics.LaunchService(gm.rocketParts) != null) continue;
            gm.rocketParts.Add(p);
            added++;
        }
        UpdateValid(gm);
        return added;
    }

    // Takes everything off the rocket
    public static void Clear(GameManager gm)
    {
        gm.rocketParts.Clear();
        UpdateValid(gm);
    }

    // Removes parts that are no longer bought, null entries and duplicates, then recalculates
    // rocketValid. Call it when the model opens, in case something changed while it was closed.
    public static void Sync(GameManager gm)
    {
        List<PartData> clean = new List<PartData>();
        PartData launcher = null;
        foreach (PartData p in gm.rocketParts)
        {
            if (p == null || clean.Contains(p) || !gm.boughtParts.Contains(p)) continue;
            if (p.type == PartType.LaunchService)
            {
                if (launcher != null) continue;
                launcher = p;
            }
            clean.Add(p);
        }
        gm.rocketParts.Clear();
        gm.rocketParts.AddRange(clean);
        UpdateValid(gm);
    }

    public static void UpdateValid(GameManager gm)
    {
        gm.rocketValid = RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission);
    }
}
