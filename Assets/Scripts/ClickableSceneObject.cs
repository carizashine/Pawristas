using UnityEngine;
using UnityEngine.SceneManagement;

// Allows object in scene to load another scene when interacte dwith
public class ClickableSceneObject : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName;

    [Header("Return Position")]
    [SerializeField] private bool savePlayerReturnPosition = true;

    // Load target scene
    public void LoadScene()
    {
        if (savePlayerReturnPosition)
        {
            SavePlayerPosition();
        }

        SceneManager.LoadScene(sceneName);
    }

    // Finds user and states their current transform data
    private void SavePlayerPosition()
    {
        SimpleFPSController playerController = FindFirstObjectByType<SimpleFPSController>();

        if (playerController == null)
        {
            Debug.LogWarning("ClickableSceneObject: Could not find player to save return position.");
            return;
        }

        SceneReturnData.SaveReturnPosition(playerController.transform);
    }
}