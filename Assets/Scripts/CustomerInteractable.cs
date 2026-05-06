using System.Collections;
using UnityEngine;

public class CustomerInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string promptText = "Click to take order";

    [Header("UI")]
    [SerializeField] private OrderDisplayUI orderDisplayUI;

    [Header("Gemini Dialogue")]
    [SerializeField] private GeminiDialogueClient geminiDialogueClient;
    [SerializeField] private bool useAIOrderDialogue = true;

    [Header("Audio")]
    [SerializeField] private AudioSource orderAudio;

    private bool isOpeningOrder;

    public void SetOrderDisplayUI(OrderDisplayUI ui)
    {
        orderDisplayUI = ui;
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public void Interact()
    {
        if (isOpeningOrder)
        {
            return;
        }

        StartCoroutine(OpenOrderRoutine());
    }

    private IEnumerator OpenOrderRoutine()
    {
        isOpeningOrder = true;

        if (GameSessionManager.Instance == null)
        {
            Debug.LogError("CustomerInteractable: GameSessionManager not found.");
            isOpeningOrder = false;
            yield break;
        }

        if (GameSessionManager.Instance.CurrentOrder == null)
        {
            GameSessionManager.Instance.StartNewCustomerOrder();
        }

        Order currentOrder = GameSessionManager.Instance.CurrentOrder;

        if (useAIOrderDialogue &&
            currentOrder != null &&
            string.IsNullOrWhiteSpace(currentOrder.customerOrderDialogue))
        {
            if (geminiDialogueClient == null)
            {
                geminiDialogueClient = FindFirstObjectByType<GeminiDialogueClient>();
            }

            if (geminiDialogueClient != null)
            {
                yield return StartCoroutine(
                    geminiDialogueClient.GenerateOrderDialogue(
                        currentOrder,
                        generatedDialogue =>
                        {
                            currentOrder.customerOrderDialogue = generatedDialogue;
                        }
                    )
                );
            }
            else
            {
                Debug.LogWarning("CustomerInteractable: GeminiDialogueClient not found. Using normal order text.");
            }
        }

        if (orderDisplayUI == null)
        {
            orderDisplayUI = FindFirstObjectByType<OrderDisplayUI>();
        }

        if (orderDisplayUI == null)
        {
            Debug.LogError("CustomerInteractable: OrderDisplayUI not found.");
            isOpeningOrder = false;
            yield break;
        }

        orderDisplayUI.ShowCurrentOrder();

        if (orderAudio == null)
        {
            orderAudio = GetComponent<AudioSource>();
        }

        if (orderAudio != null)
        {
            orderAudio.Play();
        }

        Debug.Log("Customer order opened.");

        isOpeningOrder = false;
    }
}