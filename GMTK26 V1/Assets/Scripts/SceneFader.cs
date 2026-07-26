using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent full-screen fade used for Home / World1 / World2 scene changes.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] private Color fadeColor = Color.black;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GraphicRaycaster raycaster;
    private Image fadeImage;
    private bool busy;
    private bool holdingBlack;

    public static bool IsBusy => Instance != null && Instance.busy;
    public static bool IsHoldingBlack => Instance != null && Instance.holdingBlack;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("SceneFader");
        go.AddComponent<SceneFader>();
    }

    public static void Load(string sceneName)
    {
        ForceLoad(sceneName, holdBlack: false, null);
    }

    public static void Load(string sceneName, Action onFailedStart)
    {
        ForceLoad(sceneName, holdBlack: false, onFailedStart);
    }

    /// <summary>
    /// Fade to black, load scene, and stay black until Reveal() — used so How To Play can show on top.
    /// </summary>
    public static void LoadHoldBlack(string sceneName)
    {
        ForceLoad(sceneName, holdBlack: true, null);
    }

    public static void ClearBusy()
    {
        if (Instance == null)
            return;

        Instance.StopAllCoroutines();
        Instance.busy = false;
        Instance.holdingBlack = false;
        Instance.SetOverlayBlocking(false);
    }

    public static IEnumerator RevealRoutine()
    {
        EnsureExists();
        if (Instance == null)
            yield break;

        yield return Instance.StartCoroutine(Instance.RevealInternal());
    }

    private static void ForceLoad(string sceneName, bool holdBlack, Action onFailedStart)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            onFailedStart?.Invoke();
            return;
        }

        EnsureExists();
        if (Instance == null)
        {
            onFailedStart?.Invoke();
            return;
        }

        Instance.StopAllCoroutines();
        Instance.busy = false;
        Instance.holdingBlack = false;
        Instance.SetOverlayBlocking(false);
        Instance.StartCoroutine(Instance.FadeAndLoad(sceneName, holdBlack, onFailedStart));
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
        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!busy && !holdingBlack)
            SetOverlayBlocking(false);
    }

    private IEnumerator FadeAndLoad(string sceneName, bool holdBlack, Action onFailedStart)
    {
        busy = true;
        holdingBlack = false;
        yield return FadeTo(1f);

        AsyncOperation op = null;
        try
        {
            op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception)
        {
            op = null;
        }

        if (op == null)
        {
            try
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            catch (Exception)
            {
                busy = false;
                holdingBlack = false;
                SetOverlayBlocking(false);
                onFailedStart?.Invoke();
                yield break;
            }
        }
        else
        {
            while (!op.isDone)
                yield return null;
        }

        yield return null;

        if (holdBlack)
        {
            // Stay fully black; How To Play will draw above this.
            holdingBlack = true;
            busy = true;
            SetOverlayBlocking(true);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            yield break;
        }

        yield return FadeTo(0f);
        busy = false;
        holdingBlack = false;
        SetOverlayBlocking(false);
    }

    private IEnumerator RevealInternal()
    {
        holdingBlack = false;
        busy = true;
        yield return FadeTo(0f);
        busy = false;
        SetOverlayBlocking(false);
    }

    private IEnumerator FadeTo(float target)
    {
        if (canvasGroup == null)
            yield break;

        SetOverlayBlocking(true);

        float start = canvasGroup.alpha;
        float duration = Mathf.Max(0.05f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        canvasGroup.alpha = target;
        if (target <= 0.01f)
            SetOverlayBlocking(false);
    }

    private void SetOverlayBlocking(bool blocking)
    {
        if (canvasGroup != null)
        {
            if (!blocking && !holdingBlack)
                canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = blocking;
            canvasGroup.interactable = blocking;
        }

        if (fadeImage != null)
            fadeImage.raycastTarget = blocking;

        if (raycaster != null)
            raycaster.enabled = blocking;

        if (canvas != null)
            canvas.enabled = blocking || holdingBlack || (canvasGroup != null && canvasGroup.alpha > 0.01f);
    }

    private void BuildOverlay()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        raycaster = gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject imageGo = new GameObject("Fade", typeof(RectTransform));
        imageGo.transform.SetParent(transform, false);

        RectTransform rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imageGo.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = false;

        SetOverlayBlocking(false);
    }
}
