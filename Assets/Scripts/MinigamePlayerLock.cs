using UnityEngine;

public class MinigamePlayerLock : MonoBehaviour
{
    //disable lock
    [SerializeField] private MonoBehaviour fpsController;
    [SerializeField] private MonoBehaviour fpsInteract;

    private void Start()
    {
        if (fpsController == null)
        {
            fpsController = GetComponent<SimpleFPSController>();
        }

        if (fpsController != null)
        {
            fpsController.enabled = false;
        }

        if (fpsInteract == null)
        {
            fpsInteract = GetComponent<FPSInteract>();
        }

        if (fpsInteract != null)
        {
            fpsInteract.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}