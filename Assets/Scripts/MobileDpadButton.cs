using UnityEngine;
using UnityEngine.EventSystems;

//dpad directions
public enum DPadButtonDirection
{
    Forward,
    Back,
    Left,
    Right
}

//dpad buttons
public class MobileDPadButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public DPadButtonDirection direction;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (MobileDPad.Instance == null)
        {
            // Debug.LogWarning("MobileDPadButton: No MobileDPad found in scene.");
            return;
        }

        switch (direction)
        {
            case DPadButtonDirection.Forward:
                MobileDPad.Instance.SetForwardDown();
                break;

            case DPadButtonDirection.Back:
                MobileDPad.Instance.SetBackDown();
                break;

            case DPadButtonDirection.Left:
                MobileDPad.Instance.SetLeftDown();
                break;

            case DPadButtonDirection.Right:
                MobileDPad.Instance.SetRightDown();
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseButton();
    }

    private void ReleaseButton()
    {
        if (MobileDPad.Instance == null)
        {
            return;
        }

        switch (direction)
        {
            case DPadButtonDirection.Forward:
            case DPadButtonDirection.Back:
                MobileDPad.Instance.SetVerticalUp();
                break;

            case DPadButtonDirection.Left:
            case DPadButtonDirection.Right:
                MobileDPad.Instance.SetHorizontalUp();
                break;
        }
    }
}