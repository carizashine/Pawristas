using UnityEngine;
using UnityEngine.EventSystems;

// Allows cafe items to be draggable with user input
public class CafeCupDraggable : MonoBehaviour
{
    public static bool IsDraggingAnyCafeItem { get; private set; }

    [Header("Item")]
    // Tracks current state of cafe item to see if its getting dragged or not
    public CafeItemType itemType = CafeItemType.Espresso;

    [Header("References")]
    public Camera dragCamera;
    public MonoBehaviour fpsController;
    public CafeCounterDropZone dropZone;

    [Tooltip("Optional. If set, dropping the espresso cup near it triggers the syrup minigame.")]
    public SyrupStationDropZone syrupStation;

    [Header("Drag Settings")]
    public float dragSmoothness = 8f;

    [Header("Player Movement")]
    public bool disablePlayerMovementWhileDragging = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    // Current drag and offset data
    private Vector3 targetPosition;
    private Vector3 offset;
    private float screenDepth;

    // Variables for current state
    private bool isDragging;
    private bool isPlaced;

    private int activeTouchId = -1;

    private Collider cupCollider;

    [SerializeField] private AudioSource pickupAudio;

    // Save starting positions and rotations for items to move
    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        cupCollider = GetComponent<Collider>();

        if (dragCamera == null)
        {
            dragCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (dragCamera == null)
        {
            dragCamera = Camera.main;

            if (dragCamera == null)
            {
                return;
            }
        }

        // Touch controls for mobile and pc
        #if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
                HandleTouchInput();
        #else
                HandleMouseInput();
        #endif
    }

    private void LateUpdate()
    {
        if (isDragging)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                dragSmoothness * Time.deltaTime
            );
        }
    }

    // Handle drag behaviors for mouse input
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag(Input.mousePosition, -1, false);
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateTargetPosition(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            StopDrag(Input.mousePosition, false);
        }
    }

    // Handles drag behavior for mobile
    private void HandleTouchInput()
    {
        if (isDragging)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.fingerId != activeTouchId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (touch.phase == TouchPhase.Moved &&
                        SimpleFPSController.Instance != null)
                    {
                        SimpleFPSController.Instance.ApplyMobileLookDelta(touch.deltaPosition);
                    }

                    UpdateTargetPosition(touch.position);
                    return;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    StopDrag(touch.position, true);
                    return;
                }
            }

            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase != TouchPhase.Began)
            {
                continue;
            }

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                continue;
            }

            if (TryStartDrag(touch.position, touch.fingerId, true))
            {
                return;
            }
        }
    }

    // Tracks dragging items from a mouse or touch position
    private bool TryStartDrag(Vector2 screenPosition, int touchId, bool isMobileTouch)
    {
        if (isPlaced)
        {
            return false;
        }

        if (cupCollider == null)
        {
            return false;
        }

        Ray ray = dragCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Start dragging only if the ray hit this object
            if (hit.collider == cupCollider || hit.collider.transform.IsChildOf(transform))
            {
                isDragging = true;
                IsDraggingAnyCafeItem = true;
                activeTouchId = touchId;

                if (pickupAudio != null)
                {
                    pickupAudio.Play();
                }

                if (!isMobileTouch &&
                    disablePlayerMovementWhileDragging &&
                    fpsController != null)
                {
                    fpsController.enabled = false;
                }

                screenDepth = dragCamera.WorldToScreenPoint(transform.position).z;

                Vector3 pointerWorldPosition = GetPointerWorldPosition(screenPosition);
                offset = transform.position - pointerWorldPosition;

                targetPosition = transform.position;

                Debug.Log("Started dragging cafe item: " + itemType);

                return true;
            }
        }

        return false;
    }

    // Updates where item should move while dragged
    private void UpdateTargetPosition(Vector2 screenPosition)
    {
        targetPosition = GetPointerWorldPosition(screenPosition) + offset;
    }

    private void StopDrag(Vector2 screenPosition, bool isMobileTouch)
    {
        isDragging = false;
        IsDraggingAnyCafeItem = false;
        activeTouchId = -1;

        // 1) Check the syrup station first (espresso cups only).
        bool syrupStationReceived = false;

        if (syrupStation != null &&
            itemType == CafeItemType.Espresso &&
            syrupStation.IsMouseOverDropZone(dragCamera, screenPosition))
        {
            syrupStation.ReceiveCup(this);
            syrupStationReceived = true;
        }

        // 2) Otherwise check the pickup counter.
        if (!syrupStationReceived)
        {
            if (dropZone != null &&
                dropZone.IsMouseOverDropZone(dragCamera, screenPosition, itemType))
            {
                dropZone.ReceiveCup(this);
            }
            else
            {
                Debug.Log(itemType + " was not dropped on a valid zone — resetting.");
                ResetCup();
            }
        }

        if (!isMobileTouch &&
            disablePlayerMovementWhileDragging &&
            fpsController != null)
        {
            fpsController.enabled = true;
        }
    }

    private Vector3 GetPointerWorldPosition(Vector2 screenPosition)
    {
        Vector3 pointerScreenPosition = screenPosition;
        pointerScreenPosition.z = screenDepth;

        return dragCamera.ScreenToWorldPoint(pointerScreenPosition);
    }

    // Send item back to original position
    private void ResetCup()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    public void SnapToCounter(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    public void MarkPlaced()
    {
        isPlaced = true;
        isDragging = false;
        IsDraggingAnyCafeItem = false;
        activeTouchId = -1;
    }

    public void SetPickupAudio(AudioSource audioSource)
    {
        pickupAudio = audioSource;
    }
}