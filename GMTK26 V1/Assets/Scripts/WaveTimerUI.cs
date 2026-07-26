using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives scene Wave / Protect / Next Wave texts. Falls back to a runtime label only for
/// game-over / finished / boss hints when those scene texts aren't present.
/// Does not resize or restyle scene-authored TMP.
/// </summary>
public class WaveTimerUI : MonoBehaviour
{
    [SerializeField] private ChickenSpawner spawner;
    [SerializeField] private TMP_FontAsset pixelonFont;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI waveLabel;
    [SerializeField] private TextMeshProUGUI waveSubLabel;
    [SerializeField] private TextMeshProUGUI nextWaveLabel;
    [SerializeField] private float fontSize = 42f;
    [SerializeField] private float gameOverFontSize = 72f;
    [SerializeField] private float hintFontSize = 36f;
    [SerializeField] private Color textColor = Color.white;

    [SerializeField] private string waveFormat = "Wave {0}";
    [SerializeField] private string nextWaveFormat = "Next Wave in {0} s";
    [SerializeField] private string waveSubText = "Protect Your Chickens !";
    [SerializeField] private float waveBannerDuration = 2f;
    [SerializeField] private string gameOverText = "GAME OVER!\nALL CHICKENS DEAD!";
    [SerializeField] private string laserLostGameOverText = "GAME OVER!\nLASER CHICKEN DEAD!";
    [SerializeField] private string finishedText = "LEVEL 1 DONE!!";
    [SerializeField] private string homeSceneName = "HomeScene";

    private TextMeshProUGUI hintLabel;
    private Coroutine hintRoutine;
    private Coroutine waveBannerRoutine;
    private string activeGameOverText;
    private bool showingFinished;
    private bool usingSceneHud;
    private int lastBannerWave = -1;

    private GameObject gameOverRoot;
    private TextMeshProUGUI gameOverLabel;
    private bool gameOverUiBuilt;
    private bool navigatingAway;

    private void Awake()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<ChickenSpawner>();

        ResolveSceneLabels();
        usingSceneHud = waveLabel != null || nextWaveLabel != null;

        if (waveSubLabel != null && !string.IsNullOrEmpty(waveSubLabel.text))
            waveSubText = waveSubLabel.text;

        if (usingSceneHud)
            EnsureHudCanvasActive();
        else if (label == null)
            label = CreateLabel();

        activeGameOverText = gameOverText;

        // Only restyle the runtime fallback label — never touch scene-authored TMP sizes.
        if (!usingSceneHud && label != null)
            ApplyFont(label, fontSize);

        HideWaveBanner();
        if (nextWaveLabel != null)
            nextWaveLabel.enabled = false;

