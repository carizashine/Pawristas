using UnityEngine;
using UnityEngine.EventSystems;

public class CafeCupDraggable : MonoBehaviour
{
    public static bool IsDraggingAnyCafeItem { get; private set; }

    [Header("Item")]
    public CafeItemType itemType = CafeItemType.Espresso;

    [Header("References")]
    public Camera dragCamera;
    public MonoBehaviour fpsController;
    public CafeCounterDropZone dropZone;

    [Tooltip("Optional. If set, dropping the espresso cup near it triggers the syrup minigame.")]
    public SyrupStationDropZone syrupStation;

    [Header("Drag Settings")]
    public float dragSmoothness = 10f;

    [Header("Collision While Dragging")]
    [SerializeField] private bool collideWhileDragging = true;
    [SerializeField] private float collisionSkin = 0.03f;

    [Header("Player Movement")]
    public bool disablePlayerMovementWhileDragging = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Vector3 targetPosition;
    private Vector3 offset;
    private float screenDepth;

    private bool isDragging;
    private bool isPlaced;

    private int activeTouchId = -1;

    private Collider cupCollider;
    private Rigidbody cupRigidbody;

    [SerializeField] private AudioSource pickupAudio;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        cupCollider = GetComponent<Collider>();
        cupRigidbody = GetComponent<Rigidbody>();

        if (dragCamera == null)
        {
            dragCamera = Camera.main;
        }

        if (cupCollider == null)
        {
            Debug.LogWarning(name + " needs a Collider to be draggable and collide.");
        }

        SetupRigidbodyForDragging();
    }

    private void SetupRigidbodyForDragging()
    {
        if (!collideWhileDragging)
        {
            return;
        }

        if (cupRigidbody == null)
        {
            cupRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        cupRigidbody.useGravity = false;
        cupRigidbody.isKinematic = true;
        cupRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        cupRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        if (cupCollider != null && cupCollider.isTrigger)
        {
            Debug.LogWarning(name + " collider is set to Is Trigger. Uncheck Is Trigger if you want it to collide while dragging.");
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

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        HandleTouchInput();
#else
        HandleMouseInput();
#endif
    }

    private void LateUpdate()
    {
        if (!isDragging)
        {
            return;
        }

        Vector3 desiredPosition = Vector3.Lerp(
            transform.position,
            targetPosition,
            dragSmoothness * Time.deltaTime
        );

        MoveWithCollision(desiredPosition);
    }

    private void MoveWithCollision(Vector3 desiredPosition)
    {
        if (!collideWhileDragging || cupRigidbody == null || cupCollider == null || cupCollider.isTrigger)
        {
            transform.position = desiredPosition;
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 delta = desiredPosition - currentPosition;

        float distance = delta.magnitude;

        if (distance <= 0.0001f)
        {
            return;
        }

        Vector3 direction = delta / distance;

        bool hitSomething = cupRigidbody.SweepTest(
            direction,
            out RaycastHit hit,
            distance + collisionSkin,
            QueryTriggerInteraction.Ignore
        );

        if (hitSomething)
        {
            float safeDistance = Mathf.Max(0f, hit.distance - collisionSkin);
            Vector3 safePosition = currentPosition + direction * safeDistance;

            cupRigidbody.position = safePosition;
            transform.position = safePosition;
            return;
        }

        cupRigidbody.position = desiredPosition;
        transform.position = desiredPosition;
    }

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

    private void UpdateTargetPosition(Vector2 screenPosition)
    {
        targetPosition = GetPointerWorldPosition(screenPosition) + offset;
    }

    private void StopDrag(Vector2 screenPosition, bool isMobileTouch)
    {
        isDragging = false;
        IsDraggingAnyCafeItem = false;
        activeTouchId = -1;

        bool syrupStationReceived = false;

        if (syrupStation != null &&
            itemType == CafeItemType.Espresso &&
            syrupStation.IsMouseOverDropZone(dragCamera, screenPosition))
        {
            syrupStation.ReceiveCup(this);
            syrupStationReceived = true;
        }

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

    private void ResetCup()
    {
        if (cupRigidbody != null)
        {
            cupRigidbody.position = startPosition;
            cupRigidbody.rotation = startRotation;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    public void SnapToCounter(Vector3 position, Quaternion rotation)
    {
        if (cupRigidbody != null)
        {
            cupRigidbody.position = position;
            cupRigidbody.rotation = rotation;
        }

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