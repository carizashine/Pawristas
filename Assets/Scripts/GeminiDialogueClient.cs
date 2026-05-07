using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiDialogueClient : MonoBehaviour
{
    [Header("Gemini")]
    [Tooltip("Prototype only. Do not commit a real API key or ship it in a public build.")]
    [SerializeField] private string apiKey = "PASTE_YOUR_GEMINI_API_KEY_HERE";

    [SerializeField] private string model = "gemini-2.5-flash";

    [Header("Request Settings")]
    [SerializeField] private float timeoutSeconds = 10f;

    [Serializable]
    private class GeminiRequest
    {
        public Content[] contents;
        public GenerationConfig generationConfig;
    }

    [Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [Serializable]
    private class Part
    {
        public string text;
    }

    [Serializable]
    private class GenerationConfig
    {
        public float temperature = 0.75f;
        public int maxOutputTokens = 180;
        public ThinkingConfig thinkingConfig;
    }

    [Serializable]
    private class ThinkingConfig
    {
        public int thinkingBudget = 0;
    }

    [Serializable]
    private class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [Serializable]
    private class Candidate
    {
        public Content content;
        public string finishReason;
    }

    public IEnumerator GenerateOrderDialogue(Order order, Action<string> onComplete)
    {
        if (order == null)
        {
            onComplete?.Invoke("");
            yield break;
        }

        string prompt = BuildOrderPrompt(order);

        yield return StartCoroutine(SendGeminiRequest(
            prompt,
            GetFallbackOrderDialogue(order),
            CleanDialogue,
            IsUsableDialogue,
            onComplete
        ));
    }

    public IEnumerator GenerateReactionDialogue(
        Order order,
        PlayerOrderResult result,
        int finalScore,
        Action<string> onComplete
    )
    {
        if (order == null || result == null)
        {
            onComplete?.Invoke(GetFallbackReaction(finalScore));
            yield break;
        }

        string prompt = BuildReactionPrompt(order, result, finalScore);

        yield return StartCoroutine(SendGeminiRequest(
            prompt,
            GetFallbackReaction(finalScore),
            CleanDialogue,
            IsUsableDialogue,
            onComplete
        ));
    }

    private IEnumerator SendGeminiRequest(
        string prompt,
        string fallback,
        Func<string, string> cleaner,
        Func<string, bool> validator,
        Action<string> onComplete
    )
    {
        if (string.IsNullOrWhiteSpace(apiKey) ||
            apiKey == "PASTE_YOUR_GEMINI_API_KEY_HERE")
        {
            Debug.LogWarning("GeminiDialogueClient: API key is missing.");
            onComplete?.Invoke(fallback);
            yield break;
        }

        GeminiRequest body = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content
                {
                    parts = new Part[]
                    {
                        new Part { text = prompt }
                    }
                }
            },
            generationConfig = new GenerationConfig
            {
                temperature = 0.75f,
                maxOutputTokens = 180,
                thinkingConfig = new ThinkingConfig
                {
                    thinkingBudget = 0
                }
            }
        };

        string json = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        string url =
            "https://generativelanguage.googleapis.com/v1beta/models/" +
            model +
            ":generateContent";

        using UnityWebRequest request = new UnityWebRequest(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.RoundToInt(timeoutSeconds);

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-goog-api-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                "Gemini request failed: " +
                request.error +
                "\nResponse: " +
                request.downloadHandler.text
            );

            onComplete?.Invoke(fallback);
            yield break;
        }

        Debug.Log("Gemini raw response: " + request.downloadHandler.text);

        string text = TryExtractText(request.downloadHandler.text);
        text = cleaner(text);

        if (!validator(text))
        {
            Debug.LogWarning("Gemini returned unusable text: " + text);
            text = fallback;
        }

        onComplete?.Invoke(text);
    }

    private string BuildOrderPrompt(Order order)
    {
        string syrupText = order.syrup == SyrupType.None
            ? "no syrup"
            : order.syrup + " syrup";

        return
            "You are writing a customer's spoken order for a cozy animal cafe game called Pawristas.\n" +
            "Return only the customer's spoken order. No labels. No quotation marks.\n" +
            "Start with a casual greeting like Hey!, Hey there!, Hi!, Heya!, or Heyyo!.\n" +
            "Then politely ask for the order.\n" +
            "Use at most one playful animal/cafe word. Do not force wordplay every time.\n" +
            "Vary the wording. Possible playful words include purr-fect, paw-sitive, paw-lease, pawsome, meowgical, fur-tastic, cozy paws, whisker-worthy, tail-wagging, or treat-tastic.\n" +
            "Do not repeat the same playful word if another one would fit better.\n" +
            "Write 1 or 2 short sentences, 14 to 23 words total.\n" +
            "Cute, polite, cozy, and clear.\n" +
            "The order details must stay exactly the same.\n\n" +

            "Customer name: " + order.customerName + "\n" +
            "Drink: " + order.drinkType + "\n" +
            "Espresso shots: " + order.espressoShots + "\n" +
            "Syrup: " + syrupText + "\n" +
            "Pastry: " + order.pastry + "\n\n" +

            "Customer order line:";
    }

    private string BuildReactionPrompt(Order order, PlayerOrderResult result, int finalScore)
    {
        string themeName = "Cozy Cafe Day";
        string themeDescription = "Customers are friendly and cozy.";
        string moodText = "warm and cheerful";

        if (GameSessionManager.Instance != null && GameSessionManager.Instance.CurrentCafeTheme != null)
        {
            themeName = GameSessionManager.Instance.CurrentCafeTheme.themeName;
            themeDescription = GameSessionManager.Instance.CurrentCafeTheme.themeDescription;
            moodText = GameSessionManager.Instance.CurrentCafeTheme.mood;
        }

        string mood =
            finalScore >= 90 ? "very happy" :
            finalScore >= 70 ? "happy" :
            finalScore >= 40 ? "polite but disappointed" :
            "sad";

        return
            "You are writing dialogue for a cozy animal cafe game called Pawristas.\n" +
            "Return only one complete customer reaction. No labels. No quotation marks.\n" +
            "Write 1 or 2 short sentences, 18 to 25 words total.\n" +
            "Cute, polite, cafe-themed.\n" +
            "Use at most one playful animal/cafe word. Do not force wordplay every time.\n" +
            "Vary the wording. Possible happy words include purr-fect, paw-sitive, pawsome, meowgical, fur-tastic, whisker-worthy, tail-wagging, cozy-pawed, or treat-tastic.\n" +
            "Possible disappointed words include purr-plexed, un-fur-tunate, paw-fully, whisker-confused, a little ruff, not quite pawsome, or missing my treat.\n" +
            "Do not repeat the same playful word if another one would fit better.\n" +
            "Use happier wordplay for good scores and disappointed wordplay for bad scores.\n" +
            "Do not mention the exact score or percentage.\n" +
            "Mood: " + mood + ".\n\n" +

            "Order wanted: " +
            order.drinkType + ", " +
            order.espressoShots + " espresso shot(s), " +
            order.syrup + " syrup, " +
            order.pastry + ".\n" +

            "Player made: " +
            result.espressoSuccessfulShots + "/" + result.espressoRequiredShots + " espresso shots, " +
            "syrupMade=" + result.syrupMade + ", syrup=" + result.syrup + ", " +
            "pastryMade=" + result.pastryMade + ", pastry=" + result.pastry + ".\n" +

            "Daily cafe theme: " + themeName + " - " + themeDescription + "\n" +
            "Theme mood: " + moodText + "\n" +
            "Customer name: " + order.customerName + "\n" +
            "Animal type: " + order.animalType + "\n" +
            "Personality: " + order.personality + "\n" +
            "Speaking style: " + order.speakingStyle + "\n\n" +

            "Write the customer reaction now:";
    }

    private string TryExtractText(string json)
    {
        try
        {
            GeminiResponse parsed = JsonUtility.FromJson<GeminiResponse>(json);

            if (parsed == null ||
                parsed.candidates == null ||
                parsed.candidates.Length == 0)
            {
                return null;
            }

            Candidate firstCandidate = parsed.candidates[0];

            if (firstCandidate == null ||
                firstCandidate.content == null ||
                firstCandidate.content.parts == null ||
                firstCandidate.content.parts.Length == 0)
            {
                return null;
            }

            return firstCandidate.content.parts[0].text;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to parse Gemini response: " + e.Message);
            return null;
        }
    }

    private string CleanDialogue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = text.Trim();

        text = text.Replace("\"", "");
        text = text.Replace("“", "");
        text = text.Replace("”", "");

        text = text.Replace("Customer order line:", "");
        text = text.Replace("Order line:", "");
        text = text.Replace("Customer reaction:", "");
        text = text.Replace("Reaction:", "");
        text = text.Replace("Customer:", "");

        return text.Trim();
    }

    private bool IsUsableDialogue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] words = text.Split(
            new char[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (words.Length < 5)
        {
            return false;
        }

        if (text.Length < 12)
        {
            return false;
        }

        if (text == "Oh," ||
            text == "Oh." ||
            text == "Hmm," ||
            text == "Hmm.")
        {
            return false;
        }

        return true;
    }

    private string GetFallbackOrderDialogue(Order order)
    {
        string syrupText = order.syrup == SyrupType.None
            ? "no syrup"
            : order.syrup + " syrup";

        return "Hey there, could I please have a " +
               order.drinkType +
               " with " +
               order.espressoShots +
               " espresso shot(s), " +
               syrupText +
               ", and a " +
               order.pastry +
               "?";
    }

    private string GetFallbackReaction(int finalScore)
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

    private string CleanJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = text.Trim();

        text = text.Replace("```json", "");
        text = text.Replace("```", "");

        int firstBrace = text.IndexOf('{');
        int lastBrace = text.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            text = text.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return text.Trim();
    }

    private bool IsUsableJson(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
        text.Contains("{") &&
        text.Contains("}");
    }

    public IEnumerator GenerateDailyTheme(Action<CafeTheme> onComplete)
    {
        string prompt =
            "Generate one dialogue-only daily cafe theme for a cozy animal cafe game called Pawristas.\n" +
            "The theme must affect only customer names, personalities, order dialogue, and reaction dialogue.\n" +
            "Do not mention decorations, scene changes, props, lighting, outfits, weather effects, or objects changing.\n\n" +
            "Choose a theme similar to these examples:\n" +
            "- Sleepy Monday Rush\n" +
            "- Cozy Study Session\n" +
            "- Tiny Celebration Day\n" +
            "- Treat Yourself Tuesday\n" +
            "- Chaotic Espresso Hour\n" +
            "- Pastry Panic\n" +
            "- Caffeine Emergency\n" +
            "- Fancy Furball Day\n" +
            "- Grumpy but Lovable Day\n" +
            "- Drama Club Cafe\n\n" +
            "Return only JSON in this exact format:\n" +
            "{\n" +
            "  \"themeName\": \"\",\n" +
            "  \"themeDescription\": \"\",\n" +
            "  \"mood\": \"\"\n" +
            "}";

        yield return StartCoroutine(SendGeminiRequest(
            prompt,
            "{\"themeName\":\"Cozy Cafe Day\",\"themeDescription\":\"Customers are friendly, cozy, and excited for drinks and pastries.\",\"mood\":\"warm, cheerful, relaxed\"}",
            CleanJson,
            IsUsableJson,
            text =>
            {
                CafeTheme theme = JsonUtility.FromJson<CafeTheme>(text);

                if (theme == null || string.IsNullOrWhiteSpace(theme.themeName))
                {
                    theme = new CafeTheme
                    {
                        themeName = "Cozy Cafe Day",
                        themeDescription = "Customers are friendly, cozy, and excited for drinks and pastries.",
                        mood = "warm, cheerful, relaxed"
                    };
                }

                onComplete?.Invoke(theme);
            }
        ));
    }

    public IEnumerator GenerateCustomerProfile(CafeTheme theme, Order order, Action<CustomerProfile> onComplete)
    {
        if (order == null)
        {
            onComplete?.Invoke(null);
            yield break;
        }

        string syrupText = order.syrup == SyrupType.None ? "no syrup" : order.syrup + " syrup";

        string themeName = theme != null ? theme.themeName : "Cozy Cafe Day";
        string themeDescription = theme != null ? theme.themeDescription : "Customers are friendly and cozy.";
        string mood = theme != null ? theme.mood : "warm, cheerful";

        string prompt =
            "Generate a customer profile for a cozy animal cafe game called Pawristas.\n\n" +
            "Daily cafe theme:\n" +
            themeName + " - " + themeDescription + "\n" +
            "Mood: " + mood + "\n\n" +
            "Customer order:\n" +
            "Drink: " + order.drinkType + "\n" +
            "Espresso shots: " + order.espressoShots + "\n" +
            "Syrup: " + syrupText + "\n" +
            "Pastry: " + order.pastry + "\n\n" +
            "Return only JSON in this exact format:\n" +
            "{\n" +
            "  \"name\": \"\",\n" +
            "  \"animalType\": \"\",\n" +
            "  \"personality\": \"\",\n" +
            "  \"speakingStyle\": \"\",\n" +
            "  \"orderDialogue\": \"\"\n" +
            "}\n\n" +
            "Rules:\n" +
            "- The orderDialogue must clearly include the exact drink, espresso shots, syrup, and pastry.\n" +
            "- Keep orderDialogue under 25 words.\n" +
            "- Match the daily theme through wording and personality only.\n" +
            "- Do not mention decorations, props, lighting, costumes, weather effects, or scene changes.\n" +
            "- Do not add extra order items.\n" +
            "- Use a playful animal cafe name.";

        string fallbackJson =
            "{\"name\":\"" + order.customerName + "\"," +
            "\"animalType\":\"cat\"," +
            "\"personality\":\"friendly and cozy\"," +
            "\"speakingStyle\":\"polite and cheerful\"," +
            "\"orderDialogue\":\"" + GetFallbackOrderDialogue(order).Replace("\"", "") + "\"}";

        yield return StartCoroutine(SendGeminiRequest(
            prompt,
            fallbackJson,
            CleanJson,
            IsUsableJson,
            text =>
            {
                CustomerProfile profile = JsonUtility.FromJson<CustomerProfile>(text);

                if (profile == null || string.IsNullOrWhiteSpace(profile.orderDialogue))
                {
                    profile = new CustomerProfile
                    {
                        name = order.customerName,
                        animalType = "cat",
                        personality = "friendly and cozy",
                        speakingStyle = "polite and cheerful",
                        orderDialogue = GetFallbackOrderDialogue(order)
                    };
                }

                onComplete?.Invoke(profile);
            }
        ));
    }
}