        // Chaos has no waves — hide the story wave HUD entirely.
        if (GameMode.IsChaos)
            HideChaosWaveHud();
    }

    private void HideChaosWaveHud()
    {
        lastBannerWave = int.MaxValue;
        HideWaveBanner();

        if (waveLabel != null)
        {
            waveLabel.text = string.Empty;
            waveLabel.enabled = false;
            if (waveLabel.gameObject != null)
                waveLabel.gameObject.SetActive(false);
        }

        if (waveSubLabel != null)
        {
            waveSubLabel.text = string.Empty;
            waveSubLabel.enabled = false;
            if (waveSubLabel.gameObject != null)
                waveSubLabel.gameObject.SetActive(false);
        }

        if (nextWaveLabel != null)
        {
            nextWaveLabel.text = string.Empty;
            nextWaveLabel.enabled = false;
            if (nextWaveLabel.gameObject != null)
                nextWaveLabel.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (spawner == null)
            return;

        if (spawner.IsGameOver)
        {
            StopWaveBanner();
            ClearHint();
            HideSceneStatusLabels();
            ShowGameOverUi(string.IsNullOrEmpty(activeGameOverText) ? gameOverText : activeGameOverText);
            return;
        }

        HideGameOverUi();

        if (spawner.IsFinished || showingFinished)
        {
            StopWaveBanner();
            ShowStatus(finishedText);
            return;
        }

        if (!spawner.HasStarted)
        {
            StopWaveBanner();
            HideWaveBanner();
            if (nextWaveLabel != null)
            {
                nextWaveLabel.text = string.Empty;
                nextWaveLabel.enabled = false;
            }
            return;
        }

        // Chaos: no wave banner / next-wave countdown.
        if (GameMode.IsChaos)
        {
            StopWaveBanner();
            HideChaosWaveHud();
            return;
        }

        int waveNum = Mathf.Max(1, spawner.CurrentWave);
        if (spawner.CurrentWave > 0 && waveNum != lastBannerWave)
            ShowWaveBanner(waveNum);

        if (spawner.IsWaitingForNextWave)
        {
            int seconds = Mathf.CeilToInt(spawner.SecondsUntilNextWave);
            if (nextWaveLabel != null)
            {
                nextWaveLabel.text = string.Format(nextWaveFormat, seconds);
                nextWaveLabel.enabled = true;
                if (nextWaveLabel.gameObject != null)
                    nextWaveLabel.gameObject.SetActive(true);
            }
            else if (!usingSceneHud && label != null)
            {
                ApplyFont(label, fontSize);
                label.text = string.Format(waveFormat, waveNum) + "\n" + string.Format(nextWaveFormat, seconds);
                label.enabled = true;
            }
        }
        else if (nextWaveLabel != null)
        {
            nextWaveLabel.text = string.Empty;
            nextWaveLabel.enabled = false;
        }
        else if (!usingSceneHud && label != null)
        {
            label.text = string.Empty;
            label.enabled = false;
        }
    }

    private void ShowWaveBanner(int waveNum)
    {
        lastBannerWave = waveNum;
        StopWaveBanner();

        if (waveLabel != null)
        {
            waveLabel.text = string.Format(waveFormat, waveNum);
            waveLabel.enabled = true;
            if (waveLabel.gameObject != null)
                waveLabel.gameObject.SetActive(true);
        }

        if (waveSubLabel != null)
        {
            waveSubLabel.text = waveSubText;
            waveSubLabel.enabled = true;
            if (waveSubLabel.gameObject != null)
                waveSubLabel.gameObject.SetActive(true);
        }

        waveBannerRoutine = StartCoroutine(HideWaveBannerAfter(waveBannerDuration));
    }

    private IEnumerator HideWaveBannerAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideWaveBanner();
        waveBannerRoutine = null;
    }

    private void HideWaveBanner()
    {
        if (waveLabel != null)
            waveLabel.enabled = false;
        if (waveSubLabel != null)
            waveSubLabel.enabled = false;
    }

    private void StopWaveBanner()
    {
        if (waveBannerRoutine != null)
        {
            StopCoroutine(waveBannerRoutine);
            waveBannerRoutine = null;
        }
    }

    public void ShowFinished()
    {
        showingFinished = true;
        ClearHint();
        StopWaveBanner();
        ShowStatus(finishedText);
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

    private void ShowGameOverUi(string message)
    {
        EnsureGameOverUi();
        if (gameOverRoot == null)
            return;

        if (gameOverLabel != null)
        {
            ApplyFont(gameOverLabel, gameOverFontSize);
            // Keep the scene HUD cream/yellow look when available.
            if (waveLabel != null)
                gameOverLabel.color = waveLabel.color;
            else
                gameOverLabel.color = new Color(0.992f, 0.969f, 0.635f, 1f);
            gameOverLabel.text = message;
            gameOverLabel.enabled = true;
        }

        if (!gameOverRoot.activeSelf)
            gameOverRoot.SetActive(true);
    }

    private void HideGameOverUi()
    {
        if (gameOverRoot != null && gameOverRoot.activeSelf)
            gameOverRoot.SetActive(false);
    }

    private void HideSceneStatusLabels()
    {
        HideWaveBanner();
        if (nextWaveLabel != null)
        {
            nextWaveLabel.text = string.Empty;
            nextWaveLabel.enabled = false;
        }

        if (label != null)
        {
            label.text = string.Empty;
            label.enabled = false;
        }

        if (waveLabel != null)
        {
            waveLabel.text = string.Empty;
            waveLabel.enabled = false;
        }
    }

    private void ShowStatus(string message)
    {
        HideWaveBanner();
        if (nextWaveLabel != null)
        {
            nextWaveLabel.text = string.Empty;
            nextWaveLabel.enabled = false;
        }

        TextMeshProUGUI target = label != null ? label : waveLabel;
        if (target == null)
            target = label = CreateLabel();

        if (target == waveLabel)
        {
            // Scene wave title — keep its size/style, only swap the string.
            target.text = message;
            target.enabled = true;
            if (target.gameObject != null)
                target.gameObject.SetActive(true);
            return;
        }

        ApplyFont(target, gameOverFontSize);
        target.text = message;
        target.enabled = true;
    }

    private void EnsureGameOverUi()
    {
        if (gameOverUiBuilt)
            return;

        gameOverUiBuilt = true;
        EnsureEventSystem();

        TMP_FontAsset font = ResolveFont();

        GameObject canvasGo = new GameObject("GameOverCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        gameOverRoot = new GameObject("GameOverRoot", typeof(RectTransform));
        gameOverRoot.transform.SetParent(canvasGo.transform, false);
        RectTransform rootRt = gameOverRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Centered two-line status (wide enough for "GAME OVER!" on one line).
        GameObject textGo = new GameObject("GameOverText", typeof(RectTransform));
        textGo.transform.SetParent(gameOverRoot.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = new Vector2(-120f, 40f);
        textRt.sizeDelta = new Vector2(1100f, 220f);

        gameOverLabel = textGo.AddComponent<TextMeshProUGUI>();
        gameOverLabel.alignment = TextAlignmentOptions.Center;
        gameOverLabel.enableWordWrapping = false;
        gameOverLabel.overflowMode = TextOverflowModes.Overflow;
        gameOverLabel.raycastTarget = false;
        if (font != null)
        {
            gameOverLabel.font = font;
            gameOverLabel.fontSharedMaterial = font.material;
        }

        // Side buttons (right of the message).
        CreateGameOverButton(
            gameOverRoot.transform,
            "REPLAY",
            new Vector2(520f, 70f),
            new Color(0.92f, 0.82f, 0.28f, 1f),
            new Color(0.12f, 0.1f, 0.05f, 1f),
            font,
            ReplayCurrentMap);

        CreateGameOverButton(
            gameOverRoot.transform,
            "HOME",
            new Vector2(520f, -20f),
            new Color(0.85f, 0.28f, 0.22f, 1f),
            Color.white,
            font,
            ReturnToHome);

        gameOverRoot.SetActive(false);
    }

    private void CreateGameOverButton(
        Transform parent,
        string labelText,
        Vector2 anchoredPos,
        Color bgColor,
        Color labelColor,
        TMP_FontAsset font,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(labelText + "Button", typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);
        RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = anchoredPos;
        buttonRt.sizeDelta = new Vector2(280f, 72f);

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
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = labelColor;
        tmp.raycastTarget = false;
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }
    }

    private void ReplayCurrentMap()
    {
        if (navigatingAway || SceneFader.IsBusy)
            return;

        navigatingAway = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.SetPaused(false);

        // Keep Story/Chaos + map prefs; skip How To Play on replay.
        GameMode.PendingHowToPlay = false;
        GameMode.PendingStartScene = null;
        GameAudio.HoldBgmForIntro = false;

        SceneFader.EnsureExists();
        SceneFader.ClearBusy();
        SceneFader.Load(SceneManager.GetActiveScene().name);
    }

    private void ReturnToHome()
    {
        if (navigatingAway || SceneFader.IsBusy)
            return;

        navigatingAway = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.SetPaused(false);

        GameMode.PendingHowToPlay = false;
        GameMode.PendingStartScene = null;
        GameAudio.HoldBgmForIntro = false;

        SceneFader.EnsureExists();
        SceneFader.ClearBusy();
        SceneFader.Load(homeSceneName);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private TMP_FontAsset ResolveFont()
    {
        if (pixelonFont != null)
            return pixelonFont;

        TMP_FontAsset fromResources = Resources.Load<TMP_FontAsset>("Pixelon SDF");
        if (fromResources != null)
            return fromResources;

        return TMP_Settings.defaultFontAsset;
    }

    private void ResolveSceneLabels()
    {
        TextMeshProUGUI[] all = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            TextMeshProUGUI tmp = all[i];
            if (tmp == null || !tmp.gameObject.scene.IsValid() || !tmp.gameObject.scene.isLoaded)
                continue;

            // Skip How To Play canvas texts.
            if (IsUnderNamedParent(tmp.transform, "How To Play") || IsUnderNamedParent(tmp.transform, "HowToPlay"))
                continue;

            string n = tmp.gameObject.name;
            if (waveLabel == null && (n == "WaveText" || n == "Wave"))
                waveLabel = tmp;
            else if (waveSubLabel == null && (n == "WaveSubText" || n == "Wave (1)"))
                waveSubLabel = tmp;
            else if (nextWaveLabel == null && (n == "Next Wave" || n.StartsWith("Next Wave")))
                nextWaveLabel = tmp;
        }
    }

    private void EnsureHudCanvasActive()
    {
        Transform root = null;
        if (waveLabel != null)
            root = waveLabel.transform.parent;
        else if (nextWaveLabel != null)
            root = nextWaveLabel.transform.parent;
        else if (waveSubLabel != null)
            root = waveSubLabel.transform.parent;

        if (root == null)
            return;

        // Activate the gameplay HUD canvas (often stored as ChickenSpawner.introBanner).
        Canvas canvas = root.GetComponentInParent<Canvas>(true);
        if (canvas != null)
            canvas.gameObject.SetActive(true);

        // Static CluckIcons on this canvas are decorative leftovers — CluckLivesUI owns the live count.
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name.StartsWith("CluckIcon"))
                child.gameObject.SetActive(false);
        }
    }

    private static bool IsUnderNamedParent(Transform t, string parentName)
    {
        while (t != null)
        {
            if (t.name == parentName)
                return true;
            t = t.parent;
        }

        return false;
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

        Transform canvasTf = null;
        if (waveLabel != null)
            canvasTf = waveLabel.transform.parent;
        else if (label != null)
            canvasTf = label.transform.parent;

        if (canvasTf == null)
        {
            if (label == null)
                label = CreateLabel();
            canvasTf = label != null ? label.transform.parent : null;
        }

        if (canvasTf == null)
            return;

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
