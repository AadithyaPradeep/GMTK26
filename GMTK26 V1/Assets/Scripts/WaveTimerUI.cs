using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wave countdown + game-over text (Pixelon SDF).
/// </summary>
public class WaveTimerUI : MonoBehaviour
{
    [SerializeField] private ChickenSpawner spawner;
    [SerializeField] private TMP_FontAsset pixelonFont;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float fontSize = 42f;
    [SerializeField] private float gameOverFontSize = 72f;
    [SerializeField] private float hintFontSize = 36f;
    [SerializeField] private Color textColor = Color.white;

    [SerializeField] private string waveFormat = "WAVE {0}\n{1}";
    [SerializeField] private string gameOverText = "GAME OVER\nAll chickens lost";
    [SerializeField] private string laserLostGameOverText = "GAME OVER\nLaser chicken lost";
    [SerializeField] private string finishedText = "FINISHED";

    private TextMeshProUGUI hintLabel;
    private Coroutine hintRoutine;
    private string activeGameOverText;
    private bool showingFinished;

    private void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<ChickenSpawner>();

        if (label == null)
            label = CreateLabel();

        activeGameOverText = gameOverText;
        ApplyFont(label, fontSize);
    }

    private void Update()
    {
        if (label == null || spawner == null)
            return;

        if (spawner.IsGameOver)
        {
            ApplyFont(label, gameOverFontSize);
            label.text = string.IsNullOrEmpty(activeGameOverText) ? gameOverText : activeGameOverText;
            label.enabled = true;
            ClearHint();
            return;
        }

        if (spawner.IsFinished || showingFinished)
        {
            ApplyFont(label, gameOverFontSize);
            label.text = finishedText;
            label.enabled = true;
            return;
        }

        ApplyFont(label, fontSize);

        if (spawner.IsWaitingForNextWave)
        {
            int seconds = Mathf.CeilToInt(spawner.SecondsUntilNextWave);
            label.text = string.Format(waveFormat, spawner.CurrentWave, seconds);
            label.enabled = true;
        }
        else
        {
            label.text = string.Empty;
            label.enabled = false;
        }
    }

    public void ShowFinished()
    {
        showingFinished = true;
        ClearHint();
        if (label == null)
            return;

        ApplyFont(label, gameOverFontSize);
        label.text = finishedText;
        label.enabled = true;
    }

    /// <summary>Centered Pixelon hint. Pass durationSeconds &lt; 0 to keep until ClearHint.</summary>
    public void ShowHint(string message, float durationSeconds = 5f)
    {
        EnsureHintLabel();
        if (hintLabel == null)
            return;

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        ApplyFont(hintLabel, hintFontSize);
        hintLabel.text = message;
        hintLabel.enabled = true;

        // Pin near the top of the screen.
        RectTransform rt = hintLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -100f);
        rt.sizeDelta = new Vector2(1200f, 120f);

        if (durationSeconds < 0f)
        {
            hintRoutine = null;
            return;
        }

        hintRoutine = StartCoroutine(HideHintAfter(durationSeconds));
    }

    public void SetLaserLostGameOver()
    {
        activeGameOverText = laserLostGameOverText;
    }

    public void ClearHint()
    {
        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
        }

        if (hintLabel != null)
        {
            hintLabel.text = string.Empty;
            hintLabel.enabled = false;
        }
    }

    private IEnumerator HideHintAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ClearHint();
    }

    private void ApplyFont(TextMeshProUGUI tmp, float size)
    {
        if (tmp == null || pixelonFont == null)
            return;

        tmp.font = pixelonFont;
        tmp.fontSharedMaterial = pixelonFont.material;
        tmp.fontSize = size;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void EnsureHintLabel()
    {
        if (hintLabel != null)
            return;

        Transform canvasTf = label != null ? label.transform.parent : null;
        if (canvasTf == null)
        {
            CreateLabel();
            canvasTf = label.transform.parent;
        }

        GameObject hintGo = new GameObject("BossHintText");
        hintGo.transform.SetParent(canvasTf, false);

        hintLabel = hintGo.AddComponent<TextMeshProUGUI>();
        RectTransform rt = hintLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -100f);
        rt.sizeDelta = new Vector2(1200f, 120f);
        hintLabel.raycastTarget = false;
        hintLabel.enabled = false;
    }

    private TextMeshProUGUI CreateLabel()
    {
        GameObject canvasGo = new GameObject("WaveTimerCanvas");
        canvasGo.transform.SetParent(null);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject textGo = new GameObject("WaveTimerText");
        textGo.transform.SetParent(canvasGo.transform, false);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -36f);
        rt.sizeDelta = new Vector2(900f, 180f);

        tmp.raycastTarget = false;
        return tmp;
    }
}
