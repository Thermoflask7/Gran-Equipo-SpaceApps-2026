using UnityEngine;
using UnityEngine.UI;

// Ponlo en un Button. Si targetScene esta vacio, va a la siguiente escena del loop.
[RequireComponent(typeof(Button))]
public class NextSceneButton : MonoBehaviour
{
    public string targetScene = "";

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Go);
    }

    void Go()
    {
        if (targetScene == "") GameManager.Instance.GoToNextScene();
        else GameManager.Instance.GoToScene(targetScene);
    }
}
