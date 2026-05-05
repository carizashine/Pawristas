//not needed anymore

using UnityEngine;

public class EspressoPuckDraggable : MonoBehaviour
{
    [Header("References")]
    public Camera dragCamera;
    public EspressoDropZone dropZone;

    [Header("Drag Settings")]
    public float dragSmoothness = 25f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Vector3 targetPosition;
    private Vector3 offset;
    private float screenDepth;

    private bool isDragging;
    private Collider puckCollider;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        puckCollider = GetComponent<Collider>();

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
        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == puckCollider || hit.collider.transform.IsChildOf(transform))
            {
                isDragging = true;

                screenDepth = dragCamera.WorldToScreenPoint(transform.position).z;

                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                offset = transform.position - mouseWorldPosition;

                targetPosition = transform.position;

                Debug.Log("Started dragging espresso puck.");
            }
        }
    }

    private void UpdateTargetPosition()
    {
        targetPosition = GetMouseWorldPosition() + offset;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = screenDepth;

        return dragCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

    private void StopDrag()
    {
        isDragging = false;

        if (dropZone != null && dropZone.IsPuckInside(puckCollider))
        {
            bool success = dropZone.ReceivePuck(this);

            if (success)
            {
                return;
            }
        }

        ResetPuck();
    }

    public void ResetPuck()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}