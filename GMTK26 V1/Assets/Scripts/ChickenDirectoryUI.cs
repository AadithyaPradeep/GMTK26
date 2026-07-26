using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Home overlay: scrollable accordion chicken directory.
/// </summary>
public class ChickenDirectoryUI : MonoBehaviour
{
    private const float CardCollapsedHeight = 130f;
    private const float CardExpandedExtra = 160f;

    private TMP_FontAsset font;
    private ChickenDirectoryCatalog catalog;
    private System.Action onClosed;

    private GameObject root;
    private GameObject homeToHide;
    private readonly List<CardView> cards = new List<CardView>();
    private int openIndex = -1;

    private class CardView
    {
        public GameObject root;
        public RectTransform rootRt;
        public GameObject storyGo;
        public TextMeshProUGUI arrow;
        public LayoutElement layout;
        public bool expanded;
    }

    public static ChickenDirectoryUI Show(
        Transform canvasParent,
        TMP_FontAsset font,
        ChickenDirectoryCatalog catalog,
        GameObject homeContentToHide,
        System.Action onClosed)
    {
        GameObject go = new GameObject("ChickenDirectoryUI", typeof(RectTransform));
        go.transform.SetParent(canvasParent, false);
        StretchFull(go.GetComponent<RectTransform>());

        ChickenDirectoryUI ui = go.AddComponent<ChickenDirectoryUI>();
        ui.font = font;
        ui.catalog = catalog != null ? catalog : ChickenDirectoryCatalog.LoadOrCreateDefaults();
        ui.homeToHide = homeContentToHide;
        ui.onClosed = onClosed;
        ui.Build();
        return ui;
    }

    private void Build()
    {
        if (homeToHide != null)
            homeToHide.SetActive(false);

        root = gameObject;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.05f, 0.08f, 0.05f, 1f);
        backdrop.raycastTarget = true;

        // Header
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(root.transform, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0f, 110f);

