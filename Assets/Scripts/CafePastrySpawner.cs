using System.Collections;
using UnityEngine;

public class CafePastrySpawner : MonoBehaviour
{
    [Header("Plate")]
    public GameObject platePrefab;
    public Transform finishedPlateSpawnPoint;

    [Header("Pastry Prefabs")]
    public GameObject chocolateSprinkleDonutPrefab;
    public GameObject pinkSprinkleDonutPrefab;
    public GameObject glazedDonutPrefab;
    public GameObject oreoCupcakePrefab;
    public GameObject redVelvetCupcakePrefab;

    [Header("Pastry Placement On Plate")]
    public Vector3 pastryLocalPosition = new Vector3(0f, 0.12f, 0f);
    public Vector3 pastryLocalRotation = Vector3.zero;
    public Vector3 pastryLocalScale = Vector3.one;

    [Header("Drag Setup")]
    public Camera playerCamera;
    public MonoBehaviour fpsController;
    public CafeCounterDropZone counterDropZone;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private IEnumerator Start()
    {
        yield return null;

        if (GameSessionManager.Instance == null ||
            GameSessionManager.Instance.PlayerResult == null)
        {
            yield break;
        }

        PlayerOrderResult result = GameSessionManager.Instance.PlayerResult;

        if (!result.pastryMade)
        {
            yield break;
        }

        SpawnFinishedPastryPlate(result.pastry);
    }

    private void SpawnFinishedPastryPlate(PastryType pastryType)
    {
        if (platePrefab == null)
        {
            Debug.LogWarning("CafePastrySpawner: Plate prefab is missing.");
            return;
        }

        GameObject pastryPrefab = GetPrefabForPastry(pastryType);

        if (pastryPrefab == null)
        {
            Debug.LogWarning("CafePastrySpawner: No pastry prefab assigned for " + pastryType);
            return;
        }

        Transform spawnPoint = finishedPlateSpawnPoint;
        bool alreadyPlaced = false;

        if (OrderProgressTracker.Instance != null &&
            OrderProgressTracker.Instance.HasPlacedPastryOnCounter &&
            counterDropZone != null &&
            counterDropZone.GetSnapPoint(CafeItemType.Pastry) != null)
        {
            spawnPoint = counterDropZone.GetSnapPoint(CafeItemType.Pastry);
            alreadyPlaced = true;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("CafePastrySpawner: Finished plate spawn point is missing.");
            return;
        }

        GameObject plate = Instantiate(
            platePrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        GameObject pastry = Instantiate(pastryPrefab, plate.transform);

        pastry.transform.localPosition = pastryLocalPosition;
        pastry.transform.localEulerAngles = pastryLocalRotation;
        pastry.transform.localScale = pastryLocalScale;

        if (plate.GetComponent<Collider>() == null)
        {
            plate.AddComponent<BoxCollider>();
        }

        CafeCupDraggable draggable = plate.GetComponent<CafeCupDraggable>();

        if (draggable == null)
        {
            draggable = plate.AddComponent<CafeCupDraggable>();
        }

        draggable.itemType = CafeItemType.Pastry;
        draggable.dragCamera = playerCamera != null ? playerCamera : Camera.main;
        draggable.fpsController = fpsController;
        draggable.dropZone = counterDropZone;
        draggable.disablePlayerMovementWhileDragging = false;

        if (alreadyPlaced)
        {
            draggable.MarkPlaced();

            if (counterDropZone != null)
            {
                counterDropZone.RegisterPlacedItem(draggable);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log("Spawned finished pastry plate in Cafe: " + pastryType + ". Already placed: " + alreadyPlaced);
        }
    }

    private GameObject GetPrefabForPastry(PastryType pastryType)
    {
        switch (pastryType)
        {
            case PastryType.ChocolateSprinkleDonut:
                return chocolateSprinkleDonutPrefab;

            case PastryType.PinkSprinkleDonut:
                return pinkSprinkleDonutPrefab;

            case PastryType.GlazedDonut:
                return glazedDonutPrefab;

            case PastryType.OreoCupcake:
                return oreoCupcakePrefab;

            case PastryType.RedVelvetCupcake:
                return redVelvetCupcakePrefab;

            default:
                return null;
        }
    }
}