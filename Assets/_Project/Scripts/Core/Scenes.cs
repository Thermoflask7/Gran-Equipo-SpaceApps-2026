// Nombres exactos de las escenas y su orden en el loop (ver GDD.md).
// Usar estas constantes en vez de escribir los nombres a mano.
public static class Scenes
{
    public const string Menu = "00_Menu";
    public const string Planning = "01_Planeacion";
    public const string Launch = "02_Lanzamiento";
    public const string Results = "03_Resultados";

    public static readonly string[] Order = { Menu, Planning, Launch, Results };
}
