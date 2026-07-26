using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Home overlay: pick maps + Combo (Story) or maps only (Chaos).
/// </summary>
public class MapSelectUI : MonoBehaviour
{
    private TMP_FontAsset font;
    private Sprite[] mapPreviews;
    private bool chaosMode;
    private Action<string> onSelected;
    private Action onClosed;

    private GameObject homeToHide;

    public static MapSelectUI Show(
        Transform canvasParent,
        TMP_FontAsset font,
        Sprite[] mapPreviews,
        GameObject homeContentToHide,
        bool chaosMode,
        Action<string> onSelected,
        Action onClosed)
    {
        GameObject go = new GameObject("MapSelectUI", typeof(RectTransform));
        go.transform.SetParent(canvasParent, false);
        StretchFull(go.GetComponent<RectTransform>());

        MapSelectUI ui = go.AddComponent<MapSelectUI>();
        ui.font = font;
        ui.mapPreviews = mapPreviews;
        ui.homeToHide = homeContentToHide;
        ui.chaosMode = chaosMode;
        ui.onSelected = onSelected;
        ui.onClosed = onClosed;
        ui.Build();
        return ui;
    }

    private void Build()
    {
        if (homeToHide != null)
            homeToHide.SetActive(false);

        Image backdrop = gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.05f, 0.08f, 0.05f, 1f);
        backdrop.raycastTarget = true;

        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(transform, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0f, 110f);

