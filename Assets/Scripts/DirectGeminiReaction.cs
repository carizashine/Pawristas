using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DirectGeminiReactionClient : MonoBehaviour
{
    [Header("Gemini")]
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
        public float temperature = 0.7f;
        public int maxOutputTokens = 120;
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

    public IEnumerator GetReaction(
        Order order,
        PlayerOrderResult result,
        int finalScore,
        Action<string> onComplete
    )
    {
        if (string.IsNullOrWhiteSpace(apiKey) ||
            apiKey == "PASTE_YOUR_GEMINI_API_KEY_HERE")
        {
            Debug.LogWarning("Gemini reaction client: API key is missing.");
            onComplete?.Invoke(GetFallbackReaction(finalScore));
            yield break;
        }

        string prompt = BuildPrompt(order, result, finalScore);

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
                temperature = 0.7f,
                maxOutputTokens = 120,
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
                "Direct Gemini request failed: " +
                request.error +
                "\nResponse: " +
                request.downloadHandler.text
            );

            onComplete?.Invoke(GetFallbackReaction(finalScore));
            yield break;
        }

        Debug.Log("Gemini raw response: " + request.downloadHandler.text);

        string reaction = TryExtractText(request.downloadHandler.text);
        reaction = CleanReaction(reaction);

        if (!IsUsableReaction(reaction))
        {
            Debug.LogWarning("Gemini returned unusable reaction: " + reaction);
            reaction = GetFallbackReaction(finalScore);
        }

        onComplete?.Invoke(reaction);
    }

    private string BuildPrompt(Order order, PlayerOrderResult result, int finalScore)
    {
        if (order == null || result == null)
        {
            return "Write one short cute customer reaction for a cozy animal cafe game.";
        }

        string mood =
            finalScore >= 90 ? "very happy" :
            finalScore >= 70 ? "happy" :
            finalScore >= 40 ? "polite but disappointed" :
            "sad";

        return
            "You are writing dialogue for a cozy animal cafe game called Pawristas.\n" +
            "Return only two complete customer sentence.\n" +
            "The sentence must be 8 to 15 words.\n" +
            "Cute, polite, cafe-themed.\n" +
            "No quotation marks. No labels. No bullet points.\n" +
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

    private string CleanReaction(string reaction)
    {
        if (string.IsNullOrWhiteSpace(reaction))
        {
            return "";
        }

        reaction = reaction.Trim();

        reaction = reaction.Replace("\"", "");
        reaction = reaction.Replace("“", "");
        reaction = reaction.Replace("”", "");

        reaction = reaction.Replace("Customer reaction:", "");
        reaction = reaction.Replace("Reaction:", "");
        reaction = reaction.Trim();

        int newlineIndex = reaction.IndexOf('\n');
        if (newlineIndex >= 0)
        {
            reaction = reaction.Substring(0, newlineIndex).Trim();
        }

        return reaction;
    }

    private bool IsUsableReaction(string reaction)
    {
        if (string.IsNullOrWhiteSpace(reaction))
        {
            return false;
        }

        string trimmed = reaction.Trim();

        if (trimmed.Length < 12)
        {
            return false;
        }

        string[] words = trimmed.Split(
            new char[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (words.Length < 5)
        {
            return false;
        }

        if (trimmed == "Oh," ||
            trimmed == "Oh." ||
            trimmed == "Oh dear," ||
            trimmed == "Hmm," ||
            trimmed == "Hmm.")
        {
            return false;
        }

        return true;
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
            return "Hmm... this is okay, but something is off.";
        }

        return "I do not think this is what I ordered.";
    }
}