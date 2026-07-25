using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title / home scene UI. Play = story mode, Chaos = bomb-rush gun mode.
/// </summary>
public class HomeMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private string titleText = "EXPLODING CHICKENS";
    [SerializeField] private string playButtonText = "PLAY";
    [SerializeField] private string chaosButtonText = "CHAOS";

    private bool loading;

    private void Awake()
    {
        Time.timeScale = 1f;
        SceneFader.EnsureExists();
        BuildUi();
    }

    public void OnPlayPressed()
    {
        StartGame(chaos: false);
    }

    public void OnChaosPressed()
    {
        StartGame(chaos: true);
    }

    private void StartGame(bool chaos)
    {
        if (loading || SceneFader.IsBusy)
            return;

        loading = true;
        if (chaos)
            GameMode.SetChaos();
        else
            GameMode.SetStory();

        SceneFader.Load(gameSceneName);
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

        // PLAY (left) + CHAOS (right)
        CreateMenuButton(
            root.transform,
            "PlayButton",
            playButtonText,
            new Vector2(-180f, 0f),
            new Color(0.92f, 0.82f, 0.28f, 1f),
            new Color(0.12f, 0.1f, 0.05f, 1f),
            OnPlayPressed);

        CreateMenuButton(
            root.transform,
            "ChaosButton",
            chaosButtonText,
            new Vector2(180f, 0f),
            new Color(0.85f, 0.28f, 0.22f, 1f),
            Color.white,
            OnChaosPressed);
    }

    private void CreateMenuButton(
        Transform parent,
        string name,
        string labelText,
        Vector2 anchoredPos,
        Color bgColor,
        Color labelColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);
        RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0.5f, 0.32f);
        buttonRt.anchorMax = new Vector2(0.5f, 0.32f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = anchoredPos;
        buttonRt.sizeDelta = new Vector2(300f, 96f);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = bgColor;

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(buttonGo.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 48f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = labelColor;
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
