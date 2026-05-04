using System.Collections;
using UnityEngine;
using TMPro;

public class PastryMinigameController : MonoBehaviour
{
    [Header("Selected Pastry")]
    [SerializeField] private PastryType selectedPastry = PastryType.None;

    [Header("Timing")]
    [SerializeField] private float returnToCafeDelay = 1.2f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI selectedPastryText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private bool pastryComplete = false;
    private bool isFinishing = false;

    private void Start()
    {
        if (instructionText != null)
        {
            instructionText.text = "Drag the customer's pastry onto the plate.";
        }

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        UpdateUI();
    }

    public void SelectPastry(PastryType pastry)
    {
        if (pastryComplete || isFinishing)
        {
            return;
        }

        selectedPastry = pastry;
        pastryComplete = true;

        if (feedbackText != null)
        {
            feedbackText.text = "Selected: " + GetPastryDisplayName(selectedPastry);
        }

        UpdateUI();

        StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay()
    {
        isFinishing = true;

        yield return new WaitForSeconds(returnToCafeDelay);

        FinishPastryMinigame();
    }

    private void FinishPastryMinigame()
    {
        float qualityScore = 1f;

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.SavePastryResult(selectedPastry, qualityScore);
        }
        else
        {
            Debug.LogWarning("PastryMinigameController: GameSessionManager not found.");
        }

        if (OrderProgressTracker.Instance != null)
        {
            OrderProgressTracker.Instance.MarkPastryComplete();
        }
        else
        {
            Debug.LogWarning("PastryMinigameController: OrderProgressTracker not found.");
        }

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.GoToCafe();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cafe");
        }
    }

    private void UpdateUI()
    {
        if (selectedPastryText != null)
        {
            selectedPastryText.text = "Selected Pastry: " + GetPastryDisplayName(selectedPastry);
        }
    }

    private string GetPastryDisplayName(PastryType pastry)
    {
        switch (pastry)
        {
            case PastryType.ChocolateSprinkleDonut:
                return "Chocolate Sprinkle Donut";

            case PastryType.PinkSprinkleDonut:
                return "Pink Sprinkle Donut";

            case PastryType.GlazedDonut:
                return "Glazed Donut";

            case PastryType.OreoCupcake:
                return "Oreo Cupcake";

            case PastryType.RedVelvetCupcake:
                return "Red Velvet Cupcake";

            default:
                return "None";
        }
    }
}