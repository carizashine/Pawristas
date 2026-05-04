using UnityEngine;

public class CustomerInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string promptText = "Click to take order";

    [Header("UI")]
    [SerializeField] private OrderDisplayUI orderDisplayUI;

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
        if (GameSessionManager.Instance == null)
        {
            Debug.LogError("CustomerInteractable: GameSessionManager not found.");
            return;
        }

        if (GameSessionManager.Instance.CurrentOrder == null)
        {
            GameSessionManager.Instance.StartNewCustomerOrder();
        }

        if (orderDisplayUI == null)
        {
            orderDisplayUI = FindFirstObjectByType<OrderDisplayUI>();
        }

        if (orderDisplayUI == null)
        {
            Debug.LogError("CustomerInteractable: OrderDisplayUI not found.");
            return;
        }

        orderDisplayUI.ShowCurrentOrder();

        Debug.Log("Customer order opened.");
    }
}