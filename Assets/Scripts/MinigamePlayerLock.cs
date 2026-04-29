using UnityEngine;

public class MinigamePlayerLock : MonoBehaviour
{
    [SerializeField] private Transform minigamePosition;
    [SerializeField] private MonoBehaviour fpsController;

    void Start()
    {
        transform.position = minigamePosition.position;
        transform.rotation = minigamePosition.rotation;

        if (fpsController != null)
        {
            fpsController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}