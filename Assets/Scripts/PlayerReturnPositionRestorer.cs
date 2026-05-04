using UnityEngine;

public class PlayerReturnPositionRestorer : MonoBehaviour
{
    private void Start()
    {
        if (!SceneReturnData.HasReturnPosition)
        {
            return;
        }

        CharacterController controller = GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = SceneReturnData.GetReturnPosition();
        transform.rotation = SceneReturnData.GetReturnRotation();

        if (controller != null)
        {
            controller.enabled = true;
        }

        SceneReturnData.Clear();
    }
}