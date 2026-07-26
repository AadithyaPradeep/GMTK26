using System.Collections;
using TMPro;
using UnityEngine;
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
    [SerializeField] private string gameOverText = "GAME OVER\nAll chickens lost";
    [SerializeField] private string laserLostGameOverText = "GAME OVER\nLaser chicken lost";
    [SerializeField] private string finishedText = "LEVEL 1 DONE!!";

    private TextMeshProUGUI hintLabel;
    private Coroutine hintRoutine;
    private Coroutine waveBannerRoutine;
    private string activeGameOverText;
    private bool showingFinished;
    private bool usingSceneHud;
    private int lastBannerWave = -1;

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
    }

    private void Update()
    {
        if (spawner == null)
            return;

        if (spawner.IsGameOver)
        {
            StopWaveBanner();
            ShowStatus(string.IsNullOrEmpty(activeGameOverText) ? gameOverText : activeGameOverText);
            ClearHint();
            return;
        }

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
