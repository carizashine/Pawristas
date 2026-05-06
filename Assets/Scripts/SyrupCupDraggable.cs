using UnityEngine;
using UnityEngine.EventSystems;

// A/D on desktop.
// Touch-drag left/right on mobile.
// Y and X are fully locked. Only Z changes.
public class SyrupCupDraggable : MonoBehaviour
{
    [Tooltip("Movement speed in world units per second for keyboard controls.")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Left boundary in world Z.")]
    [SerializeField] private float minZ = -3f;

    [Tooltip("Right boundary in world Z.")]
    [SerializeField] private float maxZ = 3f;

    [Header("Mobile Drag")]
    [SerializeField] private bool useMobileDrag = true;

    [Tooltip("How much world Z movement happens per pixel of finger movement.")]
    [SerializeField] private float mobileDragSensitivity = 0.01f;

    [Tooltip("Turn this on/off if dragging feels reversed on the phone.")]
    [SerializeField] private bool invertMobileDrag = true;

    private int activeTouchId = -1;
    private Vector2 lastTouchPosition;

    private void Update()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        HandleMobileDrag();
#else
        HandleKeyboardMovement();
#endif
    }

    private void HandleKeyboardMovement()
    {
        float input = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            input = 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            input = -1f;
        }

        if (input == 0f)
        {
            return;
        }

        MoveAlongZ(input * moveSpeed * Time.deltaTime);
    }

    private void HandleMobileDrag()
    {
        if (!useMobileDrag)
        {
            return;
        }

        if (activeTouchId == -1)
        {
            TryStartTouchDrag();
            return;
        }

        UpdateTouchDrag();
    }

    private void TryStartTouchDrag()
    {
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

            activeTouchId = touch.fingerId;
            lastTouchPosition = touch.position;
            return;
        }
    }

    private void UpdateTouchDrag()
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
                Vector2 delta = touch.position - lastTouchPosition;
                lastTouchPosition = touch.position;

                float dragAmount = delta.x * mobileDragSensitivity;

                if (invertMobileDrag)
                {
                    dragAmount *= -1f;
                }

                MoveAlongZ(dragAmount);
                return;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                activeTouchId = -1;
                return;
            }
        }

        activeTouchId = -1;
    }

    private void MoveAlongZ(float amount)
    {
        Vector3 pos = transform.position;
        pos.z = Mathf.Clamp(pos.z + amount, minZ, maxZ);
        transform.position = pos;
    }
}