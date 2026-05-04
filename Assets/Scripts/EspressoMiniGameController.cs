using System.Collections;
using UnityEngine;
using TMPro;

public class EspressoMiniGameController : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private int requiredShots = 1;
    [SerializeField] private int currentShotNumber = 1;
    [SerializeField] private int successfulShots = 0;

    [Header("Moving Puck")]
    [SerializeField] private Transform movingPuck;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private float moveSpeed = 4f;

    [Header("Target Zone")]
    [SerializeField] private Transform perfectZonePoint;
    [SerializeField] private Transform successDropPoint;

    [Tooltip("How close the puck must be to the perfect zone. Smaller = stricter.")]
    [SerializeField] private float successRadius = 0.12f;

    [Header("Espresso Cup")]
    [SerializeField] private GameObject espressoCupPrefab;
    [SerializeField] private Transform espressoCupSpawnPoint;

    [Header("Timing")]
    [SerializeField] private float nextShotDelay = 0.7f;
    [SerializeField] private float returnToCafeDelay = 1.4f;
    [SerializeField] private float dropAnimationTime = 0.35f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI shotProgressText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private bool canClick = true;
    private bool shouldMovePuck = true;
    private bool minigameFinished = false;
    private float movementTimer = 0f;

    private void Start()
    {
        SetupRequiredShots();

        currentShotNumber = 1;
        successfulShots = 0;

        canClick = true;
        shouldMovePuck = true;
        minigameFinished = false;

        if (instructionText != null)
        {
            instructionText.text = "Click when the espresso puck is directly over the target.";
        }

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        ResetPuckToStart();
        UpdateUI();

        Debug.Log("Espresso timing game started. Required shots: " + requiredShots);
    }

    private void Update()
    {
        if (minigameFinished)
        {
            return;
        }

        if (shouldMovePuck)
        {
            MovePuck();
        }

        if (canClick && Input.GetMouseButtonDown(0))
        {
            TryDropPuck();
        }
    }

    private void SetupRequiredShots()
    {
        if (GameSessionManager.Instance != null &&
            GameSessionManager.Instance.CurrentOrder != null)
        {
            requiredShots = GameSessionManager.Instance.CurrentOrder.espressoShots;
        }

        requiredShots = Mathf.Clamp(requiredShots, 1, 4);
    }

    private void MovePuck()
    {
        if (movingPuck == null || leftPoint == null || rightPoint == null)
        {
            Debug.LogWarning(
                "Espresso timing minigame cannot move puck. " +
                "MovingPuck: " + movingPuck +
                ", LeftPoint: " + leftPoint +
                ", RightPoint: " + rightPoint
            );
            return;
        }

        movementTimer += Time.deltaTime * moveSpeed;

        float t = Mathf.PingPong(movementTimer, 1f);

        movingPuck.position = Vector3.Lerp(
            leftPoint.position,
            rightPoint.position,
            t
        );
    }

    private void TryDropPuck()
    {
        if (movingPuck == null || perfectZonePoint == null)
        {
            Debug.LogWarning("EspressoMiniGameController: Moving puck or target zone is missing.");
            return;
        }

        canClick = false;
        shouldMovePuck = false;

        float distance = Vector3.Distance(
            movingPuck.position,
            perfectZonePoint.position
        );

        bool landed = distance <= successRadius;

        Debug.Log("Shot attempt distance from perfect zone: " + distance + " / required: " + successRadius);

        if (landed)
        {
            successfulShots++;

            if (feedbackText != null)
            {
                feedbackText.text = "Perfect shot!";
            }

            Debug.Log("Espresso shot landed. Score: " + successfulShots + " / " + requiredShots);

            UpdateUI();

            StartCoroutine(AnimatePuckDropThenContinue());
            return;
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Missed!";
        }

        Debug.Log("Espresso shot missed. Score: " + successfulShots + " / " + requiredShots);

        UpdateUI();

        StartCoroutine(GoToNextShotOrFinish());
    }

    private IEnumerator AnimatePuckDropThenContinue()
    {
        if (movingPuck == null || successDropPoint == null)
        {
            yield return GoToNextShotOrFinish();
            yield break;
        }

        Vector3 startPosition = movingPuck.position;
        Vector3 endPosition = successDropPoint.position;

        float elapsed = 0f;

        while (elapsed < dropAnimationTime)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / dropAnimationTime;
            t = Mathf.SmoothStep(0f, 1f, t);

            movingPuck.position = Vector3.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        movingPuck.position = endPosition;

        yield return GoToNextShotOrFinish();
    }

    private IEnumerator GoToNextShotOrFinish()
    {
        yield return new WaitForSeconds(nextShotDelay);

        if (currentShotNumber >= requiredShots)
        {
            FinishEspressoMinigame();
            yield break;
        }

        currentShotNumber++;

        ResetPuckToStart();

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        canClick = true;
        shouldMovePuck = true;

        UpdateUI();
    }

    private void ResetPuckToStart()
    {
        movementTimer = 0f;

        if (movingPuck != null && leftPoint != null)
        {
            movingPuck.position = leftPoint.position;
        }
    }

    private void FinishEspressoMinigame()
    {
        minigameFinished = true;
        canClick = false;
        shouldMovePuck = false;

        if (movingPuck != null)
        {
            movingPuck.gameObject.SetActive(false);
        }

        SpawnEspressoCup();

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.SaveEspressoResult(successfulShots, requiredShots);
        }
        else
        {
            Debug.LogWarning("EspressoMiniGameController: GameSessionManager not found.");
        }

        if (OrderProgressTracker.Instance != null)
        {
            OrderProgressTracker.Instance.MarkEspressoComplete();
        }
        else
        {
            Debug.LogWarning("EspressoMiniGameController: OrderProgressTracker not found.");
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Espresso ready! Score: " + successfulShots + " / " + requiredShots;
        }

        UpdateUI();

        StartCoroutine(ReturnToCafeAfterDelay());
    }

    private void SpawnEspressoCup()
    {
        if (espressoCupPrefab == null || espressoCupSpawnPoint == null)
        {
            Debug.LogWarning("EspressoMiniGameController: Espresso cup prefab or spawn point is missing.");
            return;
        }

        Instantiate(
            espressoCupPrefab,
            espressoCupSpawnPoint.position,
            espressoCupSpawnPoint.rotation
        );
    }

    private IEnumerator ReturnToCafeAfterDelay()
    {
        yield return new WaitForSeconds(returnToCafeDelay);

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.GoToCafe();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cafe");
        }
    }

    private void UpdateUI()
    {
        if (shotProgressText != null)
        {
            shotProgressText.text = "Shot: " + currentShotNumber + " / " + requiredShots;
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + successfulShots + " / " + requiredShots;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (perfectZonePoint != null)
        {
            Gizmos.DrawWireSphere(perfectZonePoint.position, successRadius);
        }
    }
}