using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

// Handles first person interaction and lets player use mobile and pc inputs
public class FPSInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool allowMouseClick = true;

    [Header("Mobile Touch Interaction")]
    [SerializeField] private bool allowTouchInteract = true;

    [Tooltip("Prevents instantly re-entering a minigame after returning to the cafe.")]
    [SerializeField] private float mobileInteractCooldownAfterEnable = 0.75f;

    [Tooltip("Maximum time a mobile touch can last and still count as a tap.")]
    [SerializeField] private float maxTapDuration = 0.3f;

    [Tooltip("Maximum finger movement in pixels before the touch is treated as a drag/look swipe instead of a tap.")]
    [SerializeField] private float maxTapMoveDistance = 35f;

    [Header("Search")]
    [Tooltip("Search this many parent levels for interact scripts or LoadScene scripts.")]
    [SerializeField] private int parentSearchLevels = 5;

    [Tooltip("Also search children of the hit object/parents. Useful for imported models.")]
    [SerializeField] private bool searchChildren = true;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private IInteractable currentInteractable;

    private MonoBehaviour currentLoadSceneComponent;
    private MethodInfo currentLoadSceneMethod;

    private float canInteractTime;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
    private int candidateTouchId = -1;
    private Vector2 candidateTouchStartPosition;
    private float candidateTouchStartTime;
    private bool candidateTouchStartedOverUI;
#endif

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        canInteractTime = Time.time + mobileInteractCooldownAfterEnable;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        ResetMobileTapCandidate();
#endif
    }

    private void Start()
    {
        canInteractTime = Time.time + mobileInteractCooldownAfterEnable;
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                ClearCurrentTarget();
                HidePrompt();
                return;
            }
        }

        CheckForTarget();

        if (PressedInteract())
        {
            TryInteract();
        }
    }

    // Casts a ray to check whether the player is looking at an interactable
    private void CheckForTarget()
    {
        ClearCurrentTarget();

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            HidePrompt();
            return;
        }

        if (showDebugLogs)
        {
            // Debug.Log("FPSInteract ray hit: " + hit.collider.name);
        }

        currentInteractable = FindInteractableFromHit(hit.collider);

        if (currentInteractable != null)
        {
            if (showDebugLogs)
            {
                // Debug.Log("FPSInteract found IInteractable: " + currentInteractable.GetType().Name);
            }

            ShowPrompt(currentInteractable.GetPromptText());
            return;
        }

        FindLoadSceneTargetFromHit(hit.collider);

        if (currentLoadSceneComponent != null && currentLoadSceneMethod != null)
        {
            if (showDebugLogs)
            {
                // Debug.Log("FPSInteract found LoadScene target: " + currentLoadSceneComponent.GetType().Name);
            }

            ShowPrompt("Click to interact");
            return;
        }

        if (showDebugLogs)
        {
            // Debug.Log("FPSInteract hit " + hit.collider.name + " but found no interact target.");
        }

        HidePrompt();
    }

    // Searches the hit object and its parents
    private IInteractable FindInteractableFromHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        Transform current = hitCollider.transform;

        for (int level = 0; level < parentSearchLevels && current != null; level++)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    return interactable;
                }
            }

            if (searchChildren)
            {
                MonoBehaviour[] childBehaviours = current.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour behaviour in childBehaviours)
                {
                    if (behaviour is IInteractable interactable)
                    {
                        return interactable;
                    }
                }
            }

            current = current.parent;
        }

        return null;
    }

    private void FindLoadSceneTargetFromHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return;
        }

        Transform current = hitCollider.transform;

        for (int level = 0; level < parentSearchLevels && current != null; level++)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (TrySetLoadSceneTarget(behaviour))
                {
                    return;
                }
            }

            if (searchChildren)
            {
                MonoBehaviour[] childBehaviours = current.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour behaviour in childBehaviours)
                {
                    if (TrySetLoadSceneTarget(behaviour))
                    {
                        return;
                    }
                }
            }

            current = current.parent;
        }
    }

    // Use reflection to check is component has a LoadScene method
    private bool TrySetLoadSceneTarget(MonoBehaviour behaviour)
    {
        if (behaviour == null)
        {
            return false;
        }

        MethodInfo method = behaviour.GetType().GetMethod(
            "LoadScene",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method == null)
        {
            return false;
        }

        if (method.GetParameters().Length != 0)
        {
            return false;
        }

        currentLoadSceneComponent = behaviour;
        currentLoadSceneMethod = method;
        return true;
    }

    // Check if user pressed interaction in the frame
    private bool PressedInteract()
    {
        if (Time.time < canInteractTime)
        {
            return false;
        }

        if (Input.GetKeyDown(interactKey))
        {
            return true;
        }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return PressedMobileTapInteract();
#else
        if (!allowMouseClick)
        {
            return false;
        }

        return Input.GetMouseButtonDown(0);
#endif
    }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
    private bool PressedMobileTapInteract()
    {
        if (!allowTouchInteract)
        {
            return false;
        }

        if (CafeCupDraggable.IsDraggingAnyCafeItem)
        {
            ResetMobileTapCandidate();
            return false;
        }

        if (Input.touchCount == 0)
        {
            ResetMobileTapCandidate();
            return false;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                bool startedOverUI =
                    EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId);

                if (startedOverUI)
                {
                    continue;
                }

                candidateTouchId = touch.fingerId;
                candidateTouchStartPosition = touch.position;
                candidateTouchStartTime = Time.time;
                candidateTouchStartedOverUI = false;

                return false;
            }

            if (touch.fingerId != candidateTouchId)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                float movedDistance = Vector2.Distance(
                    candidateTouchStartPosition,
                    touch.position
                );

                if (movedDistance > maxTapMoveDistance)
                {
                    ResetMobileTapCandidate();
                }

                return false;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                float duration = Time.time - candidateTouchStartTime;

                float movedDistance = Vector2.Distance(
                    candidateTouchStartPosition,
                    touch.position
                );

                bool isQuickTap =
                    !candidateTouchStartedOverUI &&
                    duration <= maxTapDuration &&
                    movedDistance <= maxTapMoveDistance;

                ResetMobileTapCandidate();

                if (isQuickTap)
                {
                    return true;
                }

                return false;
            }

            if (touch.phase == TouchPhase.Canceled)
            {
                ResetMobileTapCandidate();
                return false;
            }
        }

        return false;
    }

    private void ResetMobileTapCandidate()
    {
        candidateTouchId = -1;
        candidateTouchStartPosition = Vector2.zero;
        candidateTouchStartTime = 0f;
        candidateTouchStartedOverUI = false;
    }
#endif

    // Performs interaction with whichever valid target is currently selected
    private void TryInteract()
    {
        if (currentInteractable != null)
        {
            if (showDebugLogs)
            {
                Debug.Log("Interacting with IInteractable: " + currentInteractable.GetType().Name);
            }

            currentInteractable.Interact();

            canInteractTime = Time.time + 0.15f;
            return;
        }

        if (currentLoadSceneComponent != null && currentLoadSceneMethod != null)
        {
            if (showDebugLogs)
            {
                Debug.Log("Interacting with LoadScene target: " + currentLoadSceneComponent.GetType().Name);
            }

            currentLoadSceneMethod.Invoke(currentLoadSceneComponent, null);

            canInteractTime = Time.time + 0.15f;
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log("Tried to interact, but no target was found.");
        }
    }

    // Clears the corrently detected interaction target
    private void ClearCurrentTarget()
    {
        currentInteractable = null;
        currentLoadSceneComponent = null;
        currentLoadSceneMethod = null;
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