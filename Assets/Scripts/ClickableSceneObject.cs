using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickableSceneObject : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName;

    [Header("Return Position")]
    [SerializeField] private bool savePlayerReturnPosition = true;

    public void LoadScene()
    {
        if (savePlayerReturnPosition)
        {
            SavePlayerPosition();
        }

        SceneManager.LoadScene(sceneName);
    }

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