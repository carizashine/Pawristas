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

    [SerializeField] private AudioSource cupSpawnAudio;

    [Header("Quality Reveal Pan")]
    [Tooltip("Camera that pans down to reveal the quality. If null, Camera.main is used.")]
    [SerializeField] private Camera revealCamera;

    [Tooltip("Where the camera ends up when looking down at the cup. Set this above the cup.")]
    [SerializeField] private Transform panTarget;

    [Tooltip("Renderer of the disc using the EspressoQuality shader. Its material's _Quality property gets driven by the score.")]
    [SerializeField] private Renderer qualityDiscRenderer;

    [Tooltip("Float property name on the shader.")]
    [SerializeField] private string qualityShaderProperty = "_Quality";

    [Tooltip("Pause after the last shot before the camera starts panning.")]
    [SerializeField] private float panStartDelay = 0.7f;

    [Tooltip("How many seconds the pan + rotation takes.")]
    [SerializeField] private float panDuration = 1.6f;

    [Header("Timing")]
    [SerializeField] private float nextShotDelay = 0.7f;
    [Tooltip("Legacy fallback — only used when no Pan Target is assigned.")]
    [SerializeField] private float returnToCafeDelay = 1.4f;
    [SerializeField] private float dropAnimationTime = 0.35f;
    [Header("Audio")]
    [SerializeField] private AudioSource successAudio;
    [SerializeField] private AudioSource failAudio;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI shotProgressText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private bool canClick = true;
    private bool shouldMovePuck = true;
    private bool minigameFinished = false;
    private bool awaitingFinalClick = false;
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

        // Hide the quality disc until the cup spawns at the end of the minigame.
        if (qualityDiscRenderer != null)
        {
            qualityDiscRenderer.gameObject.SetActive(false);
        }

        Debug.Log("Espresso timing game started. Required shots: " + requiredShots);
    }

    private void Update()
    {
        if (minigameFinished)
        {
            // After the pan completes we wait for a click to leave the scene.
            if (awaitingFinalClick && Input.GetMouseButtonDown(0))
            {
                awaitingFinalClick = false;
                LoadCafe();
            }
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
            if (successAudio != null)
            {
                successAudio.Play();
            }

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
        if (failAudio != null)
        {
            failAudio.Play();
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

        // If the user has set up the pan target + quality disc, do the reveal.
        // Otherwise fall back to the original auto-load-cafe behavior.
        if (panTarget != null)
        {
            StartCoroutine(PanAndRevealQuality());
        }
        else
        {
            StartCoroutine(ReturnToCafeAfterDelay());
        }
    }

    private IEnumerator PanAndRevealQuality()
    {
        yield return new WaitForSeconds(panStartDelay);

        // Drive the shader with our 0..1 quality score.
        ApplyQualityToShader();

        if (revealCamera == null)
        {
            revealCamera = Camera.main;
        }

        if (revealCamera != null && panTarget != null)
        {
            Vector3 startPos    = revealCamera.transform.position;
            Quaternion startRot = revealCamera.transform.rotation;

            Vector3 endPos    = panTarget.position;
            // Look straight down (X = 90), keep current Y/Z so framing feels consistent.
            Quaternion endRot = Quaternion.Euler(
                90f,
                revealCamera.transform.eulerAngles.y,
                0f
            );

            float elapsed = 0f;
            while (elapsed < panDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);

                revealCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                revealCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

                yield return null;
            }

            revealCamera.transform.position = endPos;
            revealCamera.transform.rotation = endRot;
        }

        // Update UI to show final result and prompt for click.
        if (instructionText != null)
        {
            instructionText.text = GetQualityLabel() + " espresso — click to continue.";
        }

        if (shotProgressText != null)
        {
            shotProgressText.text = "";
        }

        awaitingFinalClick = true;
    }

    private void ApplyQualityToShader()
    {
        if (qualityDiscRenderer == null)
        {
            Debug.LogWarning("EspressoMiniGameController: Quality Disc Renderer not assigned.");
            return;
        }

        float quality = requiredShots > 0
            ? (float)successfulShots / requiredShots
            : 0f;

        // .material gives us a per-instance copy so we don't mutate the shared asset.
        qualityDiscRenderer.material.SetFloat(qualityShaderProperty, quality);

        Debug.Log("Applied quality " + quality + " to espresso disc shader.");
    }

    private string GetQualityLabel()
    {
        float q = requiredShots > 0 ? (float)successfulShots / requiredShots : 0f;

        if (q >= 0.75f) return "Great";
        if (q >= 0.4f)  return "Alright";
        return "Bad";
    }

    private void LoadCafe()
    {
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.GoToCafe();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cafe");
        }
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

        if (cupSpawnAudio != null)
        {
            cupSpawnAudio.Play();
        }

        // Reveal the quality disc now that the cup is in the scene.
        if (qualityDiscRenderer != null)
        {
            qualityDiscRenderer.gameObject.SetActive(true);
        }
    }

    private IEnumerator ReturnToCafeAfterDelay()
    {
        yield return new WaitForSeconds(returnToCafeDelay);
        LoadCafe();
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