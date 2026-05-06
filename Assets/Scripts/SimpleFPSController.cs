using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleFPSController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraHolder;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float mobileLookSensitivity = 0.12f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Mobile")]
    [SerializeField] private bool allowMobileDPadInput = true;
    [SerializeField] private bool allowMobileTouchLook = true;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnDesktop = true;

    private float verticalVelocity;
    private float xRotation;

    private int activeLookFingerId = -1;
    private Vector2 lastLookTouchPosition;

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

#if UNITY_IOS || UNITY_ANDROID
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#else
        if (lockCursorOnDesktop)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
#endif
    }

    private void OnEnable()
    {
        activeLookFingerId = -1;

        if (MobileDPad.Instance != null)
        {
            MobileDPad.Instance.StopAllMovement();
        }
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
#if UNITY_IOS || UNITY_ANDROID
        if (allowMobileTouchLook)
        {
            HandleMobileTouchLook();
        }
#else
        HandleMouseLook();
#endif
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        ApplyLook(mouseX, mouseY);
    }

    private void HandleMobileTouchLook()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (IsTouchOverUI(touch.fingerId))
            {
                continue;
            }

            // Only use the right half of the screen for looking.
            if (touch.position.x < Screen.width * 0.45f)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Began)
            {
                activeLookFingerId = touch.fingerId;
                lastLookTouchPosition = touch.position;
                return;
            }

            if (touch.fingerId == activeLookFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 delta = touch.position - lastLookTouchPosition;
                    lastLookTouchPosition = touch.position;

                    float lookX = delta.x * mobileLookSensitivity;
                    float lookY = delta.y * mobileLookSensitivity;

                    ApplyLook(lookX, lookY);
                    return;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    activeLookFingerId = -1;
                    return;
                }
            }
        }
    }

    private void ApplyLook(float lookX, float lookY)
    {
        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * lookX);
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        if (allowMobileDPadInput && MobileDPad.Instance != null)
        {
            Vector2 mobileMove = MobileDPad.Instance.MoveInput;

            if (mobileMove.sqrMagnitude > 0.01f)
            {
                moveX = mobileMove.x;
                moveZ = mobileMove.y;
            }
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        if (controller != null && controller.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * currentSpeed;
        finalMove.y = verticalVelocity;

        if (controller != null)
        {
            controller.Move(finalMove * Time.deltaTime);
        }
    }

    private bool IsTouchOverUI(int fingerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}