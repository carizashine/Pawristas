using UnityEngine;
using UnityEngine.SceneManagement;

// Load scene
public class SimpleSceneLoader : MonoBehaviour
{
    public void LoadCafeScene()
    {
        SceneManager.LoadScene("Cafe");
    }
}