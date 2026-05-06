using UnityEngine;

public class CafeCounterDropZone : MonoBehaviour
{
    [Header("Snap Points")]
    public Transform espressoSnapPoint;
    public Transform pastrySnapPoint;

    [Header("Drop Settings")]
    public float screenAcceptRadius = 200f;
    [Header("Audio")]
    [SerializeField] private AudioSource placeAudio;

    private CafeCupDraggable placedEspresso;
    private CafeCupDraggable placedPastry;

    public bool HasPlacedCup()
    {
        return HasPlacedEspresso() || HasPlacedPastry();
    }

    public bool HasPlacedEspresso()
    {
        if (placedEspresso != null)
        {
            return true;
        }

        return OrderProgressTracker.Instance != null &&
               OrderProgressTracker.Instance.HasPlacedEspressoOnCounter;
    }

    public bool HasPlacedPastry()
    {
        if (placedPastry != null)
        {
            return true;
        }

        return OrderProgressTracker.Instance != null &&
               OrderProgressTracker.Instance.HasPlacedPastryOnCounter;
    }

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

        Vector3 screenPoint = cam.WorldToScreenPoint(target.position);

        float distance = Vector2.Distance(
            mousePosition,
            new Vector2(screenPoint.x, screenPoint.y)
        );

        Debug.Log(itemType + " screen distance from snap point: " + distance);

        return distance <= screenAcceptRadius;
    }

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

    public void ClearPlacedCup()
    {
        ClearPlacedItems();
    }
}