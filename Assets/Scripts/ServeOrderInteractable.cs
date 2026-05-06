using System.Collections;
using UnityEngine;
using TMPro;

public class ServeOrderInteractable : MonoBehaviour, IInteractable
{
    [Header("Serving")]
    [SerializeField] private CafeCounterDropZone counterDropZone;
    [SerializeField] private CustomerSpawner customerSpawner;

    [Header("Gemini Dialogue")]
    [SerializeField] private GeminiDialogueClient geminiDialogueClient;
    [SerializeField] private bool useAIReactionDialogue = true;

    [Header("Requirements")]
    [SerializeField] private bool requireAnyItemOnCounter = true;

    [Header("Reaction UI")]
    [SerializeField] private GameObject reactionPanel;
    [SerializeField] private TextMeshProUGUI reactionText;

    [Header("Timing")]
    [SerializeField] private float reactionTime = 4f;

    [Header("Reaction Sounds")]
    [SerializeField] private AudioSource happyAudio;
    [SerializeField] private AudioSource sadAudio;

    private bool isServing = false;

    public string GetPromptText()
    {
        return "Click to serve order";
    }

    public void Interact()
    {
        if (isServing)
        {
            return;
        }

        if (requireAnyItemOnCounter)
        {
            bool hasSomethingReady =
                counterDropZone != null &&
                counterDropZone.HasPlacedCup();

            if (!hasSomethingReady)
            {
                ShowReactionMessage("There is nothing ready to serve yet!");
                Debug.Log("Cannot serve: nothing on counter.");
                return;
            }
        }

        StartCoroutine(ServeRoutine());
    }

    private IEnumerator ServeRoutine()
    {
        isServing = true;

        GameObject customer = GetCurrentCustomerObject();

        int finalScore = GetFinalScore();

        if (finalScore >= 70)
        {
            if (happyAudio != null)
            {
                happyAudio.Play();
            }
        }
        else
        {
            if (sadAudio != null)
            {
                sadAudio.Play();
            }
        }

        if (customer != null)
        {
            CustomerReactionSwap reactionSwap = customer.GetComponent<CustomerReactionSwap>();

            if (reactionSwap != null)
            {
                reactionSwap.ShowReaction(finalScore);
            }
        }

        string reactionMessage = GetCustomerReactionText(finalScore);

        if (useAIReactionDialogue)
        {
            if (geminiDialogueClient == null)
            {
                geminiDialogueClient = FindFirstObjectByType<GeminiDialogueClient>();
            }

            if (geminiDialogueClient != null &&
                GameSessionManager.Instance != null &&
                GameSessionManager.Instance.CurrentOrder != null &&
                GameSessionManager.Instance.PlayerResult != null)
            {
                yield return StartCoroutine(
                    geminiDialogueClient.GenerateReactionDialogue(
                        GameSessionManager.Instance.CurrentOrder,
                        GameSessionManager.Instance.PlayerResult,
                        finalScore,
                        aiText =>
                        {
                            reactionMessage = aiText;
                        }
                    )
                );
            }
            else
            {
                Debug.LogWarning("ServeOrderInteractable: GeminiDialogueClient/order/result missing. Using fallback reaction.");
            }
        }

        string scoreBreakdown = GetScoreBreakdownText();
        string fullMessage = reactionMessage + "\n\n" + scoreBreakdown;

        ShowReactionMessage(fullMessage);

        yield return new WaitForSeconds(reactionTime);

        if (reactionPanel != null)
        {
            reactionPanel.SetActive(false);
        }

        if (counterDropZone != null)
        {
            counterDropZone.ClearPlacedItems();
        }

        if (customerSpawner == null)
        {
            customerSpawner = FindFirstObjectByType<CustomerSpawner>();
        }

        if (customerSpawner != null)
        {
            customerSpawner.RemoveCurrentCustomer();
        }
        else if (customer != null)
        {
            Destroy(customer);
        }

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.StartNewCustomerOrder();
        }

        if (customerSpawner != null)
        {
            customerSpawner.SpawnCustomerIfNone();
        }

        isServing = false;
    }

    private GameObject GetCurrentCustomerObject()
    {
        CustomerInteractable customer = FindFirstObjectByType<CustomerInteractable>();

        if (customer != null)
        {
            return customer.gameObject;
        }

        return null;
    }

    private string GetCustomerReactionText(int finalScore)
    {
        if (finalScore >= 90)
        {
            return "Amazing! This is exactly what I wanted!";
        }

        if (finalScore >= 70)
        {
            return "Thank you! This looks pretty good!";
        }

        if (finalScore >= 40)
        {
            return "Hmm, this is okay, but something is a little off.";
        }

        return "Oh dear, I do not think this is what I ordered.";
    }

    private string GetScoreBreakdownText()
    {
        if (GameSessionManager.Instance == null ||
            GameSessionManager.Instance.CurrentOrder == null ||
            GameSessionManager.Instance.PlayerResult == null)
        {
            return "Score: unavailable";
        }

        Order order = GameSessionManager.Instance.CurrentOrder;
        PlayerOrderResult result = GameSessionManager.Instance.PlayerResult;

        int finalScore = GameSessionManager.Instance.CalculateFinalScore();

        string espressoLine = "Espresso: ";

        if (result.espressoMade)
        {
            espressoLine += result.espressoSuccessfulShots + " / " + result.espressoRequiredShots;
        }
        else
        {
            espressoLine += "0 / " + order.espressoShots;
        }

        string pastryLine = "Pastry: ";

        if (result.pastryMade)
        {
            pastryLine += result.pastry == order.pastry ? "1 / 1" : "0 / 1";
        }
        else
        {
            pastryLine += "0 / 1";
        }

        string syrupLine = "Syrup: ";

        if (result.syrupMade)
        {
            int syrupScore = Mathf.RoundToInt(result.syrupAccuracyScore);
            syrupLine += syrupScore + " / 1";
        }
        else
        {
            syrupLine += "0 / 1";
        }

        return
            "Score: " + finalScore + "%\n" +
            espressoLine + "\n" +
            pastryLine + "\n" +
            syrupLine;
    }

    private int GetFinalScore()
    {
        if (GameSessionManager.Instance == null)
        {
            return 0;
        }

        return GameSessionManager.Instance.CalculateFinalScore();
    }

    private void ShowReactionMessage(string message)
    {
        if (reactionPanel != null)
        {
            reactionPanel.SetActive(true);
        }

        if (reactionText != null)
        {
            reactionText.text = message;
        }

        Debug.Log(message);
    }
}