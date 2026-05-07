using UnityEngine;

// Defines plate area where pastries can be dragged and dropped during minigame
public class PastryPlateDropZone : MonoBehaviour
{
    [Header("References")]
    public PastryMinigameController pastryController;

    [Header("Drop Behavior")]
    public Transform snapPoint;
    public float acceptRadius = 1.5f;
    public bool snapPastryOnSuccess = true;
    public bool hidePastryAfterSuccess = false;
    public bool resetPastryAfterSuccess = false;

    private void Awake()
    {
        if (pastryController == null)
        {
            pastryController = FindFirstObjectByType<PastryMinigameController>();
        }
    }

    // Checks whether dragged pastry is close enough to drop zone
    public bool IsPastryInside(Collider pastryCollider)
    {
        if (pastryCollider == null)
        {
            return false;
        }

        Transform target = snapPoint != null ? snapPoint : transform;

        float distance = Vector3.Distance(
            pastryCollider.bounds.center,
            target.position
        );

        Debug.Log("Pastry distance from plate snap point: " + distance);

        return distance <= acceptRadius;
    }

    // Called when pastry is released over the plate
    public bool ReceivePastry(PastryDraggable pastry)
    {
        if (pastryController == null)
        {
            pastryController = FindFirstObjectByType<PastryMinigameController>();
        }

        if (pastryController == null)
        {
            Debug.LogError("PastryPlateDropZone: Pastry controller is missing.");
            return false;
        }

        Debug.Log("Pastry dropped successfully: " + pastry.pastryType);

        pastryController.SelectPastry(pastry.pastryType);

        if (pastry != null)
        {
            if (snapPastryOnSuccess && snapPoint != null)
            {
                pastry.transform.position = snapPoint.position;
                pastry.transform.rotation = snapPoint.rotation;
            }

            if (hidePastryAfterSuccess)
            {
                pastry.gameObject.SetActive(false);
            }
            else if (resetPastryAfterSuccess)
            {
                pastry.ResetPastry();
            }
        }

        return true;
    }

    // Draws accepted drop radius
    private void OnDrawGizmosSelected()
    {
        Transform center = snapPoint != null ? snapPoint : transform;
        Gizmos.DrawWireSphere(center.position, acceptRadius);
    }
}