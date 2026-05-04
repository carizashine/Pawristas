using UnityEngine;

public class CafeEspressoCupSpawner : MonoBehaviour
{
    [Header("Cup")]
    public GameObject espressoCupPrefab;
    public Transform cupSpawnPoint;

    [Header("Drag Setup")]
    public Camera playerCamera;
    public MonoBehaviour fpsController;
    public CafeCounterDropZone counterDropZone;

    private void Start()
    {
        bool espressoComplete =
            GameSessionManager.Instance != null &&
            GameSessionManager.Instance.PlayerResult != null &&
            GameSessionManager.Instance.PlayerResult.espressoMade;

        if (!espressoComplete)
        {
            return;
        }

        SpawnEspressoCup();
    }

    private void SpawnEspressoCup()
    {
        if (espressoCupPrefab == null)
        {
            Debug.LogWarning("CafeEspressoCupSpawner is missing espresso cup prefab.");
            return;
        }

        Transform spawnPoint = cupSpawnPoint;
        bool alreadyPlaced = false;

        if (OrderProgressTracker.Instance != null &&
            OrderProgressTracker.Instance.HasPlacedEspressoOnCounter &&
            counterDropZone != null &&
            counterDropZone.GetSnapPoint(CafeItemType.Espresso) != null)
        {
            spawnPoint = counterDropZone.GetSnapPoint(CafeItemType.Espresso);
            alreadyPlaced = true;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("CafeEspressoCupSpawner is missing spawn point.");
            return;
        }

        GameObject cup = Instantiate(
            espressoCupPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (cup.GetComponent<Collider>() == null)
        {
            cup.AddComponent<BoxCollider>();
        }

        CafeCupDraggable draggable = cup.GetComponent<CafeCupDraggable>();

        if (draggable == null)
        {
            draggable = cup.AddComponent<CafeCupDraggable>();
        }

        draggable.itemType = CafeItemType.Espresso;
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

        Debug.Log("Spawned espresso cup in Cafe. Already placed: " + alreadyPlaced);
    }
}