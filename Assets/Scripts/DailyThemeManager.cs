using System.Collections;
using UnityEngine;
using TMPro;

public class DailyThemeManager : MonoBehaviour
{
    [Header("Gemini")]
    [SerializeField] private GeminiDialogueClient geminiDialogueClient;

    [Header("Theme UI")]
    [SerializeField] private GameObject themePanel;
    [SerializeField] private TextMeshProUGUI themeText;
    [SerializeField] private float displayTime = 4f;

    private IEnumerator Start()
    {
        if (GameSessionManager.Instance == null)
        {
            Debug.LogWarning("DailyThemeManager: GameSessionManager not found.");
            yield break;
        }

        if (GameSessionManager.Instance.HasShownDailyTheme)
        {
            if (themePanel != null)
            {
                themePanel.SetActive(false);
            }

            yield break;
        }

        if (geminiDialogueClient == null)
        {
            geminiDialogueClient = FindFirstObjectByType<GeminiDialogueClient>();
        }

        if (geminiDialogueClient == null)
        {
            Debug.LogWarning("DailyThemeManager: GeminiDialogueClient not found.");
            yield break;
        }

        if (GameSessionManager.Instance.CurrentCafeTheme == null)
        {
            yield return StartCoroutine(
                geminiDialogueClient.GenerateDailyTheme(theme =>
                {
                    GameSessionManager.Instance.SetCafeTheme(theme);
                })
            );
        }

        CafeTheme currentTheme = GameSessionManager.Instance.CurrentCafeTheme;

        if (themeText != null && currentTheme != null)
        {
            themeText.text =
                "Today's Cafe Theme:\n" +
                currentTheme.themeName +
                "\n\n" +
                currentTheme.themeDescription;
        }

        if (themePanel != null)
        {
            themePanel.SetActive(true);
            yield return new WaitForSeconds(displayTime);
            themePanel.SetActive(false);
        }

        GameSessionManager.Instance.MarkDailyThemeShown();
    }

}