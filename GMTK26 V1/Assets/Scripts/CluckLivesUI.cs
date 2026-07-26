using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left row of CluckIcons — one per alive regular/panic chicken.
/// Icons disappear from the right when flock dies; reappear when more spawn.
/// Last remaining icon blinks red.
/// </summary>
public class CluckLivesUI : MonoBehaviour
{
    public static CluckLivesUI Instance { get; private set; }

    [SerializeField] private ChickenSpawner spawner;
    [SerializeField] private Sprite cluckIcon;
    [SerializeField] private float iconSize = 96f;
    [SerializeField] private float spacing = 12f;
    [SerializeField] private Vector2 padding = new Vector2(28f, 24f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float blinkSpeed = 4f;

    private readonly List<Image> icons = new List<Image>();
    private RectTransform row;
    private int displayedCount = -1;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("CluckLivesUI");
        go.AddComponent<CluckLivesUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (spawner == null)
            spawner = FindAnyObjectByType<ChickenSpawner>();

        if (cluckIcon == null)
            cluckIcon = ResolveIcon();

        BuildUi();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (row == null)
            return;

        if (spawner == null)
            spawner = FindAnyObjectByType<ChickenSpawner>();

        if (spawner == null)
        {
            if (row.gameObject.activeSelf)
                row.gameObject.SetActive(false);
            return;
        }

        // Hide during how-to-play / before game starts.
        bool show = spawner.HasStarted && !HowToPlayIntro.IsShowing;
        if (row.gameObject.activeSelf != show)
            row.gameObject.SetActive(show);

        if (!show)
            return;

        int alive = spawner.ProtectedAlive;
        if (alive != displayedCount)
            SetCount(alive);

        UpdateDangerBlink();
    }

    private void SetCount(int count)
    {
        count = Mathf.Max(0, count);
        displayedCount = count;

        while (icons.Count < count)
            icons.Add(CreateIcon(icons.Count));

        for (int i = 0; i < icons.Count; i++)
        {
            if (icons[i] == null)
                continue;

            icons[i].gameObject.SetActive(i < count);
            icons[i].color = normalColor;
        }
    }

    private void UpdateDangerBlink()
    {
        if (displayedCount != 1 || icons.Count == 0 || icons[0] == null)
            return;

        if (!icons[0].gameObject.activeSelf)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * blinkSpeed * Mathf.PI) + 1f) * 0.5f;
        icons[0].color = Color.Lerp(normalColor, dangerColor, t);
    }

    private Image CreateIcon(int index)
    {
        GameObject go = new GameObject("CluckIcon (" + index + ")", typeof(RectTransform));
        go.transform.SetParent(row, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(iconSize, iconSize);
        rt.anchoredPosition = new Vector2(index * (iconSize + spacing), 0f);

        Image img = go.AddComponent<Image>();
        img.sprite = cluckIcon;
        img.color = normalColor;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("CluckLivesCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rowGo = new GameObject("IconRow", typeof(RectTransform));
        rowGo.transform.SetParent(canvasGo.transform, false);
        row = rowGo.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(0f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.anchoredPosition = new Vector2(padding.x, -padding.y);
        row.sizeDelta = new Vector2(800f, iconSize);
        rowGo.SetActive(false);
    }

    private static Sprite ResolveIcon()
    {
        Sprite fromResources = Resources.Load<Sprite>("CluckIcon");
        if (fromResources != null)
            return fromResources;

        Sprite[] all = Resources.LoadAll<Sprite>("CluckIcon");
        if (all != null && all.Length > 0)
            return all[0];

        Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null || img.sprite == null)
                continue;
            if (img.gameObject.name.StartsWith("CluckIcon"))
                return img.sprite;
        }

        return null;
    }
}
