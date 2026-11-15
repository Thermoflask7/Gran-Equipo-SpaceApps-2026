// Exact scene names and their order in the loop (see GDD.md).
// Use these constants instead of typing the names by hand.
// The Spanish scene names will be renamed at the end (task 23); until then do not change them.
public static class Scenes
{
    public const string Menu = "00_Menu";
    public const string Planning = "01_Planeacion";
    public const string Launch = "02_Lanzamiento";
    public const string Results = "03_Resultados";

    public static readonly string[] Order = { Menu, Planning, Launch, Results };
}
