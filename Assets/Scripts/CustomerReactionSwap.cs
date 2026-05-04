using UnityEngine;

public class CustomerReactionSwap : MonoBehaviour
{
    [Header("Current Visual")]
    [Tooltip("The current visible customer model. This should be the idle model child, not the whole customer root.")]
    [SerializeField] private GameObject currentVisual;

    [Header("Reaction Prefabs")]
    [SerializeField] private GameObject happyPrefab;
    [SerializeField] private GameObject sadPrefab;

    [Header("Reaction Settings")]
    [SerializeField] private int happyScoreThreshold = 70;

    private GameObject spawnedReactionVisual;

    public void ShowReaction(int score)
    {
        GameObject prefabToSpawn = score >= happyScoreThreshold ? happyPrefab : sadPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("CustomerReactionSwap: Missing happy or sad prefab.");
            return;
        }

        if (currentVisual != null)
        {
            currentVisual.SetActive(false);
        }

        if (spawnedReactionVisual != null)
        {
            Destroy(spawnedReactionVisual);
        }

        spawnedReactionVisual = Instantiate(
            prefabToSpawn,
            transform.position,
            transform.rotation,
            transform
        );

        spawnedReactionVisual.transform.localPosition = Vector3.zero;
        spawnedReactionVisual.transform.localRotation = Quaternion.identity;
        spawnedReactionVisual.transform.localScale = Vector3.one;

        Debug.Log("Customer reaction visual changed. Score: " + score);
    }

    public void ResetToIdle()
    {
        if (spawnedReactionVisual != null)
        {
            Destroy(spawnedReactionVisual);
            spawnedReactionVisual = null;
        }

        if (currentVisual != null)
        {
            currentVisual.SetActive(true);
        }
    }
}