        string titleText = chaosMode ? "CHOOSE CHAOS MAP" : "CHOOSE MAP";
        TextMeshProUGUI title = CreateTmp(header.transform, "Title", titleText, 48f,
            new Color(0.992f, 0.969f, 0.635f, 1f), TextAlignmentOptions.Center);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.15f, 0f);
        titleRt.anchorMax = new Vector2(0.85f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        CreateHeaderButton(header.transform, "BackButton", "BACK", new Vector2(28f, 0f), Close);

        int cardCount = GameMode.Maps.Length + (chaosMode ? 0 : 1);
        float rowWidth = Mathf.Clamp(260f * cardCount + 40f, 900f, 1500f);

        GameObject row = new GameObject("Cards", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(0f, -20f);
        rowRt.sizeDelta = new Vector2(rowWidth, 420f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(16, 16, 10, 10);

        for (int i = 0; i < GameMode.Maps.Length; i++)
        {
            GameMode.MapInfo map = GameMode.Maps[i];
            if (chaosMode && !map.ChaosEligible)
                continue;

            Sprite preview = GetPreview(i);
            CreateMapCard(row.transform, map.Id, map.DisplayName, map.OneLiner, preview, splitCombo: false);
        }

        if (!chaosMode)
        {
            CreateMapCard(
                row.transform,
                GameMode.ComboId,
                "COMBO",
                "Alternate all three after Wave 5",
                null,
                splitCombo: true);
        }
    }

    private Sprite GetPreview(int mapIndex)
    {
        if (mapPreviews == null || mapIndex < 0 || mapIndex >= mapPreviews.Length)
            return null;
        return mapPreviews[mapIndex];
    }

    private void CreateMapCard(
        Transform parent,
        string mapId,
        string title,
        string oneLiner,
        Sprite preview,
        bool splitCombo)
    {
        GameObject cardGo = new GameObject("Card_" + mapId, typeof(RectTransform));
        cardGo.transform.SetParent(parent, false);

        LayoutElement le = cardGo.AddComponent<LayoutElement>();
        le.minWidth = 220f;
        le.preferredWidth = 280f;
        le.flexibleWidth = 1f;

        Image cardBg = cardGo.AddComponent<Image>();
        cardBg.color = new Color(0.12f, 0.16f, 0.12f, 1f);

        VerticalLayoutGroup v = cardGo.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 14, 14);
        v.spacing = 10f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        GameObject previewGo = new GameObject("Preview", typeof(RectTransform));
        previewGo.transform.SetParent(cardGo.transform, false);
        LayoutElement previewLe = previewGo.AddComponent<LayoutElement>();
        previewLe.minHeight = 180f;
        previewLe.preferredHeight = 200f;
        previewLe.flexibleHeight = 1f;

        Image previewFrame = previewGo.AddComponent<Image>();
        previewFrame.color = new Color(0.08f, 0.1f, 0.08f, 1f);
        previewFrame.raycastTarget = false;

        if (splitCombo)
            BuildSplitPreview(previewGo.transform);
        else
            BuildSinglePreview(previewGo.transform, preview);

        TextMeshProUGUI titleTmp = CreateTmp(cardGo.transform, "Title", title, 30f,
            new Color(0.992f, 0.969f, 0.635f, 1f), TextAlignmentOptions.Center);
        titleTmp.raycastTarget = false;
        LayoutElement titleLe = titleTmp.gameObject.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 36f;

        TextMeshProUGUI lineTmp = CreateTmp(cardGo.transform, "OneLiner", oneLiner, 18f,
            new Color(0.75f, 0.85f, 0.7f, 1f), TextAlignmentOptions.Center);
        lineTmp.raycastTarget = false;
        lineTmp.enableWordWrapping = true;
        LayoutElement lineLe = lineTmp.gameObject.AddComponent<LayoutElement>();
        lineLe.preferredHeight = 48f;

        Button button = cardGo.AddComponent<Button>();
        button.targetGraphic = cardBg;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.18f, 0.24f, 0.18f, 1f);
        colors.pressedColor = new Color(0.1f, 0.14f, 0.1f, 1f);
        button.colors = colors;

        string captured = mapId;
        button.onClick.AddListener(() => Select(captured));
    }

    private void BuildSinglePreview(Transform parent, Sprite preview)
    {
        GameObject imgGo = new GameObject("Image", typeof(RectTransform));
        imgGo.transform.SetParent(parent, false);
        StretchFull(imgGo.GetComponent<RectTransform>());
        RectTransform rt = imgGo.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(8f, 8f);
        rt.offsetMax = new Vector2(-8f, -8f);

        Image img = imgGo.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        if (preview != null)
        {
            img.sprite = preview;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.25f, 0.32f, 0.25f, 1f);
        }
    }

    private void BuildSplitPreview(Transform parent)
    {
        int count = Mathf.Max(1, GameMode.Maps.Length);
        Color[] fallbacks =
        {
            new Color(0.35f, 0.45f, 0.28f, 1f),
            new Color(0.2f, 0.22f, 0.35f, 1f),
            new Color(0.28f, 0.26f, 0.3f, 1f),
        };

        for (int i = 0; i < count; i++)
        {
            float x0 = (float)i / count;
            float x1 = (float)(i + 1) / count;

            GameObject slice = new GameObject("Slice_" + i, typeof(RectTransform));
            slice.transform.SetParent(parent, false);
            RectTransform rt = slice.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, 0f);
            rt.anchorMax = new Vector2(x1, 1f);
            float padL = i == 0 ? 8f : 2f;
            float padR = i == count - 1 ? 8f : 2f;
            rt.offsetMin = new Vector2(padL, 8f);
            rt.offsetMax = new Vector2(-padR, -8f);

            Image img = slice.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;

            Sprite preview = GetPreview(i);
            if (preview != null)
            {
                img.sprite = preview;
                img.color = Color.white;
            }
            else
            {
                img.color = fallbacks[i % fallbacks.Length];
            }
        }
    }

    private void Select(string mapId)
    {
        Action<string> selected = onSelected;
        onClosed?.Invoke();
        Destroy(gameObject);
        selected?.Invoke(mapId);
    }

    private void Close()
    {
        if (homeToHide != null)
            homeToHide.SetActive(true);

        onClosed?.Invoke();
        Destroy(gameObject);
    }

    private void CreateHeaderButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(160f, 64f);

        Image img = buttonGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.35f, 0.22f, 1f);

        Button button = buttonGo.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI tmp = CreateTmp(buttonGo.transform, "Label", label, 28f, Color.white, TextAlignmentOptions.Center);
        StretchFull(tmp.rectTransform);
        tmp.raycastTarget = false;
    }

    private TextMeshProUGUI CreateTmp(
        Transform parent,
        string name,
        string text,
        float size,
        Color color,
        TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }

        return tmp;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
