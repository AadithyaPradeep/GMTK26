using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows countdown until the next chicken wave using Pixelon SDF.
/// </summary>
public class WaveTimerUI : MonoBehaviour
{
    [SerializeField] private ChickenSpawner spawner;
    [SerializeField] private TMP_FontAsset pixelonFont;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float fontSize = 42f;
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Shown while counting down. {0} = seconds left (ceil).")]
    [SerializeField] private string waitingFormat = "NEXT WAVE\n{0}";

    [Tooltip("Shown while a wave is currently spawning.")]
    [SerializeField] private string spawningText = "";

    private void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<ChickenSpawner>();

        if (label == null)
            label = CreateLabel();

        ApplyFont();
    }

    private void Update()
    {
        if (label == null || spawner == null)
            return;

        if (spawner.IsWaitingForNextWave)
        {
            int seconds = Mathf.CeilToInt(spawner.SecondsUntilNextWave);
            label.text = string.Format(waitingFormat, seconds);
            label.enabled = true;
        }
        else
        {
            if (string.IsNullOrEmpty(spawningText))
            {
                label.text = string.Empty;
                label.enabled = false;
            }
            else
            {
                label.enabled = true;
                label.text = spawningText;
            }
        }
    }

    private void ApplyFont()
    {
        if (label == null || pixelonFont == null)
            return;

        label.font = pixelonFont;
        label.fontSharedMaterial = pixelonFont.material;
        label.fontSize = fontSize;
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
        rt.sizeDelta = new Vector2(800f, 140f);

        tmp.raycastTarget = false;
        return tmp;
    }
}
