using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Global pause (P): dims the screen and shows Resume / Exit / volume controls.
/// Works in any gameplay world / mode. Disabled on the home scene.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color panelColor = new Color(0.12f, 0.14f, 0.1f, 0.96f);

    private Canvas canvas;
    private GameObject root;
    private Slider musicSlider;
    private Slider sfxSlider;
    private float timeScaleBeforePause = 1f;
    private bool built;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("PauseMenu");
        go.AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureEventSystem();
        BuildUi();
        SetPaused(false);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
            IsPaused = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsHomeScene(scene.name))
        {
            SetPaused(false);
            if (canvas != null)
                canvas.enabled = false;
            if (root != null)
                root.SetActive(false);
        }
        else if (canvas != null)
        {
            canvas.enabled = true;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (IsHomeScene(SceneManager.GetActiveScene().name))
            return;

        if (SceneFader.IsBusy)
            return;

        if (!Keyboard.current.pKey.wasPressedThisFrame)
            return;

        SetPaused(!IsPaused);
    }

    private static bool IsHomeScene(string sceneName)
    {
        return sceneName == "HomeScene" || sceneName == "Home";
    }

    public void SetPaused(bool paused)
    {
        if (paused == IsPaused && built)
        {
            if (root != null)
                root.SetActive(paused);
            return;
        }

        if (paused)
        {
            timeScaleBeforePause = Time.timeScale > 0.01f ? Time.timeScale : 1f;
            // If already frozen (game over), keep that on resume.
            if (Time.timeScale <= 0.01f)
                timeScaleBeforePause = 0f;

            Time.timeScale = 0f;
            AudioListener.pause = true;
            IsPaused = true;
            SyncSlidersFromAudio();
            if (root != null)
                root.SetActive(true);
        }
        else
        {
            IsPaused = false;
            AudioListener.pause = false;
            Time.timeScale = timeScaleBeforePause > 0.01f ? timeScaleBeforePause : 1f;
            // Game-over should stay frozen.
            if (timeScaleBeforePause <= 0.01f)
                Time.timeScale = 0f;
            if (root != null)
                root.SetActive(false);
            PlayerPrefs.Save();
        }
    }

    private void Resume()
    {
        SetPaused(false);
    }

    private void ExitToHome()
    {
        IsPaused = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
        if (root != null)
            root.SetActive(false);

        SceneFader.Load(homeSceneName);
    }

    private void SyncSlidersFromAudio()
    {
        float music = 0.35f;
        float sfx = 0.85f;

        if (GameAudio.Instance != null)
        {
            music = GameAudio.Instance.MusicVolume;
            sfx = GameAudio.Instance.SfxVolume;
        }
        else
        {
            music = PlayerPrefs.GetFloat("MusicVolume", music);
            sfx = PlayerPrefs.GetFloat("SfxVolume", sfx);
        }

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfx);
    }

    private void OnMusicChanged(float value)
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.MusicVolume = value;
        else
            PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void OnSfxChanged(float value)
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.SfxVolume = value;
        else
            PlayerPrefs.SetFloat("SfxVolume", value);
    }

    private void BuildUi()
    {
        if (built)
            return;
        built = true;

        root = new GameObject("PauseRoot", typeof(RectTransform));
        root.transform.SetParent(transform, false);

        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Dim backdrop
        GameObject dimGo = new GameObject("Dim", typeof(RectTransform));
        dimGo.transform.SetParent(root.transform, false);
        StretchFull(dimGo.GetComponent<RectTransform>());
        Image dim = dimGo.AddComponent<Image>();
        dim.color = dimColor;
        dim.raycastTarget = true;

        // Center panel
        GameObject panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(root.transform, false);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 420f);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = panelColor;

        CreateLabel(panelGo.transform, "PAUSED", 56f, new Vector2(0f, 150f), new Vector2(480f, 70f));

        musicSlider = CreateVolumeSlider(panelGo.transform, "MUSIC", new Vector2(0f, 70f), OnMusicChanged);
        sfxSlider = CreateVolumeSlider(panelGo.transform, "SOUND", new Vector2(0f, -10f), OnSfxChanged);

        CreateMenuButton(panelGo.transform, "RESUME", new Vector2(0f, -90f),
            new Color(0.92f, 0.82f, 0.28f, 1f), new Color(0.12f, 0.1f, 0.05f, 1f), Resume);
        CreateMenuButton(panelGo.transform, "EXIT", new Vector2(0f, -170f),
            new Color(0.85f, 0.28f, 0.22f, 1f), Color.white, ExitToHome);

        SyncSlidersFromAudio();
        root.SetActive(false);
    }

    private Slider CreateVolumeSlider(Transform parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = new GameObject(label + "Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = anchoredPos;
        rowRt.sizeDelta = new Vector2(420f, 50f);

        CreateLabel(row.transform, label, 28f, new Vector2(-140f, 0f), new Vector2(120f, 40f));

        GameObject sliderGo = new GameObject("Slider", typeof(RectTransform));
        sliderGo.transform.SetParent(row.transform, false);
        RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(60f, 0f);
        sliderRt.sizeDelta = new Vector2(260f, 24f);

        // Background
        GameObject bgGo = new GameObject("Background", typeof(RectTransform));
        bgGo.transform.SetParent(sliderGo.transform, false);
        StretchFull(bgGo.GetComponent<RectTransform>());
        Image bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.28f, 0.22f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(5f, 4f);
        fillAreaRt.offsetMax = new Vector2(-5f, -4f);

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(fillArea.transform, false);
        StretchFull(fillGo.GetComponent<RectTransform>());
        Image fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.75f, 0.85f, 0.35f, 1f);

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform));
        handleGo.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handleGo.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(22f, 28f);
        Image handle = handleGo.AddComponent<Image>();
        handle.color = new Color(0.95f, 0.95f, 0.9f, 1f);

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fillGo.GetComponent<RectTransform>();
        slider.handleRect = handleRt;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    private void CreateLabel(Transform parent, string text, float fontSize, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject("Label_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    private void CreateMenuButton(
        Transform parent,
        string labelText,
        Vector2 anchoredPos,
        Color bgColor,
        Color labelColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(labelText + "Button", typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);
        RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = anchoredPos;
        buttonRt.sizeDelta = new Vector2(280f, 64f);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = bgColor;

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(buttonGo.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 36f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = labelColor;
        label.raycastTarget = false;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
