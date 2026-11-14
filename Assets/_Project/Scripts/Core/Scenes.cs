// Nombres exactos de las escenas y su orden en el loop.
// Usar estas constantes en vez de escribir los nombres a mano.
public static class Scenes
{
    public const string Menu = "00_Menu";
    public const string Budget = "01_Presupuesto";
    public const string Shop = "02_Tienda";
    public const string Crew = "03_Tripulacion";
    public const string Launch = "04_Lanzamiento";
    public const string Mission = "05_Mision";
    public const string Results = "06_Resultados";

    public static readonly string[] Order = { Menu, Budget, Shop, Crew, Launch, Mission, Results };
}
