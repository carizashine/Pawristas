using UnityEngine;
using TMPro;

public class FPSInteract : MonoBehaviour
{
    [Header("Interaction")]
    public float distance = 3f;
    public Camera cam;

    [Header("Optional UI")]
    public TextMeshProUGUI prompt;

    private IInteractable currentInteractable;

    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (prompt != null)
        {
            prompt.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        CheckForInteractable();

        if (Input.GetMouseButtonDown(0))
        {
            TryInteract();
        }
    }

    private void CheckForInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;

                if (prompt != null)
                {
                    prompt.gameObject.SetActive(true);
                    prompt.text = interactable.GetPromptText();
                }

                return;
            }
        }

        if (prompt != null)
        {
            prompt.gameObject.SetActive(false);
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact();
                return;
            }

            ClickableSceneObject clickable = hit.collider.GetComponent<ClickableSceneObject>();

            if (clickable != null)
            {
                clickable.LoadScene();
            }
        }
    }
}