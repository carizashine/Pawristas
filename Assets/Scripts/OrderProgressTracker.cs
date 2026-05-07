using UnityEngine;

// Tracks progress through the current customer order
public class OrderProgressTracker : MonoBehaviour
{
    public static OrderProgressTracker Instance { get; private set; }

    public bool HasTakenOrder { get; private set; }
    public bool HasCompletedEspresso { get; private set; }
    public bool HasCompletedSyrup { get; private set; }
    public bool HasCompletedPastry { get; private set; }

    public bool HasPlacedEspressoOnCounter { get; private set; }
    public bool HasPlacedPastryOnCounter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Clears all progress for a new order
    public void ResetProgress()
    {
        HasTakenOrder = false;
        HasCompletedEspresso = false;
        HasCompletedSyrup = false;
        HasCompletedPastry = false;

        HasPlacedEspressoOnCounter = false;
        HasPlacedPastryOnCounter = false;
    }

    public void MarkOrderTaken()
    {
        HasTakenOrder = true;
    }

    public void MarkEspressoComplete()
    {
        HasCompletedEspresso = true;
    }

    public void MarkSyrupComplete()
    {
        HasCompletedSyrup = true;
    }

    public void MarkPastryComplete()
    {
        HasCompletedPastry = true;
    }

    public void MarkEspressoPlacedOnCounter()
    {
        HasPlacedEspressoOnCounter = true;
    }

    public void MarkPastryPlacedOnCounter()
    {
        HasPlacedPastryOnCounter = true;
    }

    // Returns true when all steps for order are complete
    public bool CanGoToRating()
    {
        return HasTakenOrder &&
               HasCompletedEspresso &&
               HasCompletedSyrup &&
               HasCompletedPastry;
    }
}