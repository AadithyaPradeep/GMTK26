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

    private CanvasGroup canvasGroup;
    private bool busy;

    public static bool IsBusy => Instance != null && Instance.busy;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("SceneFader");
        go.AddComponent<SceneFader>();
    }

    public static void Load(string sceneName)
    {
        ForceLoad(sceneName, null);
    }

    /// <summary>
    /// Always starts a load (resets stuck busy state). Calls onFailedStart if the coroutine can't begin.
    /// </summary>
    public static void ForceLoad(string sceneName, Action onFailedStart)
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
        if (Instance.canvasGroup != null)
        {
            Instance.canvasGroup.alpha = 0f;
            Instance.canvasGroup.blocksRaycasts = false;
        }

        Instance.StartCoroutine(Instance.FadeAndLoad(sceneName, onFailedStart));
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
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void StartLoad(string sceneName)
    {
        ForceLoad(sceneName, null);
    }

    private IEnumerator FadeAndLoad(string sceneName, Action onFailedStart)
    {
        busy = true;
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
            // Hard sync load — works even when async returns null in some editor setups.
            try
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            catch (Exception)
            {
                busy = false;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                }
                onFailedStart?.Invoke();
                yield break;
            }

            yield return null;
            yield return FadeTo(0f);
            busy = false;
            yield break;
        }

        while (!op.isDone)
            yield return null;

        yield return null;
        yield return FadeTo(0f);
        busy = false;
    }

    private IEnumerator FadeTo(float target)
    {
        if (canvasGroup == null)
            yield break;

        float start = canvasGroup.alpha;
        float duration = Mathf.Max(0.05f, fadeDuration);
        float elapsed = 0f;

        canvasGroup.blocksRaycasts = true;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        canvasGroup.alpha = target;
        canvasGroup.blocksRaycasts = target > 0.01f;
    }

    private void BuildOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

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

        Image image = imageGo.AddComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = true;
    }
}
