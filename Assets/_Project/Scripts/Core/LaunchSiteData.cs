using UnityEngine;

// Sitio de lanzamiento que se elige en el globo terraqueo. Los valores reales los llena P5.
[CreateAssetMenu(menuName = "SpaceApps/Sitio de lanzamiento", fileName = "NuevoSitio")]
public class LaunchSiteData : ScriptableObject
{
    public string siteName;
    [TextArea] public string description;
    public float latitude;
    public float longitude;

    // Riesgo de mal clima por dia del calendario (0 = despejado, 1 = tormenta).
    // El indice es el dia que se elige en el calendario.
    [Range(0f, 1f)] public float[] weatherRiskByDay = new float[14];
}