        TextMeshProUGUI title = CreateTmp(header.transform, "Title", "CHICKEN DIRECTORY", 48f,
            new Color(0.992f, 0.969f, 0.635f, 1f), TextAlignmentOptions.Center);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.15f, 0f);
        titleRt.anchorMax = new Vector2(0.85f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        CreateHeaderButton(header.transform, "BackButton", "BACK", new Vector2(28f, 0f), Close);

        // Scroll view
        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(root.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(40f, 40f);
        scrollRt.offsetMax = new Vector2(-40f, -130f);

        Image scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.15f);
        scrollBg.raycastTarget = true;

        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        StretchFull(vpRt);
        viewport.AddComponent<RectMask2D>();
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.01f);
        vpImg.raycastTarget = true;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = contentRt;

        IReadOnlyList<ChickenDirectoryEntry> list = catalog != null ? catalog.Entries : null;
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    cards.Add(CreateCard(content.transform, list[i], i));
            }
        }
    }

    private CardView CreateCard(Transform parent, ChickenDirectoryEntry entry, int index)
    {
        CardView card = new CardView();

        GameObject cardGo = new GameObject("Card_" + entry.displayName, typeof(RectTransform));
        cardGo.transform.SetParent(parent, false);
        card.root = cardGo;
        card.rootRt = cardGo.GetComponent<RectTransform>();

        Image cardBg = cardGo.AddComponent<Image>();
        cardBg.color = new Color(0.1f, 0.14f, 0.1f, 0.95f);

        card.layout = cardGo.AddComponent<LayoutElement>();
        card.layout.minHeight = CardCollapsedHeight;
        card.layout.preferredHeight = CardCollapsedHeight;

        VerticalLayoutGroup inner = cardGo.AddComponent<VerticalLayoutGroup>();
        inner.padding = new RectOffset(16, 16, 12, 12);
        inner.spacing = 8f;
        inner.childAlignment = TextAnchor.UpperLeft;
        inner.childControlWidth = true;
        inner.childControlHeight = true;
        inner.childForceExpandWidth = true;
        inner.childForceExpandHeight = false;

        // Top row: portrait | texts | arrow
        GameObject row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(cardGo.transform, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 100f;
        rowLe.preferredHeight = 100f;

        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 18f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        h.padding = new RectOffset(0, 0, 0, 0);

        // Portrait
        GameObject portraitGo = new GameObject("Portrait", typeof(RectTransform));
        portraitGo.transform.SetParent(row.transform, false);
        LayoutElement pLe = portraitGo.AddComponent<LayoutElement>();
        pLe.minWidth = 96f;
        pLe.preferredWidth = 96f;
        pLe.minHeight = 96f;
        pLe.preferredHeight = 96f;
        Image portrait = portraitGo.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        if (entry.portrait != null)
            portrait.sprite = entry.portrait;
        else
            portrait.color = new Color(0.3f, 0.35f, 0.3f, 1f);

        // Text column
        GameObject textCol = new GameObject("TextCol", typeof(RectTransform));
        textCol.transform.SetParent(row.transform, false);
        LayoutElement tLe = textCol.AddComponent<LayoutElement>();
        tLe.flexibleWidth = 1f;
        tLe.minWidth = 200f;
        VerticalLayoutGroup textV = textCol.AddComponent<VerticalLayoutGroup>();
        textV.spacing = 4f;
        textV.childAlignment = TextAnchor.MiddleLeft;
        textV.childControlWidth = true;
        textV.childControlHeight = true;
        textV.childForceExpandWidth = true;
        textV.childForceExpandHeight = false;

        TextMeshProUGUI nameTmp = CreateTmp(textCol.transform, "Name", entry.displayName, 34f,
            new Color(0.992f, 0.969f, 0.635f, 1f), TextAlignmentOptions.Left);
        nameTmp.raycastTarget = false;
        LayoutElement nameLe = nameTmp.gameObject.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 40f;

        string roleLine = RoleLabel(entry.role);
        if (!string.IsNullOrEmpty(entry.worldHint))
            roleLine += "  ·  " + entry.worldHint;
        TextMeshProUGUI roleTmp = CreateTmp(textCol.transform, "Role", roleLine, 20f,
            new Color(0.7f, 0.85f, 0.65f, 1f), TextAlignmentOptions.Left);
        roleTmp.raycastTarget = false;
        LayoutElement roleLe = roleTmp.gameObject.AddComponent<LayoutElement>();
        roleLe.preferredHeight = 24f;

        TextMeshProUGUI descTmp = CreateTmp(textCol.transform, "Desc", entry.shortDescription, 22f,
            new Color(0.9f, 0.9f, 0.85f, 1f), TextAlignmentOptions.Left);
        descTmp.raycastTarget = false;
        descTmp.enableWordWrapping = true;
        LayoutElement descLe = descTmp.gameObject.AddComponent<LayoutElement>();
        descLe.preferredHeight = 36f;
        descLe.flexibleWidth = 1f;

        // Arrow
        GameObject arrowGo = new GameObject("Arrow", typeof(RectTransform));
        arrowGo.transform.SetParent(row.transform, false);
        LayoutElement aLe = arrowGo.AddComponent<LayoutElement>();
        aLe.minWidth = 56f;
        aLe.preferredWidth = 56f;
        card.arrow = CreateTmp(arrowGo.transform, "ArrowLabel", "▼", 36f,
            new Color(0.992f, 0.969f, 0.635f, 1f), TextAlignmentOptions.Center);
        StretchFull(card.arrow.rectTransform);
        card.arrow.raycastTarget = false;

        // Story (hidden until expand)
        card.storyGo = new GameObject("Story", typeof(RectTransform));
        card.storyGo.transform.SetParent(cardGo.transform, false);
        LayoutElement storyLe = card.storyGo.AddComponent<LayoutElement>();
        storyLe.minHeight = 0f;
        storyLe.preferredHeight = CardExpandedExtra;
        TextMeshProUGUI storyTmp = CreateTmp(card.storyGo.transform, "StoryText",
            string.IsNullOrEmpty(entry.story) ? "(No story yet.)" : entry.story,
            24f, new Color(0.85f, 0.88f, 0.8f, 1f), TextAlignmentOptions.TopLeft);
        StretchFull(storyTmp.rectTransform);
        storyTmp.enableWordWrapping = true;
        storyTmp.raycastTarget = false;
        card.storyGo.SetActive(false);

        Button button = cardGo.AddComponent<Button>();
        button.targetGraphic = cardBg;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.16f, 0.22f, 0.16f, 1f);
        colors.pressedColor = new Color(0.08f, 0.1f, 0.08f, 1f);
        button.colors = colors;
        int captured = index;
        button.onClick.AddListener(() => ToggleCard(captured));

        return card;
    }

    private void ToggleCard(int index)
    {
        if (index < 0 || index >= cards.Count)
            return;

        if (openIndex == index)
        {
            SetExpanded(cards[index], false);
            openIndex = -1;
            return;
        }

        if (openIndex >= 0 && openIndex < cards.Count)
            SetExpanded(cards[openIndex], false);

        SetExpanded(cards[index], true);
        openIndex = index;
    }

    private static void SetExpanded(CardView card, bool expanded)
    {
        card.expanded = expanded;
        if (card.storyGo != null)
            card.storyGo.SetActive(expanded);
        if (card.layout != null)
            card.layout.preferredHeight = expanded ? CardCollapsedHeight + CardExpandedExtra : CardCollapsedHeight;
        if (card.arrow != null)
        {
            card.arrow.text = "▼";
            card.arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, expanded ? 180f : 0f);
        }
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

    private void Close()
    {
        if (homeToHide != null)
            homeToHide.SetActive(true);

        onClosed?.Invoke();
        Destroy(gameObject);
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

    private static string RoleLabel(ChickenDirectoryRole role)
    {
        switch (role)
        {
            case ChickenDirectoryRole.Threat: return "THREAT";
            case ChickenDirectoryRole.Boss: return "BOSS";
            default: return "ALLY";
        }
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
