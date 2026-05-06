using UnityEngine;

// Drop zone for the syrup stand in the Cafe scene.
// When the player drops the espresso cup here, it snaps to the snap point
// and (if syrup hasn't been done yet) triggers the syrup minigame.
public class SyrupStationDropZone : MonoBehaviour
{
    [Header("Snap Point")]
    [Tooltip("Where the cup ends up when dropped on the syrup stand.")]
    public Transform snapPoint;

    [Header("Drop Settings")]
    [Tooltip("How close (in screen pixels) the mouse must be to the snap point to count as a drop.")]
    public float screenAcceptRadius = 200f;

    [Header("Audio")]
    [SerializeField] private AudioSource placeAudio;

    public bool IsMouseOverDropZone(Camera cam, Vector2 mousePosition)
    {
        if (cam == null)
        {
            Debug.LogWarning("SyrupStationDropZone: Camera is missing.");
            return false;
        }

        if (snapPoint == null)
        {
            Debug.LogWarning("SyrupStationDropZone: Snap point is not assigned.");
            return false;
        }

        Vector3 screenPoint = cam.WorldToScreenPoint(snapPoint.position);

        float distance = Vector2.Distance(
            mousePosition,
            new Vector2(screenPoint.x, screenPoint.y)
        );

        return distance <= screenAcceptRadius;
    }

    public void ReceiveCup(CafeCupDraggable item)
    {
        if (item == null || snapPoint == null)
        {
            return;
        }

        // Only the espresso cup interacts with the syrup stand.
        if (item.itemType != CafeItemType.Espresso)
        {
            return;
        }

        item.SnapToCounter(snapPoint.position, snapPoint.rotation);
        item.MarkPlaced();

        if (placeAudio != null)
        {
            placeAudio.Play();
        }

        // If syrup hasn't been completed yet, trigger the minigame.
        // Otherwise just snap and do nothing (player has already syruped).
        bool syrupAlreadyDone =
            OrderProgressTracker.Instance != null &&
            OrderProgressTracker.Instance.HasCompletedSyrup;

        if (!syrupAlreadyDone)
        {
            SaveReturnPositionBeforeSyrupMinigame();

            if (GameSessionManager.Instance != null)
            {
                Debug.Log("Cup placed on syrup stand. Saved return position and triggering syrup minigame.");
                GameSessionManager.Instance.GoToSyrupMinigame();
            }
            else
            {
                Debug.LogWarning("SyrupStationDropZone: GameSessionManager not found — cannot load syrup minigame.");
            }
        }
        else
        {
            Debug.Log("Cup placed on syrup stand, but syrup is already complete. No action taken.");
        }
    }

    private void SaveReturnPositionBeforeSyrupMinigame()
    {
        SimpleFPSController player =
            Object.FindFirstObjectByType<SimpleFPSController>();

        if (player == null)
        {
            Debug.LogWarning("SyrupStationDropZone: Could not find SimpleFPSController to save return position.");
            return;
        }

        SceneReturnData.SaveReturnPosition(player.transform);

        Debug.Log("SyrupStationDropZone: Saved return position before syrup minigame.");
    }
}