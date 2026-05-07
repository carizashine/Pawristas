using UnityEngine;
using TMPro;

public class OrderDisplayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject orderPanel;
    [SerializeField] private TextMeshProUGUI customerNameText;
    [SerializeField] private TextMeshProUGUI orderText;

    private void Start()
    {
        HideOrder();
    }

    public void ShowCurrentOrder()
    {
        if (GameSessionManager.Instance == null)
        {
            Debug.LogError("OrderDisplayUI: GameSessionManager not found.");
            return;
        }

        Order order = GameSessionManager.Instance.CurrentOrder;

        if (order == null)
        {
            GameSessionManager.Instance.StartNewCustomerOrder();
            order = GameSessionManager.Instance.CurrentOrder;
        }

        if (customerNameText != null)
        {
            if (!string.IsNullOrWhiteSpace(order.animalType))
            {
                customerNameText.text = order.customerName + " the " + order.animalType;
            }
            else
            {
                customerNameText.text = order.customerName;
            }        
        }

        if (orderText != null)
        {
            orderText.text =
                order.GetOrderText() +
                "\n\nOrder Summary:\n" +
                order.drinkType + " · " +
                order.espressoShots + " shot(s) · " +
                order.syrup + " syrup · " +
                order.pastry;       
         }

        if (orderPanel != null)
        {
            orderPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ConfirmOrder()
    {
        if (OrderProgressTracker.Instance != null)
        {
            OrderProgressTracker.Instance.MarkOrderTaken();
        }

        HideOrder();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Order taken.");
    }

    public void HideOrder()
    {
        if (orderPanel != null)
        {
            orderPanel.SetActive(false);
        }
    }
}