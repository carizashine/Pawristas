using UnityEngine;

public enum DrinkType
{
    Latte,
    Cappuccino,
    Americano,
    Mocha
}

public enum SyrupType
{
    None,
    Vanilla,
    Caramel,
    Hazelnut,
    Chocolate
}

public enum PastryType
{
    None,
    ChocolateSprinkleDonut,
    PinkSprinkleDonut,
    GlazedDonut,
    OreoCupcake,
    RedVelvetCupcake
}

[System.Serializable]
public class Order
{
    public string customerName;

    public DrinkType drinkType;
    public int espressoShots;
    public SyrupType syrup;
    public PastryType pastry;

    [TextArea]
    public string customerOrderDialogue;

    public string GetOrderText()
    {
        if (!string.IsNullOrWhiteSpace(customerOrderDialogue))
        {
            return customerOrderDialogue;
        }

        return GetPlainOrderText();
    }

    public string GetPlainOrderText()
    {
        return customerName + " wants a " +
               drinkType + " with " +
               espressoShots + " espresso shot(s), " +
               syrup + " syrup, and a " +
               pastry + ".";
    }
}

[System.Serializable]
public class PlayerOrderResult
{
    [Header("Espresso")]
    public bool espressoMade;

    // Old/general field, kept so older scripts do not break.
    public int espressoShots;

    // New timing minigame scoring.
    public int espressoSuccessfulShots;
    public int espressoRequiredShots;
    public float espressoQualityScore;

    [Header("Syrup")]
    public bool syrupMade;
    public SyrupType syrup;
    public float syrupAccuracyScore;

    [Header("Pastry")]
    public bool pastryMade;
    public PastryType pastry;

    // Keep both names so older/newer pastry code still works.
    public float pastryQualityScore;
    public float pastryAccuracyScore;
}

public static class OrderGenerator
{
    private static readonly string[] customerNames =
    {
        "Mochi",
        "Bean",
        "Pumpkin",
        "Biscuit",
        "Chai",
        "GUAP",
    };

    public static Order GenerateRandomOrder()
    {
        Order order = new Order();

        order.customerName = customerNames[Random.Range(0, customerNames.Length)];

        order.drinkType = GetRandomEnumValue<DrinkType>();

        // 1 to 4 shots.
        // Unity int Random.Range max is exclusive, so 1, 5 means 1, 2, 3, or 4.
        order.espressoShots = Random.Range(1, 5);

        order.syrup = GetRandomEnumValue<SyrupType>();

        // Avoid None for customer pastry orders.
        order.pastry = GetRandomPastry();

        // Gemini fills this when the player clicks the customer.
        // If Gemini fails or is disabled, GetOrderText() uses GetPlainOrderText().
        order.customerOrderDialogue = "";

        return order;
    }

    private static PastryType GetRandomPastry()
    {
        PastryType[] pastries =
        {
            PastryType.ChocolateSprinkleDonut,
            PastryType.PinkSprinkleDonut,
            PastryType.GlazedDonut,
            PastryType.OreoCupcake,
            PastryType.RedVelvetCupcake
        };

        return pastries[Random.Range(0, pastries.Length)];
    }

    private static T GetRandomEnumValue<T>()
    {
        T[] values = (T[])System.Enum.GetValues(typeof(T));
        return values[Random.Range(0, values.Length)];
    }
}