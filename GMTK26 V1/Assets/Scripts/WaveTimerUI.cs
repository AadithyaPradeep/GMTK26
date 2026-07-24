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
    [SerializeField] private Color textColor = Color.white;

    [SerializeField] private string waveFormat = "WAVE {0}\n{1}";
    [SerializeField] private string gameOverText = "GAME OVER\nAll chickens lost";

    private void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<ChickenSpawner>();

        if (label == null)
            label = CreateLabel();

        ApplyFont(fontSize);
    }

    private void Update()
    {
        if (label == null || spawner == null)
            return;

        if (spawner.IsGameOver)
        {
            ApplyFont(gameOverFontSize);
            label.text = gameOverText;
            label.enabled = true;
            return;
        }

        ApplyFont(fontSize);

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

    private void ApplyFont(float size)
    {
        if (label == null || pixelonFont == null)
            return;

        label.font = pixelonFont;
        label.fontSharedMaterial = pixelonFont.material;
        label.fontSize = size;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
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
