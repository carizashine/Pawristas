using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleFPSController : MonoBehaviour
{
    public static SimpleFPSController Instance { get; private set; }

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
    [SerializeField] private float mobileLookSensitivity = 0.18f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Mobile Only")]
    [SerializeField] private bool useMobileDPad = true;
    [SerializeField] private bool useMobileTouchLook = true;

    [Tooltip("Touches on the right side of the screen look around. 0.45 means the right 55% of the screen.")]
    [SerializeField] private float mobileLookStartPercent = 0.45f;

    private float verticalVelocity;
    private float xRotation;

    private int activeLookFingerId = -1;
    private Vector2 lastLookTouchPosition;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#else
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    private void OnEnable()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        activeLookFingerId = -1;

        if (MobileDPad.Instance != null)
        {
            MobileDPad.Instance.StopAllMovement();
        }
#endif
    }

    private void Update()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        HandleMobileLook();
#else
        HandleMouseLook();
#endif

        HandleMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        ApplyLook(mouseX, mouseY);
    }

    private void HandleMobileLook()
    {
        if (!useMobileTouchLook)
        {
            return;
        }

        if (CafeCupDraggable.IsDraggingAnyCafeItem)
        {
            activeLookFingerId = -1;
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (IsTouchOverUI(touch.fingerId))
            {
                continue;
            }

            if (touch.position.x < Screen.width * mobileLookStartPercent)
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

                    ApplyMobileLookDelta(delta);
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

    public void ApplyMobileLookDelta(Vector2 delta)
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        float lookX = delta.x * mobileLookSensitivity;
        float lookY = delta.y * mobileLookSensitivity;

        ApplyLook(lookX, lookY);
#endif
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
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if (useMobileDPad && MobileDPad.Instance != null)
        {
            Vector2 dpadInput = MobileDPad.Instance.MoveInput;

            if (dpadInput.sqrMagnitude > 0.01f)
            {
                moveX = dpadInput.x;
                moveZ = dpadInput.y;
            }
        }
#endif

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