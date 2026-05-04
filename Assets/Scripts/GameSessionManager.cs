using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    public Order CurrentOrder { get; private set; }
    public PlayerOrderResult PlayerResult { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string cafeScene = "Cafe";
    [SerializeField] private string takingOrderScene = "TakingOrder";
    [SerializeField] private string espressoScene = "EspressoMinigame";
    [SerializeField] private string syrupScene = "SyrupMinigame";
    [SerializeField] private string pastryScene = "PastryMinigame";
    [SerializeField] private string ratingScene = "DrinkRating";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (CurrentOrder == null)
        {
            StartNewCustomerOrder();
        }
    }

    public void StartNewCustomerOrder()
    {
        CurrentOrder = OrderGenerator.GenerateRandomOrder();
        PlayerResult = new PlayerOrderResult();

        if (OrderProgressTracker.Instance != null)
        {
            OrderProgressTracker.Instance.ResetProgress();
        }

        Debug.Log("New customer order started: " + CurrentOrder.GetOrderText());
    }

    private void EnsurePlayerResultExists()
    {
        if (PlayerResult == null)
        {
            PlayerResult = new PlayerOrderResult();
        }
    }

    // NEW espresso timing save method.
    // Example: 2 successful out of 4 required.
    public void SaveEspressoResult(int successfulShots, int requiredShots)
    {
        EnsurePlayerResultExists();

        PlayerResult.espressoMade = true;

        PlayerResult.espressoSuccessfulShots = successfulShots;
        PlayerResult.espressoRequiredShots = requiredShots;

        // Keep this old/general field updated too.
        PlayerResult.espressoShots = successfulShots;

        if (requiredShots <= 0)
        {
            PlayerResult.espressoQualityScore = 0f;
        }
        else
        {
            PlayerResult.espressoQualityScore = Mathf.Clamp01((float)successfulShots / requiredShots);
        }

        Debug.Log(
            "Saved espresso result: " +
            successfulShots + " / " + requiredShots +
            " Quality: " + PlayerResult.espressoQualityScore
        );
    }

    // OLD compatibility method.
    // Keep this so older code that calls SaveEspressoResult(shots, qualityScore) does not break.
    public void SaveEspressoResult(int shots, float qualityScore)
    {
        EnsurePlayerResultExists();

        PlayerResult.espressoMade = true;
        PlayerResult.espressoShots = shots;
        PlayerResult.espressoSuccessfulShots = shots;

        if (CurrentOrder != null)
        {
            PlayerResult.espressoRequiredShots = CurrentOrder.espressoShots;
        }
        else
        {
            PlayerResult.espressoRequiredShots = Mathf.Max(1, shots);
        }

        PlayerResult.espressoQualityScore = Mathf.Clamp01(qualityScore);

        Debug.Log(
            "Saved espresso result. Shots: " +
            shots + ", Quality: " + PlayerResult.espressoQualityScore
        );
    }

    public void SaveSyrupResult(SyrupType syrup, float accuracyScore)
    {
        EnsurePlayerResultExists();

        PlayerResult.syrupMade = true;
        PlayerResult.syrup = syrup;
        PlayerResult.syrupAccuracyScore = Mathf.Clamp01(accuracyScore);

        Debug.Log("Saved syrup result. Syrup: " + syrup + ", Accuracy: " + PlayerResult.syrupAccuracyScore);
    }

    // Preferred pastry save method.
    // This gives 1/1 if correct, 0/1 if wrong.
    public void SavePastryResult(PastryType pastry)
    {
        EnsurePlayerResultExists();

        PlayerResult.pastryMade = true;
        PlayerResult.pastry = pastry;

        if (CurrentOrder != null && pastry == CurrentOrder.pastry)
        {
            PlayerResult.pastryAccuracyScore = 1f;
            PlayerResult.pastryQualityScore = 1f;
        }
        else
        {
            PlayerResult.pastryAccuracyScore = 0f;
            PlayerResult.pastryQualityScore = 0f;
        }

        Debug.Log(
            "Saved pastry result. Pastry: " +
            pastry +
            ", Accuracy: " +
            PlayerResult.pastryAccuracyScore
        );
    }

    // OLD compatibility method.
    // Keep this so older pastry scripts that pass qualityScore still compile.
    public void SavePastryResult(PastryType pastry, float qualityScore)
    {
        EnsurePlayerResultExists();

        PlayerResult.pastryMade = true;
        PlayerResult.pastry = pastry;

        float accuracy = 0f;

        if (CurrentOrder != null && pastry == CurrentOrder.pastry)
        {
            accuracy = 1f;
        }

        PlayerResult.pastryAccuracyScore = accuracy;
        PlayerResult.pastryQualityScore = Mathf.Clamp01(qualityScore);

        Debug.Log(
            "Saved pastry result. Pastry: " +
            pastry +
            ", Accuracy: " +
            PlayerResult.pastryAccuracyScore +
            ", Quality: " +
            PlayerResult.pastryQualityScore
        );
    }

    public int CalculateFinalScore()
    {
        if (CurrentOrder == null || PlayerResult == null)
        {
            return 0;
        }

        float totalScore = 0f;
        float maxScore = 0f;

        // Espresso: worth 1 point total.
        maxScore += 1f;

        if (PlayerResult.espressoMade)
        {
            totalScore += PlayerResult.espressoQualityScore;
        }

        // Syrup: worth 1 point total.
        maxScore += 1f;

        if (PlayerResult.syrupMade)
        {
            totalScore += PlayerResult.syrupAccuracyScore;
        }

        // Pastry: worth 1 point total.
        maxScore += 1f;

        if (PlayerResult.pastryMade)
        {
            totalScore += PlayerResult.pastryAccuracyScore;
        }

        if (maxScore <= 0f)
        {
            return 0;
        }

        float percentage = totalScore / maxScore;
        return Mathf.RoundToInt(percentage * 100f);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void GoToCafe()
    {
        LoadScene(cafeScene);
    }

    public void GoToTakingOrder()
    {
        LoadScene(takingOrderScene);
    }

    public void GoToEspressoMinigame()
    {
        LoadScene(espressoScene);
    }

    public void GoToSyrupMinigame()
    {
        LoadScene(syrupScene);
    }

    public void GoToPastryMinigame()
    {
        LoadScene(pastryScene);
    }

    public void GoToRating()
    {
        LoadScene(ratingScene);
    }
}