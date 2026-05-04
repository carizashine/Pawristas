using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance { get; private set; }

    [Header("Customer Prefabs")]
    [SerializeField] private GameObject[] customerPrefabs;

    [Header("Spawn Point")]
    [SerializeField] private Transform customerSpawnPoint;

    [Header("Order UI")]
    [SerializeField] private OrderDisplayUI orderDisplayUI;

    private GameObject currentCustomer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnCustomerIfNone();
    }

    public void SpawnCustomerIfNone()
    {
        if (currentCustomer != null)
        {
            Debug.Log("Customer already exists: " + currentCustomer.name);
            return;
        }

        SpawnRandomCustomer();
    }

    public void SpawnRandomCustomer()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0)
        {
            Debug.LogWarning("CustomerSpawner: No customer prefabs assigned.");
            return;
        }

        if (customerSpawnPoint == null)
        {
            Debug.LogWarning("CustomerSpawner: No customer spawn point assigned.");
            return;
        }

        int randomIndex = Random.Range(0, customerPrefabs.Length);
        GameObject prefab = customerPrefabs[randomIndex];

        currentCustomer = Instantiate(
            prefab,
            customerSpawnPoint.position,
            customerSpawnPoint.rotation
        );

        currentCustomer.name = prefab.name + "_CurrentCustomer";
        currentCustomer.SetActive(true);

        Debug.Log(
            "Spawned customer: " + currentCustomer.name +
            " at position " + currentCustomer.transform.position
        );

        CustomerInteractable interactable = currentCustomer.GetComponent<CustomerInteractable>();

        if (interactable == null)
        {
            interactable = currentCustomer.AddComponent<CustomerInteractable>();
        }

        if (orderDisplayUI == null)
        {
            orderDisplayUI = FindFirstObjectByType<OrderDisplayUI>();
        }

        interactable.SetOrderDisplayUI(orderDisplayUI);

        Collider customerCollider = currentCustomer.GetComponent<Collider>();

        if (customerCollider == null)
        {
            CapsuleCollider capsule = currentCustomer.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0f, 1f, 0f);
        }
    }

    public void RemoveCurrentCustomer()
    {
        if (currentCustomer != null)
        {
            Debug.Log("Removing customer: " + currentCustomer.name);
            Destroy(currentCustomer);
            currentCustomer = null;
        }
    }
}