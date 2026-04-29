using UnityEngine;
using UnityEngine.SceneManagement;

public class FPSInteract : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left click / tap
        {
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                ClickableSceneObject clickable = hit.collider.GetComponent<ClickableSceneObject>();

                if (clickable != null)
                {
                    clickable.LoadScene();
                }
            }
        }
    }
}