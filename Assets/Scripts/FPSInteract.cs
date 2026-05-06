using UnityEngine;
using UnityEngine.EventSystems;

public class FPSInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Input")]
    [SerializeField] private KeyCode keyboardInteractKey = KeyCode.E;
    [SerializeField] private bool allowMouseClickInteract = true;
    [SerializeField] private bool allowTouchInteract = true;

    [Header("Mobile Protection")]
    [Tooltip("Prevents immediately re-clicking the fridge/machine after returning from a minigame.")]
    [SerializeField] private float interactCooldownAfterEnable = 0.35f;

    [Tooltip("On mobile, ignore touches on the left side where the D-pad usually is.")]
    [SerializeField] private bool ignoreLeftSideTouches = true;

    [Range(0f, 1f)]
    [SerializeField] private float leftSideIgnorePercent = 0.45f;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;

    private IInteractable currentInteractable;
    private float canInteractTime;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        canInteractTime = Time.time + interactCooldownAfterEnable;
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                HidePrompt();
                return;
            }
        }

        CheckForInteractable();

        if (PressedInteract())
        {
            TryInteract();
        }
    }

    private void CheckForInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();

            if (currentInteractable != null)
            {
                ShowPrompt(currentInteractable.GetPromptText());
                return;
            }
        }

        HidePrompt();
    }

    private bool PressedInteract()
    {
        if (Time.time < canInteractTime)
        {
            return false;
        }

        if (Input.GetKeyDown(keyboardInteractKey))
        {
            return true;
        }

#if UNITY_IOS || UNITY_ANDROID
        if (!allowTouchInteract)
        {
            return false;
        }

        if (Input.touchCount == 0)
        {
            return false;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
        {
            return false;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return false;
        }

        if (ignoreLeftSideTouches &&
            touch.position.x < Screen.width * leftSideIgnorePercent)
        {
            return false;
        }

        return true;
#else
        if (!allowMouseClickInteract)
        {
            return false;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return false;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        return true;
#endif
    }

    private void TryInteract()
    {
        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.Interact();

        // Prevent double-triggering if the tap/click is still being processed.
        canInteractTime = Time.time + 0.15f;
    }

    private void ShowPrompt(string message)
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    private void HidePrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.text = "";
        }
    }
}