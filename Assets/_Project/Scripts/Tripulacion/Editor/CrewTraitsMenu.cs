using UnityEditor;
using UnityEngine;

// Sets the trait of each astronaut asset (Data/Astronautas), so nobody edits the .asset files by hand.
// Menu: SpaceApps -> Mission folder -> Assign astronaut traits
public static class CrewTraitsMenu
{
    const string Folder = "Assets/_Project/Data/Astronautas/";

    // Asset name -> trait. Two of each, spread so the "best on paper" crew is not automatically the best.
    static readonly string[] Ids = { "AST-P01", "AST-P02", "AST-P03", "AST-E01", "AST-E02", "AST-E03", "AST-R01", "AST-R02", "AST-R03", "AST-M01" };
    static readonly CrewTrait[] Traits =
    {
        CrewTrait.Loner, CrewTrait.TeamPlayer, CrewTrait.HardWorker,
        CrewTrait.Veteran, CrewTrait.HardWorker, CrewTrait.Rookie,
        CrewTrait.Veteran, CrewTrait.Loner, CrewTrait.Rookie,
        CrewTrait.TeamPlayer
    };

    [MenuItem("SpaceApps/Mission folder/Assign astronaut traits")]
    public static void Assign()
    {
        int count = 0;
        for (int i = 0; i < Ids.Length; i++)
        {
            AstronautData a = AssetDatabase.LoadAssetAtPath<AstronautData>(Folder + Ids[i] + ".asset");
            if (a == null) { Debug.LogWarning("Crew traits: missing " + Ids[i]); continue; }
            a.trait = Traits[i];
            EditorUtility.SetDirty(a);
            count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Crew traits: assigned " + count + " astronauts.");
    }
}
