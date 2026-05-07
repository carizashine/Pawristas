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
    public bool espressoMade;

    public int espressoShots;

    public int espressoSuccessfulShots;
    public int espressoRequiredShots;
    public float espressoQualityScore;

    public bool syrupMade;
    public SyrupType syrup;
    public float syrupAccuracyScore;

    public bool pastryMade;
    public PastryType pastry;

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
    };

    public static Order GenerateRandomOrder()
    {
        Order order = new Order();

        order.customerName = customerNames[Random.Range(0, customerNames.Length)];

        order.drinkType = GetRandomEnumValue<DrinkType>();

        // 1 to 4 shots.
        order.espressoShots = Random.Range(1, 5);

        order.syrup = GetRandomEnumValue<SyrupType>();

        order.pastry = GetRandomPastry();

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