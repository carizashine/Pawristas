using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickableSceneObject : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}