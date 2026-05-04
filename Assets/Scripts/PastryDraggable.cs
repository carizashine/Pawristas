using UnityEngine;

public class PastryDraggable : MonoBehaviour
{
    [Header("Pastry")]
    public PastryType pastryType;

    [Header("References")]
    public Camera dragCamera;
    public PastryPlateDropZone dropZone;

    [Header("Drag Settings")]
    public float dragSmoothness = 25f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Vector3 targetPosition;
    private Vector3 offset;
    private float screenDepth;

    private bool isDragging;
    private Collider pastryCollider;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        pastryCollider = GetComponent<Collider>();

        if (dragCamera == null)
        {
            dragCamera = Camera.main;
        }

        if (pastryCollider == null)
        {
            Debug.LogWarning(name + " needs a Collider to be draggable.");
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

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            100f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            Debug.Log("Clicked, but ray hit nothing.");
            return;
        }

        foreach (RaycastHit hit in hits)
        {
            PastryDraggable hitPastry = hit.collider.GetComponentInParent<PastryDraggable>();

            if (hitPastry == this)
            {
                StartDragging();
                return;
            }
        }

        Debug.Log("Clicked, but did not hit this pastry. First hit was: " + hits[0].collider.name);
    }

    private void StartDragging()
    {
        isDragging = true;

        screenDepth = dragCamera.WorldToScreenPoint(transform.position).z;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        offset = transform.position - mouseWorldPosition;

        targetPosition = transform.position;

        Debug.Log("Started dragging pastry: " + pastryType);
    }

    private void UpdateTargetPosition()
    {
        targetPosition = GetMouseWorldPosition() + offset;
    }

    private void StopDrag()
    {
        isDragging = false;

        if (dropZone != null && dropZone.IsPastryInside(pastryCollider))
        {
            bool success = dropZone.ReceivePastry(this);

            if (success)
            {
                return;
            }
        }

        ResetPastry();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = screenDepth;

        return dragCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

    public void ResetPastry()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}