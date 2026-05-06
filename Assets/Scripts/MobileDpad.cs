using UnityEngine;

public class MobileDPad : MonoBehaviour
{
    public static MobileDPad Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int horizontal;
    private int vertical;

    public Vector2 MoveInput
    {
        get
        {
            return new Vector2(horizontal, vertical);
        }
    }

    private void Awake()
    {
        Instance = this;

        if (showDebugLogs)
        {
            Debug.Log("MobileDPad is ready.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetLeftDown()
    {
        horizontal = -1;
        LogInput("Left down");
    }

    public void SetRightDown()
    {
        horizontal = 1;
        LogInput("Right down");
    }

    public void SetHorizontalUp()
    {
        horizontal = 0;
        LogInput("Horizontal up");
    }

    public void SetForwardDown()
    {
        vertical = 1;
        LogInput("Forward down");
    }

    public void SetBackDown()
    {
        vertical = -1;
        LogInput("Back down");
    }

    public void SetVerticalUp()
    {
        vertical = 0;
        LogInput("Vertical up");
    }

    public void StopAllMovement()
    {
        horizontal = 0;
        vertical = 0;
        LogInput("Stop all movement");
    }

    private void LogInput(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("MobileDPad: " + message + " | input = " + MoveInput);
        }
    }
}