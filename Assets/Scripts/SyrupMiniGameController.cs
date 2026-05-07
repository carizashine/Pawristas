using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SyrupMiniGameController : MonoBehaviour
{
    [Header("Jar")]
    [SerializeField] private Transform jarTransform;
    [SerializeField] private Renderer jarRenderer;

    [Tooltip("Which material slot on the jar renderer to tint with the syrup color. -1 tints every material slot. Use a specific index (0, 1, 2...) when your jar has multiple materials and only the body should change color.")]
    [SerializeField] private int jarMaterialIndex = -1;

    [Tooltip("How strongly the syrup color stains the jar. 0 = jar keeps its original look, 1 = full replace (old behavior). Use a partial value (~0.3-0.5) when the jar uses a textured material so the design shows through.")]
    [Range(0f, 1f)]
    [SerializeField] private float jarTintStrength = 0.5f;

    [SerializeField] private Transform pourPoint;
    [SerializeField] private Transform streamLeftPoint;
    [SerializeField] private Transform streamRightPoint;

    [Tooltip("Starting jar oscillation speed.")]
    [SerializeField] private float streamSpeed = 1.5f;

    [Tooltip("Lower bound for the random speed re-roll after each burst.")]
    [SerializeField] private float streamSpeedMin = 0.6f;

    [Tooltip("Upper bound for the random speed re-roll after each burst.")]
    [SerializeField] private float streamSpeedMax = 2.6f;

    [Header("Drop Burst")]
    [Tooltip("Seconds between each burst of drops.")]
    [SerializeField] private float burstInterval = 1.2f;

    [Tooltip("Minimum drops per burst.")]
    [SerializeField] private int burstMin = 3;

    [Tooltip("Maximum drops per burst.")]
    [SerializeField] private int burstMax = 8;

    [Tooltip("How fast drops fall.")]
    [SerializeField] private float dropFallSpeed = 0.4f;

    [Tooltip("Random Z scatter per drop within a burst.")]
    [SerializeField] private float dropSpread = 0.04f;

    [Header("Drop Appearance")]
    [Tooltip("Width (X/Z scale) of each drop.")]
    [SerializeField] private float dropWidth = 0.06f;

    [Tooltip("Height (Y scale) of each drop. Larger than width gives the elongated liquid look.")]
    [SerializeField] private float dropHeight = 0.15f;

    [Tooltip("Material applied to each spawned drop. Assign one using the Custom/SyrupDrop shader for the shiny look. If null, the default sphere material is used.")]
    [SerializeField] private Material dropMaterial;

    [Header("Cup & Fill")]
    [SerializeField] private Transform cupTransform;
    [SerializeField] private Transform fillTransform;
    [SerializeField] private Renderer fillRenderer;
    [Header("Audio")]
    [SerializeField] private AudioSource dropCaughtAudio;

    [Tooltip("How close (Z axis) a drop must be to the cup center to count as caught.")]
    [SerializeField] private float catchRadius = 0.65f;

    [Tooltip("Y offset above the cup pivot where drops are considered caught. Set this to the cup's rim height so drops disappear AT the rim instead of falling all the way to the cup's center.")]
    [SerializeField] private float catchYOffset = 0.3f;

    [Tooltip("Fill added per caught drop.")]
    [SerializeField] private float fillPerDrop = 0.025f;

    [Tooltip("Fill level (0-1) that completes the minigame before timer runs out.")]
    [SerializeField] private float targetFill = 0.75f;

    [Tooltip("Y scale of FillObject at 100% fill.")]
    [SerializeField] private float fillMaxScaleY = 1f;

    [Tooltip("Local Y of the bottom of the cup interior.")]
    [SerializeField] private float fillBaseY = 0f;

    [Header("Pre-game Countdown")]
    [Tooltip("Seconds of '3..2..1..GO!' before drops start spawning.")]
    [SerializeField] private float countdownDuration = 3f;

    [Tooltip("How long 'GO!' lingers on screen before clearing.")]
    [SerializeField] private float goLingerDuration = 0.5f;

    [Tooltip("Big text element used for the countdown and 'GO!' message.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Timer")]
    [Tooltip("Total seconds of spawning. Game ends once this expires AND all drops have landed.")]
    [SerializeField] private float timerDuration = 30f;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timing")]
    [SerializeField] private float returnToCafeDelay = 1.5f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI syrupNameText;

    // ── Runtime state ──────────────────────────────────────────────────────
    private float currentFill = 0f;
    private float streamTimer = 0f;
    private float burstTimer = 0f;
    private float timeRemaining;
    private bool inCountdown = true;
    private float countdownTimer;
    private bool spawningComplete = false;  // True once timer hits 0
    private bool gameComplete = false;
    private SyrupType currentSyrup;
    private Color syrupColor;
    private int totalSpawned = 0;
    private int totalCaught = 0;
    private float currentStreamSpeed;       // Re-rolled after each burst.

    private readonly List<ActiveDrop> activeDrops = new List<ActiveDrop>();

    private class ActiveDrop
    {
        public Transform dropTransform;
        public float lifetime;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Start()
    {
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.CurrentOrder != null)
            currentSyrup = GameSessionManager.Instance.CurrentOrder.syrup;
        else
            currentSyrup = SyrupType.Caramel;

        syrupColor = GetSyrupColor(currentSyrup);
        timeRemaining = timerDuration;

        countdownTimer = countdownDuration;
        inCountdown = countdownDuration > 0f;

        currentStreamSpeed = streamSpeed;

        TintJar();

        if (fillRenderer != null)
            fillRenderer.material.color = syrupColor;

        if (fillTransform != null)
            fillTransform.localScale = new Vector3(
                fillTransform.localScale.x,
                0.001f,
                fillTransform.localScale.z
            );

        if (instructionText != null)
            instructionText.text = inCountdown
                ? "Get ready..."
                : "A / D  —  Move the cup to catch the " + GetSyrupDisplayName(currentSyrup) + " syrup!";

        if (feedbackText != null)
            feedbackText.text = "";

        if (syrupNameText != null)
            syrupNameText.text = GetSyrupDisplayName(currentSyrup) + " Syrup";

        if (countdownText != null)
            countdownText.text = inCountdown ? Mathf.CeilToInt(countdownTimer).ToString() : "";

        RefreshTimerUI();
    }

    private void Update()
    {
        if (gameComplete) return;

        if (inCountdown)
        {
            TickCountdown();
            return;
        }

        MoveJar();

        if (!spawningComplete)
        {
            TickTimer();
            TryBurst();
        }

        UpdateDrops();
    }

    // ── Pre-game countdown ─────────────────────────────────────────────────
    private void TickCountdown()
    {
        countdownTimer -= Time.deltaTime;

        if (countdownText != null)
        {
            if (countdownTimer > 0f)
                countdownText.text = Mathf.CeilToInt(countdownTimer).ToString();
            else
                countdownText.text = "GO!";
        }

        // Linger on "GO!" for a beat, then start the game.
        if (countdownTimer <= -goLingerDuration)
        {
            inCountdown = false;

            if (countdownText != null)
                countdownText.text = "";

            if (instructionText != null)
                instructionText.text = "A / D  —  Move the cup to catch the " + GetSyrupDisplayName(currentSyrup) + " syrup!";
        }
    }

    private void OnDestroy()
    {
        CleanupDrops();
    }

    // ── Jar oscillation ────────────────────────────────────────────────────
    private void MoveJar()
    {
        if (jarTransform == null || streamLeftPoint == null || streamRightPoint == null) return;

        streamTimer += Time.deltaTime * currentStreamSpeed;
        float t = Mathf.PingPong(streamTimer, 1f);
        jarTransform.position = Vector3.Lerp(streamLeftPoint.position, streamRightPoint.position, t);
    }

    // ── Timer ──────────────────────────────────────────────────────────────
    private void TickTimer()
    {
        timeRemaining = Mathf.Max(timeRemaining - Time.deltaTime, 0f);
        RefreshTimerUI();

        if (timeRemaining <= 0f)
        {
            spawningComplete = true;

            if (timerText != null)
                timerText.text = "";

            // If no drops are still in the air, end immediately.
            if (activeDrops.Count == 0)
                FinishMinigame();
        }
    }

    private void RefreshTimerUI()
    {
        if (timerText == null) return;
        timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining);
        timerText.color = timeRemaining <= 5f ? Color.red : Color.white;
    }

    // ── Burst spawning ─────────────────────────────────────────────────────
    private void TryBurst()
    {
        burstTimer -= Time.deltaTime;
        if (burstTimer > 0f) return;
        burstTimer = burstInterval;
        SpawnBurst();
    }

    private void SpawnBurst()
    {
        Vector3 origin = pourPoint != null
            ? pourPoint.position
            : (jarTransform != null ? jarTransform.position : Vector3.zero);

        int count = Random.Range(burstMin, burstMax + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = origin;
            spawnPos.z += Random.Range(-dropSpread, dropSpread);

            // Sphere scaled taller than wide — elongated liquid drop shape.
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.transform.position = spawnPos;
            drop.transform.localScale = new Vector3(dropWidth, dropHeight, dropWidth);
            drop.name = "SyrupDrop";

            Destroy(drop.GetComponent<Collider>());

            Renderer r = drop.GetComponent<Renderer>();
            if (r != null)
            {
                // Prefer the user-assigned drop material (Custom/SyrupDrop) for the
                // shiny look. Fall back to the primitive's default material so the
                // game still works if no material is assigned.
                Material mat = dropMaterial != null
                    ? new Material(dropMaterial)
                    : new Material(r.sharedMaterial);

                float brightness = Random.Range(0.88f, 1.06f);
                Color c = syrupColor;
                Color tinted = new Color(
                    Mathf.Clamp01(c.r * brightness),
                    Mathf.Clamp01(c.g * brightness),
                    Mathf.Clamp01(c.b * brightness),
                    1f
                );

                // .color maps to _Color on both built-in Standard and our custom shader.
                mat.color = tinted;

                r.material = mat;
            }

            activeDrops.Add(new ActiveDrop
            {
                dropTransform = drop.transform,
                lifetime = 20f
            });

            totalSpawned++;
        }

        // Re-roll jar speed for variety so the dropper isn't predictable.
        // Clamp to avoid degenerate / negative speeds.
        float min = Mathf.Max(0.05f, streamSpeedMin);
        float max = Mathf.Max(min, streamSpeedMax);
        currentStreamSpeed = Random.Range(min, max);
    }

    // ── Drop update & catch ────────────────────────────────────────────────
    private void UpdateDrops()
    {
        if (cupTransform == null) return;

        // Catch line is at the cup pivot + offset (i.e. the rim, not the center).
        float catchY = cupTransform.position.y + catchYOffset;
        float cupZ   = cupTransform.position.z;

        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            ActiveDrop drop = activeDrops[i];

            if (drop.dropTransform == null)
            {
                activeDrops.RemoveAt(i);
                continue;
            }

            drop.dropTransform.position += Vector3.down * dropFallSpeed * Time.deltaTime;
            drop.lifetime -= Time.deltaTime;

            bool expired       = drop.lifetime <= 0f;
            bool reachedRim    = drop.dropTransform.position.y <= catchY;
            float distZ        = Mathf.Abs(drop.dropTransform.position.z - cupZ);
            bool inCatchRange  = reachedRim && distZ <= catchRadius;
            // If a drop misses laterally, let it keep falling visibly past the cup
            // before we destroy it — feels much better than vanishing at rim height.
            bool fellPastCup   = drop.dropTransform.position.y < catchY - catchRadius;

            if (inCatchRange)
            {
                // Caught — destroy immediately and credit the player.
                totalCaught++;
                currentFill = Mathf.Clamp01(currentFill + fillPerDrop);
                UpdateFillVisual();
                if (dropCaughtAudio != null)
                {
                    dropCaughtAudio.Play();
                }

                Destroy(drop.dropTransform.gameObject);
                activeDrops.RemoveAt(i);

                if (currentFill >= targetFill)
                {
                    FinishMinigame();
                    return;
                }

                if (spawningComplete && activeDrops.Count == 0)
                {
                    FinishMinigame();
                    return;
                }
            }
            else if (fellPastCup || expired)
            {
                // Missed — let it fall past visually, then despawn.
                Destroy(drop.dropTransform.gameObject);
                activeDrops.RemoveAt(i);

                if (spawningComplete && activeDrops.Count == 0)
                {
                    FinishMinigame();
                    return;
                }
            }
        }
    }

    // ── Fill visual ────────────────────────────────────────────────────────
    private void UpdateFillVisual()
    {
        if (fillTransform == null) return;

        float scaleY = currentFill * fillMaxScaleY;

        fillTransform.localScale = new Vector3(
            fillTransform.localScale.x,
            Mathf.Max(scaleY, 0.001f),
            fillTransform.localScale.z
        );

        Vector3 pos = fillTransform.localPosition;
        pos.y = fillBaseY + scaleY * 0.5f;
        fillTransform.localPosition = pos;
    }

    // ── Finish ─────────────────────────────────────────────────────────────
    private void FinishMinigame()
    {
        if (gameComplete) return;
        gameComplete = true;
        spawningComplete = true;
        CleanupDrops();

        float accuracy = totalSpawned > 0
            ? Mathf.Clamp01((float)totalCaught / totalSpawned)
            : 0f;

        if (feedbackText != null)
        {
            if (accuracy >= 0.85f)
                feedbackText.text = "Perfect pour!";
            else if (accuracy >= 0.5f)
                feedbackText.text = "Good pour!";
            else
                feedbackText.text = "Lots of syrup spilled!";
        }

        if (timerText != null)
            timerText.text = "";

        Debug.Log("Syrup done. Caught " + totalCaught + "/" + totalSpawned + ". Accuracy: " + accuracy);

        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.SaveSyrupResult(currentSyrup, accuracy);
        else
            Debug.LogWarning("SyrupMiniGameController: GameSessionManager not found.");

        if (OrderProgressTracker.Instance != null)
            OrderProgressTracker.Instance.MarkSyrupComplete();
        else
            Debug.LogWarning("SyrupMiniGameController: OrderProgressTracker not found.");

        StartCoroutine(ReturnToCafeAfterDelay());
    }

    private void CleanupDrops()
    {
        for (int i = activeDrops.Count - 1; i >= 0; i--)
            if (activeDrops[i].dropTransform != null)
                Destroy(activeDrops[i].dropTransform.gameObject);
        activeDrops.Clear();
    }

    private IEnumerator ReturnToCafeAfterDelay()
    {
        yield return new WaitForSeconds(returnToCafeDelay);

        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.GoToCafe();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cafe");
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private void TintJar()
    {
        if (jarRenderer == null) return;

        // Blend white with the syrup color so a textured base map keeps its
        // darker design (cap, accents, ring) while picking up a hue from syrup.
        // tintStrength = 0  → fully white (no tint, multiplied texture stays as-is)
        // tintStrength = 1  → fully syrup color (old replace behavior)
        Color tinted = Color.Lerp(Color.white, syrupColor, jarTintStrength);

        // .materials returns per-instance copies, so we don't mutate the shared
        // asset on disk. Mutating these is safe.
        Material[] mats = jarRenderer.materials;

        if (jarMaterialIndex < 0)
        {
            // Tint every material slot.
            for (int i = 0; i < mats.Length; i++)
            {
                ApplyTintColor(mats[i], tinted);
            }
        }
        else if (jarMaterialIndex < mats.Length)
        {
            // Tint only the chosen slot — leaves cap, accents, etc. untouched.
            ApplyTintColor(mats[jarMaterialIndex], tinted);
        }
        else
        {
            Debug.LogWarning(
                "SyrupMiniGameController: jarMaterialIndex " + jarMaterialIndex +
                " is out of range. Renderer has " + mats.Length + " material(s)."
            );
        }

        jarRenderer.materials = mats;
    }

    // URP/Lit uses _BaseColor; built-in Standard uses _Color; a few stylized
    // shaders use _MainColor. Set whichever ones the material exposes so the
    // tint always lands.
    private static void ApplyTintColor(Material mat, Color color)
    {
        if (mat == null) return;

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }

        if (mat.HasProperty("_MainColor"))
        {
            mat.SetColor("_MainColor", color);
        }
    }

    private Color GetSyrupColor(SyrupType syrup)
    {
        switch (syrup)
        {
            case SyrupType.Vanilla:   return new Color(1.00f, 0.95f, 0.70f);
            case SyrupType.Caramel:   return new Color(0.82f, 0.52f, 0.10f);
            case SyrupType.Hazelnut:  return new Color(0.52f, 0.32f, 0.08f);
            case SyrupType.Chocolate: return new Color(0.28f, 0.14f, 0.04f);
            default:                  return Color.gray;
        }
    }

    private string GetSyrupDisplayName(SyrupType syrup)
    {
        switch (syrup)
        {
            case SyrupType.Vanilla:   return "Vanilla";
            case SyrupType.Caramel:   return "Caramel";
            case SyrupType.Hazelnut:  return "Hazelnut";
            case SyrupType.Chocolate: return "Chocolate";
            default:                  return "Plain";
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cupTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cupTransform.position, catchRadius);
        }
        if (streamLeftPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(streamLeftPoint.position, 0.1f);
        }
        if (streamRightPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(streamRightPoint.position, 0.1f);
        }
    }
}
