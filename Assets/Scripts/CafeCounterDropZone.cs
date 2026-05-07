using UnityEngine;

// Handles cafe counter where items are ready to be dropped
public class CafeCounterDropZone : MonoBehaviour
{
    [Header("Snap Points")]
    // Position and rotation where finished espresso and pastry should snap too
    public Transform espressoSnapPoint;
    public Transform pastrySnapPoint;

    // How close touch position is to snap point
    [Header("Drop Settings")]
    public float screenAcceptRadius = 200f;
    [Header("Audio")]
    [SerializeField] private AudioSource placeAudio;

    private CafeCupDraggable placedEspresso;
    private CafeCupDraggable placedPastry;

    // Checks if one valid item has been placed on counter
    public bool HasPlacedCup()
    {
        return HasPlacedEspresso() || HasPlacedPastry();
    }

    // Checks espresso has been placed tracking placement state globally
    public bool HasPlacedEspresso()
    {
        if (placedEspresso != null)
        {
            return true;
        }

        return OrderProgressTracker.Instance != null &&
                OrderProgressTracker.Instance.HasPlacedEspressoOnCounter;
    }

    // Checks whether pastry has been placed tracking state globally
    public bool HasPlacedPastry()
    {
        if (placedPastry != null)
        {
            return true;
        }

        return OrderProgressTracker.Instance != null &&
               OrderProgressTracker.Instance.HasPlacedPastryOnCounter;
    }

    // Checks user mouse to see if position is close enough to snap point
    public bool IsMouseOverDropZone(Camera cam, Vector2 mousePosition, CafeItemType itemType)
    {
        if (cam == null)
        {
            Debug.LogWarning("CafeCounterDropZone: Camera is missing.");
            return false;
        }

        Transform target = GetSnapPoint(itemType);

        if (target == null)
        {
            Debug.LogWarning("CafeCounterDropZone: Missing snap point for " + itemType);
            return false;
        }

        // Convert snap point position into a screen position
        Vector3 screenPoint = cam.WorldToScreenPoint(target.position);

        float distance = Vector2.Distance(
            mousePosition,
            new Vector2(screenPoint.x, screenPoint.y)
        );

        Debug.Log(itemType + " screen distance from snap point: " + distance);

        return distance <= screenAcceptRadius;
    }

    // Snaps cup into place, registers it, and updates
    public void ReceiveCup(CafeCupDraggable item)
    {
        if (item == null)
        {
            return;
        }

        Transform target = GetSnapPoint(item.itemType);

        if (target == null)
        {
            Debug.LogWarning("CafeCounterDropZone: No snap point for " + item.itemType);
            return;
        }

        item.SnapToCounter(target.position, target.rotation);
        item.MarkPlaced();

        RegisterPlacedItem(item);
        Debug.Log("Placement sound should play now.");
        if (placeAudio != null)
        {
            placeAudio.Play();
        }

        // Update global order progress to other scripts
        if (item.itemType == CafeItemType.Espresso)
        {
            if (OrderProgressTracker.Instance != null)
            {
                OrderProgressTracker.Instance.MarkEspressoPlacedOnCounter();
            }
        }
        else if (item.itemType == CafeItemType.Pastry)
        {
            if (OrderProgressTracker.Instance != null)
            {
                OrderProgressTracker.Instance.MarkPastryPlacedOnCounter();
            }
        }

        Debug.Log(item.itemType + " snapped to pickup counter.");
    }

    // Stores placed item in correct local variable
    public void RegisterPlacedItem(CafeCupDraggable item)
    {
        if (item == null)
        {
            return;
        }

        if (item.itemType == CafeItemType.Espresso)
        {
            placedEspresso = item;
        }
        else if (item.itemType == CafeItemType.Pastry)
        {
            placedPastry = item;
        }
    }

    // Returns snap point for different item type
    public Transform GetSnapPoint(CafeItemType itemType)
    {
        switch (itemType)
        {
            case CafeItemType.Espresso:
                return espressoSnapPoint;

            case CafeItemType.Pastry:
                return pastrySnapPoint;

            default:
                return null;
        }
    }

    // Removes placed items from the counter
    public void ClearPlacedItems()
    {
        if (placedEspresso != null)
        {
            Destroy(placedEspresso.gameObject);
            placedEspresso = null;
        }

        if (placedPastry != null)
        {
            Destroy(placedPastry.gameObject);
            placedPastry = null;
        }
    }

    // Clears all placed items
    public void ClearPlacedCup()
    {
        ClearPlacedItems();
    }
}