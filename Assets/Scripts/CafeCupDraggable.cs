using UnityEngine;

public class CafeCupDraggable : MonoBehaviour
{
    [Header("Item")]
    public CafeItemType itemType = CafeItemType.Espresso;

    [Header("References")]
    public Camera dragCamera;
    public MonoBehaviour fpsController;
    public CafeCounterDropZone dropZone;

    [Tooltip("Optional. If set, dropping the espresso cup near it triggers the syrup minigame.")]
    public SyrupStationDropZone syrupStation;

    [Header("Drag Settings")]
    public float dragSmoothness = 25f;

    [Header("Player Movement")]
    public bool disablePlayerMovementWhileDragging = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Vector3 targetPosition;
    private Vector3 offset;
    private float screenDepth;

    private bool isDragging;
    private bool isPlaced;

    private Collider cupCollider;

    [SerializeField] private AudioSource pickupAudio;

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
            if (dragCamera == null) return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag();
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateTargetPosition();
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            StopDrag();
        }
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

    private void TryStartDrag()
    {
        if (isPlaced)
        {
            return;
        }

        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == cupCollider || hit.collider.transform.IsChildOf(transform))
            {
                isDragging = true;
                if (pickupAudio != null)
                {
                    pickupAudio.Play();
                }

                if (disablePlayerMovementWhileDragging && fpsController != null)
                {
                    fpsController.enabled = false;
                }

                screenDepth = dragCamera.WorldToScreenPoint(transform.position).z;

                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                offset = transform.position - mouseWorldPosition;

                targetPosition = transform.position;

                Debug.Log("Started dragging cafe item: " + itemType);
            }
        }
    }

    private void UpdateTargetPosition()
    {
        targetPosition = GetMouseWorldPosition() + offset;
    }

    private void StopDrag()
    {
        isDragging = false;

        // 1) Check the syrup station first (espresso cups only).
        bool syrupStationReceived = false;

        if (syrupStation != null &&
            itemType == CafeItemType.Espresso &&
            syrupStation.IsMouseOverDropZone(dragCamera, Input.mousePosition))
        {
            syrupStation.ReceiveCup(this);
            syrupStationReceived = true;
        }

        // 2) Otherwise check the pickup counter.
        if (!syrupStationReceived)
        {
            if (dropZone != null &&
                dropZone.IsMouseOverDropZone(dragCamera, Input.mousePosition, itemType))
            {
                dropZone.ReceiveCup(this);
            }
            else
            {
                Debug.Log(itemType + " was not dropped on a valid zone — resetting.");
                ResetCup();
            }
        }

        if (disablePlayerMovementWhileDragging && fpsController != null)
        {
            fpsController.enabled = true;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = screenDepth;

        return dragCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

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
    }
    public void SetPickupAudio(AudioSource audioSource)
    {
        pickupAudio = audioSource;
    }
}