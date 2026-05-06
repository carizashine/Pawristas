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

    [Tooltip("Optional. The syrup stand drop zone — if assigned, the cup respawns here after the syrup minigame.")]
    public SyrupStationDropZone syrupStation;

    [Header("Audio")]
    [SerializeField] private AudioSource spawnAudio;

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

        // Priority 1: Cup was already placed on the pickup counter.
        if (OrderProgressTracker.Instance != null &&
            OrderProgressTracker.Instance.HasPlacedEspressoOnCounter &&
            counterDropZone != null &&
            counterDropZone.GetSnapPoint(CafeItemType.Espresso) != null)
        {
            spawnPoint = counterDropZone.GetSnapPoint(CafeItemType.Espresso);
            alreadyPlaced = true;
        }
        // Priority 2: Syrup minigame is complete — spawn at the syrup stand so
        // the player can pick the cup up and bring it to the counter.
        else if (OrderProgressTracker.Instance != null &&
                 OrderProgressTracker.Instance.HasCompletedSyrup &&
                 syrupStation != null &&
                 syrupStation.snapPoint != null)
        {
            spawnPoint = syrupStation.snapPoint;
            // alreadyPlaced stays false — we want it draggable.
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
        if (spawnAudio != null)
        {
            spawnAudio.Play();
        }

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
        draggable.syrupStation = syrupStation;
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