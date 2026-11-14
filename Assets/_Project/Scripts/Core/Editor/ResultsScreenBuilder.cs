using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Menus de Unity para la pantalla de Resultados (tarea 16):
//   SpaceApps > Pantalla de resultados > Agregar a la escena abierta
//       Pone un panel con titulo, puntaje, desglose y boton "Volver al menu" en el Canvas
//       de la escena abierta (03_Resultados o tu Sandbox). Despues guarda la escena.
//   SpaceApps > Pantalla de resultados > Prueba: partida de ejemplo
//       Solo en Play: llena el GameManager con una partida completa (catalogo Artemis)
//       y vuelve a calcular la pantalla.
public static class ResultsScreenBuilder
{
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f, 0.95f);
    static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.85f);

    [MenuItem("SpaceApps/Pantalla de resultados/Agregar a la escena abierta")]
    static void AddToScene()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Pantalla de resultados", "La escena abierta no tiene Canvas.", "OK");
            return;
        }

        // Panel centrado
        GameObject root = new GameObject("PantallaResultados", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1500, 950);
        root.GetComponent<Image>().color = BoxColor;

        TextMeshProUGUI title = CreateText(root.transform, "Titulo", "Resultados", 60);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0, -60), new Vector2(1400, 80));

        TextMeshProUGUI score = CreateText(root.transform, "Puntaje", "0 / 100", 90);
        score.fontStyle = FontStyles.Bold;
        Place(score.rectTransform, new Vector2(0, -160), new Vector2(1400, 110));

        TextMeshProUGUI report = CreateText(root.transform, "Desglose", "", 26);
        report.alignment = TextAlignmentOptions.TopLeft;
        report.textWrappingMode = TextWrappingModes.Normal;
        report.overflowMode = TextOverflowModes.Ellipsis;
        Place(report.rectTransform, new Vector2(0, -520), new Vector2(1400, 560));

        GameObject buttonGo = new GameObject("BotonMenu", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root.transform, false);
        Place((RectTransform)buttonGo.transform, new Vector2(0, -875), new Vector2(420, 90));
        Image img = buttonGo.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = ButtonColor;
        TextMeshProUGUI label = CreateText(buttonGo.transform, "Texto", "Volver al menu", 36);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        buttonGo.AddComponent<NextSceneButton>(); // despues de Resultados reinicia y va al menu

        ResultsScreen screen = root.AddComponent<ResultsScreen>();
        screen.title = title;
        screen.score = score;
        screen.report = report;

        Undo.RegisterCreatedObjectUndo(root, "Agregar pantalla de resultados");
        Selection.activeGameObject = root;
    }

    [MenuItem("SpaceApps/Pantalla de resultados/Prueba: partida de ejemplo")]
    static void FakeGame()
    {
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (mission == null)
        {
            Debug.LogError("Falta la mision: corre SpaceApps -> Importar catalogo Artemis.");
            return;
        }
        MissionScoringTests.FillExampleGame(GameManager.Instance, mission);
        ResultsScreen screen = Object.FindAnyObjectByType<ResultsScreen>();
        if (screen != null) screen.Show();
        Debug.Log("Prueba: partida de ejemplo cargada. Puntaje = " + GameManager.Instance.TotalScore().ToString("0.00"));
    }

    [MenuItem("SpaceApps/Pantalla de resultados/Prueba: partida de ejemplo", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    // Posicion relativa al borde de arriba del panel
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
