using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    public void LoadCafeScene()
    {
        SceneManager.LoadScene("Cafe");
    }
}