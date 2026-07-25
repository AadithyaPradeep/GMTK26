using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title / home scene UI. Play loads the game scene.
/// </summary>
public class HomeMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private string titleText = "EXPLODING CHICKENS";
    [SerializeField] private string playButtonText = "PLAY";

    private bool loading;

    private void Awake()
    {
        Time.timeScale = 1f;
        BuildUi();
    }

    public void OnPlayPressed()
    {
        if (loading)
            return;

        loading = true;
        SceneManager.LoadScene(gameSceneName);
    }

    private void BuildUi()
    {
        GameObject root = new GameObject("HomeMenuCanvas");
        root.transform.SetParent(null);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        GameObject bgGo = new GameObject("Backdrop", typeof(RectTransform));
        bgGo.transform.SetParent(root.transform, false);
        StretchFull(bgGo.GetComponent<RectTransform>());
        Image bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.05f, 1f);
        bg.raycastTarget = true;

        GameObject titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(root.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.55f);
        titleRt.anchorMax = new Vector2(0.5f, 0.55f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(1400f, 160f);

        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = titleText;
        title.fontSize = 84f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.raycastTarget = false;
        ApplyFont(title);

        GameObject buttonGo = new GameObject("PlayButton", typeof(RectTransform));
        buttonGo.transform.SetParent(root.transform, false);
        RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0.5f, 0.32f);
        buttonRt.anchorMax = new Vector2(0.5f, 0.32f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = Vector2.zero;
        buttonRt.sizeDelta = new Vector2(320f, 96f);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.82f, 0.28f, 1f);

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 0.45f, 1f);
        colors.pressedColor = new Color(0.75f, 0.65f, 0.18f, 1f);
        button.colors = colors;
        button.onClick.AddListener(OnPlayPressed);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(buttonGo.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = playButtonText;
        label.fontSize = 48f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.12f, 0.1f, 0.05f, 1f);
        label.raycastTarget = false;
        ApplyFont(label);
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (font == null || tmp == null)
            return;

        tmp.font = font;
        tmp.fontSharedMaterial = font.material;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
