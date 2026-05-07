using UnityEngine;

public class MobileDPad : MonoBehaviour
{

    //mobile contorls
    public static MobileDPad Instance { get; private set; }
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
    }

    public void SetRightDown()
    {
        horizontal = 1;
    }

    public void SetHorizontalUp()
    {
        horizontal = 0;
    }

    public void SetForwardDown()
    {
        vertical = 1;
    }

    public void SetBackDown()
    {
        vertical = -1;
    }

    public void SetVerticalUp()
    {
        vertical = 0;
    }

    public void StopAllMovement()
    {
        horizontal = 0;
        vertical = 0;
    }
}