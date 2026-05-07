using UnityEngine;

public class CafeEspressoCupSpawner : MonoBehaviour
{
    //cup
    public GameObject espressoCupPrefab;
    public Transform cupSpawnPoint;
    public Camera playerCamera;
    public MonoBehaviour fpsController;
    public CafeCounterDropZone counterDropZone;

    public SyrupStationDropZone syrupStation;

    //audio
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
            // Debug.LogWarning("CafeEspressoCupSpawner is missing espresso cup prefab.");
            return;
        }

        Transform spawnPoint = cupSpawnPoint;
        bool alreadyPlaced = false;

        //Cup already placed on the pickup counter
        if (OrderProgressTracker.Instance != null &&
            OrderProgressTracker.Instance.HasPlacedEspressoOnCounter &&
            counterDropZone != null &&
            counterDropZone.GetSnapPoint(CafeItemType.Espresso) != null)
        {
            spawnPoint = counterDropZone.GetSnapPoint(CafeItemType.Espresso);
            alreadyPlaced = true;
        }
        // Syrup minigame is complete, spawn at the syrup stand so the player can pick the cup up and bring it to the counter.
        else if (OrderProgressTracker.Instance != null &&
                 OrderProgressTracker.Instance.HasCompletedSyrup &&
                 syrupStation != null &&
                 syrupStation.snapPoint != null)
        {
            spawnPoint = syrupStation.snapPoint;
        }

        if (spawnPoint == null)
        {
            // Debug.LogWarning("CafeEspressoCupSpawner is missing spawn point.");
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

        // Debug.Log("Spawned espresso cup in Cafe. Already placed: " + alreadyPlaced);
    }
}