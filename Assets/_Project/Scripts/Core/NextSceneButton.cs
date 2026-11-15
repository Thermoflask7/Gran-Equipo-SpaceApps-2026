using UnityEngine;
using UnityEngine.UI;

// Put it on a Button. If targetScene is empty, it goes to the next scene in the loop.
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
