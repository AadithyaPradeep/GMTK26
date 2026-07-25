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
        if (string.IsNullOrEmpty(sceneName))
            return;

        EnsureExists();
        Instance.StartLoad(sceneName);
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
        if (busy)
            return;

        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        busy = true;
        yield return FadeTo(1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            yield return FadeTo(0f);
            busy = false;
            yield break;
        }

        while (!op.isDone)
            yield return null;

        // Let the new scene run Awake/Start once under cover of black.
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
