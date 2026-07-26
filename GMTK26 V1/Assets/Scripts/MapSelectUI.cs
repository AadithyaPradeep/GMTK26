using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Home overlay: pick maps + Combo (Story) or maps only (Chaos).
/// Click a map to preview its roster; CONTINUE or Enter starts the run.
/// </summary>
public class MapSelectUI : MonoBehaviour
{
    private const float IconSize = 76f;

    private static readonly Color TitleGold = new Color(1f, 0.92f, 0.45f, 1f);
    private static readonly Color TitleChaos = new Color(1f, 0.72f, 0.38f, 1f);
    private static readonly Color AllyAccent = new Color(0.55f, 0.95f, 0.62f, 1f);
    private static readonly Color EnemyAccent = new Color(1f, 0.48f, 0.42f, 1f);
    private static readonly Color MutedText = new Color(0.78f, 0.86f, 0.74f, 1f);

    private TMP_FontAsset font;
    private Sprite[] mapPreviews;
    private bool chaosMode;
    private Action<string> onSelected;
    private Action onClosed;

    private GameObject homeToHide;
    private ChickenDirectoryCatalog catalog;

    private string pendingMapId;
    private readonly Dictionary<string, Image> cardBackgrounds = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> cardRims = new Dictionary<string, Image>();
    private readonly Dictionary<string, RectTransform> cardRoots = new Dictionary<string, RectTransform>();

    private GameObject rosterPlaceholder;
    private GameObject rosterContent;
    private RectTransform allyIcons;
    private RectTransform enemyIcons;
    private TextMeshProUGUI allyEmpty;
    private TextMeshProUGUI enemyEmpty;
    private TextMeshProUGUI rosterTitle;

    private RectTransform hoverCard;
    private TextMeshProUGUI hoverTitle;
    private TextMeshProUGUI hoverRole;
    private TextMeshProUGUI hoverWave;
    private TextMeshProUGUI hoverBody;
    private Image hoverPortrait;
    private Sprite nineSlice;

    private Color accent;
    private Color bgDeep;
    private Color bgMid;
    private Color cardIdle;
    private Color cardSelected;
    private Color rimIdle;
    private Color rimSelected;
    private Color panelBg;

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
        ui.catalog = ChickenDirectoryCatalog.LoadOrCreateDefaults();
        ui.Build();
        return ui;
    }

    private void ApplyPalette()
    {
        if (chaosMode)
        {
            accent = TitleChaos;
            bgDeep = new Color(0.07f, 0.04f, 0.05f, 1f);
            bgMid = new Color(0.14f, 0.07f, 0.08f, 1f);
            cardIdle = new Color(0.16f, 0.09f, 0.09f, 1f);
            cardSelected = new Color(0.28f, 0.14f, 0.1f, 1f);
            rimIdle = new Color(0.35f, 0.18f, 0.14f, 1f);
            rimSelected = new Color(1f, 0.62f, 0.28f, 1f);
            panelBg = new Color(0.12f, 0.07f, 0.07f, 0.98f);
        }
        else
        {
            accent = TitleGold;
            bgDeep = new Color(0.04f, 0.08f, 0.05f, 1f);
            bgMid = new Color(0.08f, 0.14f, 0.09f, 1f);
            cardIdle = new Color(0.1f, 0.16f, 0.11f, 1f);
            cardSelected = new Color(0.16f, 0.28f, 0.16f, 1f);
            rimIdle = new Color(0.22f, 0.32f, 0.2f, 1f);
            rimSelected = new Color(1f, 0.9f, 0.35f, 1f);
            panelBg = new Color(0.07f, 0.12f, 0.08f, 0.98f);
        }
    }

    private void Update()
    {
        if (hoverCard != null && hoverCard.gameObject.activeSelf)
            FollowHover();

        if (string.IsNullOrEmpty(pendingMapId) || Keyboard.current == null)
            return;

        if (Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            Confirm();
    }

    private void Build()
    {
        ApplyPalette();
        nineSlice = LoadNineSlice();

        if (homeToHide != null)
            homeToHide.SetActive(false);

        Image backdrop = gameObject.AddComponent<Image>();
        backdrop.color = bgDeep;
        backdrop.raycastTarget = true;

        CreateWash("TopWash", new Vector2(0f, 0.72f), new Vector2(1f, 1f),
            new Color(bgMid.r, bgMid.g, bgMid.b, 0.85f));
        CreateWash("BottomWash", new Vector2(0f, 0f), new Vector2(1f, 0.42f),
            new Color(bgMid.r, bgMid.g, bgMid.b, 0.7f));

        BuildHeader();
        BuildCardsRow();
        BuildRosterPanel();
        BuildHoverCard();
        ShowPlaceholder();
    }

    private void CreateWash(string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void BuildHeader()
    {
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(transform, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.9f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;

        string titleText = chaosMode ? "CHOOSE CHAOS MAP" : "CHOOSE MAP";
        TextMeshProUGUI title = CreateTmp(header.transform, "Title", titleText, 48f,
            accent, TextAlignmentOptions.Center);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.18f, 0.38f);
        titleRt.anchorMax = new Vector2(0.82f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        string sub = chaosMode
            ? "Endless bombs & lightning — pick a battlefield"
            : "Select a world, scout the flock, then dive in";
        TextMeshProUGUI subtitle = CreateTmp(header.transform, "Subtitle", sub, 18f,
            MutedText, TextAlignmentOptions.Center);
        RectTransform subRt = subtitle.rectTransform;
        subRt.anchorMin = new Vector2(0.15f, 0.05f);
        subRt.anchorMax = new Vector2(0.85f, 0.42f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        GameObject line = new GameObject("HeaderLine", typeof(RectTransform));
        line.transform.SetParent(header.transform, false);
        RectTransform lineRt = line.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0.12f, 0f);
        lineRt.anchorMax = new Vector2(0.88f, 0.06f);
        lineRt.offsetMin = Vector2.zero;
        lineRt.offsetMax = Vector2.zero;
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(accent.r, accent.g, accent.b, 0.5f);
        lineImg.raycastTarget = false;

        CreateHeaderButton(header.transform, "BackButton", "BACK", Close);
    }

    private void BuildCardsRow()
    {
        int cardCount = GameMode.Maps.Length + (chaosMode ? 0 : 1);

        GameObject row = new GameObject("Cards", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.04f, 0.40f);
        rowRt.anchorMax = new Vector2(0.96f, 0.88f);
        rowRt.offsetMin = Vector2.zero;
        rowRt.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(12, 12, 8, 8);

        for (int i = 0; i < GameMode.Maps.Length; i++)
        {
            GameMode.MapInfo map = GameMode.Maps[i];
            if (chaosMode && !map.ChaosEligible)
                continue;

            CreateMapCard(row.transform, map.Id, map.DisplayName, map.OneLiner, GetPreview(i), false, cardCount);
        }

        if (!chaosMode)
        {
            CreateMapCard(
                row.transform,
                GameMode.ComboId,
                "COMBO",
                "All three worlds after Wave 5",
                null,
                true,
                cardCount);
        }
    }

    private void BuildRosterPanel()
    {
        GameObject panel = new GameObject("Roster", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.06f, 0.03f);
        panelRt.anchorMax = new Vector2(0.94f, 0.38f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelBgImg = panel.AddComponent<Image>();
        ApplySliced(panelBgImg, panelBg);
        panelBgImg.raycastTarget = true;

        // Placeholder
        rosterPlaceholder = new GameObject("Placeholder", typeof(RectTransform));
        rosterPlaceholder.transform.SetParent(panel.transform, false);
        StretchFull(rosterPlaceholder.GetComponent<RectTransform>());

        TextMeshProUGUI phTitle = CreateTmp(rosterPlaceholder.transform, "PhTitle", "SELECT A MAP ABOVE", 30f,
            accent, TextAlignmentOptions.Center);
        RectTransform phTitleRt = phTitle.rectTransform;
        phTitleRt.anchorMin = new Vector2(0.05f, 0.52f);
        phTitleRt.anchorMax = new Vector2(0.95f, 0.78f);
        phTitleRt.offsetMin = Vector2.zero;
        phTitleRt.offsetMax = Vector2.zero;
        phTitle.raycastTarget = false;

        TextMeshProUGUI phBody = CreateTmp(rosterPlaceholder.transform, "PhBody",
            chaosMode
                ? "You'll see the exploding & lightning threats for that map"
                : "Allies, enemies, powers, and which wave they show up",
            18f, MutedText, TextAlignmentOptions.Center);
        RectTransform phBodyRt = phBody.rectTransform;
        phBodyRt.anchorMin = new Vector2(0.1f, 0.22f);
        phBodyRt.anchorMax = new Vector2(0.9f, 0.52f);
        phBodyRt.offsetMin = Vector2.zero;
        phBodyRt.offsetMax = Vector2.zero;
        phBody.raycastTarget = false;
        phBody.textWrappingMode = TextWrappingModes.Normal;
        phBody.overflowMode = TextOverflowModes.Ellipsis;

        // Content
        rosterContent = new GameObject("Content", typeof(RectTransform));
        rosterContent.transform.SetParent(panel.transform, false);
        StretchFull(rosterContent.GetComponent<RectTransform>());
        RectTransform contentRt = rosterContent.GetComponent<RectTransform>();
        contentRt.offsetMin = new Vector2(18f, 12f);
        contentRt.offsetMax = new Vector2(-18f, -12f);
        rosterContent.SetActive(false);

        rosterTitle = CreateTmp(rosterContent.transform, "RosterTitle", "FLOCK PREVIEW", 20f,
            accent, TextAlignmentOptions.Center);
        RectTransform titleRt = rosterTitle.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0.88f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        rosterTitle.raycastTarget = false;
        rosterTitle.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI hint = CreateTmp(rosterContent.transform, "Hint", "HOVER A CHICKEN FOR DETAILS", 14f,
            new Color(0.62f, 0.7f, 0.58f, 1f), TextAlignmentOptions.Center);
        RectTransform hintRt = hint.rectTransform;
        hintRt.anchorMin = new Vector2(0f, 0.80f);
        hintRt.anchorMax = new Vector2(1f, 0.88f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;
        hint.raycastTarget = false;

        // Allies column (left)
        allyIcons = CreateRoleBlock(rosterContent.transform, "ALLIES", AllyAccent,
            new Vector2(0f, 0.28f), new Vector2(0.48f, 0.78f), out allyEmpty);

        // Enemies column (right)
        enemyIcons = CreateRoleBlock(rosterContent.transform, "ENEMIES", EnemyAccent,
            new Vector2(0.52f, 0.28f), new Vector2(1f, 0.78f), out enemyEmpty);

        // Continue row
        GameObject continueRow = new GameObject("ContinueRow", typeof(RectTransform));
        continueRow.transform.SetParent(rosterContent.transform, false);
        RectTransform continueRt = continueRow.GetComponent<RectTransform>();
        continueRt.anchorMin = new Vector2(0f, 0f);
        continueRt.anchorMax = new Vector2(1f, 0.26f);
        continueRt.offsetMin = Vector2.zero;
        continueRt.offsetMax = Vector2.zero;

        HorizontalLayoutGroup continueLayout = continueRow.AddComponent<HorizontalLayoutGroup>();
        continueLayout.spacing = 18f;
        continueLayout.childAlignment = TextAnchor.MiddleCenter;
        continueLayout.childControlWidth = true;
        continueLayout.childControlHeight = true;
        continueLayout.childForceExpandWidth = false;
        continueLayout.childForceExpandHeight = false;
        continueLayout.padding = new RectOffset(8, 8, 6, 6);

        CreateLabeledButton(continueRow.transform, "ContinueButton", "CONTINUE", 320f, 56f, Confirm);

        TextMeshProUGUI enterHint = CreateTmp(continueRow.transform, "EnterHint", "or press ENTER", 20f,
            MutedText, TextAlignmentOptions.MidlineLeft);
        enterHint.raycastTarget = false;
        LayoutElement enterLe = enterHint.gameObject.AddComponent<LayoutElement>();
        enterLe.preferredWidth = 220f;
        enterLe.minWidth = 180f;
        enterLe.preferredHeight = 56f;
        enterLe.minHeight = 56f;
    }

    private RectTransform CreateRoleBlock(
        Transform parent,
        string label,
        Color labelColor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        out TextMeshProUGUI emptyLabel)
    {
        GameObject block = new GameObject(label + "Block", typeof(RectTransform));
        block.transform.SetParent(parent, false);
        RectTransform blockRt = block.GetComponent<RectTransform>();
        blockRt.anchorMin = anchorMin;
        blockRt.anchorMax = anchorMax;
        blockRt.offsetMin = Vector2.zero;
        blockRt.offsetMax = Vector2.zero;

        Image blockBg = block.AddComponent<Image>();
        ApplySliced(blockBg, new Color(0f, 0f, 0f, 0.25f));
        blockBg.raycastTarget = false;

        TextMeshProUGUI labelTmp = CreateTmp(block.transform, "Label", label, 18f, labelColor, TextAlignmentOptions.Left);
        RectTransform labelRt = labelTmp.rectTransform;
        labelRt.anchorMin = new Vector2(0.04f, 0.78f);
        labelRt.anchorMax = new Vector2(0.96f, 0.98f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        labelTmp.raycastTarget = false;

        // Scrollable icon area so Combo can't blow past the box
        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(block.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.03f, 0.04f);
        scrollRt.anchorMax = new Vector2(0.97f, 0.76f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        Image scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.01f);
        scrollBg.raycastTarget = true;

        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        StretchFull(viewport.GetComponent<RectTransform>());
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.002f);
        vpImg.raycastTarget = true;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject icons = new GameObject("Icons", typeof(RectTransform));
        icons.transform.SetParent(viewport.transform, false);
        RectTransform iconsRt = icons.GetComponent<RectTransform>();
        iconsRt.anchorMin = new Vector2(0f, 0f);
        iconsRt.anchorMax = new Vector2(0f, 1f);
        iconsRt.pivot = new Vector2(0f, 0.5f);
        iconsRt.anchoredPosition = Vector2.zero;
        iconsRt.sizeDelta = new Vector2(100f, 0f);
        scroll.content = iconsRt;

        HorizontalLayoutGroup h = icons.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter fit = icons.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        emptyLabel = CreateTmp(icons.transform, "Empty", "—", 20f,
            new Color(0.45f, 0.5f, 0.45f, 1f), TextAlignmentOptions.MidlineLeft);
        emptyLabel.raycastTarget = false;
        LayoutElement emptyLe = emptyLabel.gameObject.AddComponent<LayoutElement>();
        emptyLe.preferredWidth = 28f;
        emptyLe.preferredHeight = IconSize;

        return iconsRt;
    }

    private void BuildHoverCard()
    {
        GameObject card = new GameObject("HoverCard", typeof(RectTransform));
        card.transform.SetParent(transform, false);
        hoverCard = card.GetComponent<RectTransform>();
        hoverCard.anchorMin = new Vector2(0.5f, 0.5f);
        hoverCard.anchorMax = new Vector2(0.5f, 0.5f);
        hoverCard.pivot = new Vector2(0f, 0f);
        hoverCard.sizeDelta = new Vector2(340f, 200f);
        hoverCard.gameObject.SetActive(false);

        Image bg = card.AddComponent<Image>();
        ApplySliced(bg, chaosMode
            ? new Color(0.16f, 0.08f, 0.08f, 0.98f)
            : new Color(0.08f, 0.14f, 0.1f, 0.98f));
        bg.raycastTarget = false;

        // Fixed regions so text never blows the card open
        hoverPortrait = CreateHoverPortrait(card.transform);

        hoverTitle = CreateTmp(card.transform, "Title", "", 18f, accent, TextAlignmentOptions.TopLeft);
        SetAnchors(hoverTitle.rectTransform, 0.28f, 0.72f, 0.94f, 0.94f);
        hoverTitle.raycastTarget = false;
        hoverTitle.textWrappingMode = TextWrappingModes.Normal;
        hoverTitle.overflowMode = TextOverflowModes.Ellipsis;

        hoverRole = CreateTmp(card.transform, "Role", "", 15f, AllyAccent, TextAlignmentOptions.TopLeft);
        SetAnchors(hoverRole.rectTransform, 0.28f, 0.58f, 0.94f, 0.72f);
        hoverRole.raycastTarget = false;
        hoverRole.overflowMode = TextOverflowModes.Ellipsis;

        hoverWave = CreateTmp(card.transform, "Wave", "", 16f, TitleGold, TextAlignmentOptions.TopLeft);
        SetAnchors(hoverWave.rectTransform, 0.06f, 0.44f, 0.94f, 0.56f);
        hoverWave.raycastTarget = false;
        hoverWave.overflowMode = TextOverflowModes.Ellipsis;

        hoverBody = CreateTmp(card.transform, "Body", "", 14f, MutedText, TextAlignmentOptions.TopLeft);
        SetAnchors(hoverBody.rectTransform, 0.06f, 0.06f, 0.94f, 0.42f);
        hoverBody.raycastTarget = false;
        hoverBody.textWrappingMode = TextWrappingModes.Normal;
        hoverBody.overflowMode = TextOverflowModes.Ellipsis;
    }

    private Image CreateHoverPortrait(Transform parent)
    {
        GameObject frame = new GameObject("Portrait", typeof(RectTransform));
        frame.transform.SetParent(parent, false);
        RectTransform frameRt = frame.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.05f, 0.58f);
        frameRt.anchorMax = new Vector2(0.24f, 0.92f);
        frameRt.offsetMin = Vector2.zero;
        frameRt.offsetMax = Vector2.zero;

        Image frameImg = frame.AddComponent<Image>();
        ApplySliced(frameImg, new Color(0f, 0f, 0f, 0.35f));
        frameImg.raycastTarget = false;

        GameObject imgGo = new GameObject("Img", typeof(RectTransform));
        imgGo.transform.SetParent(frame.transform, false);
        StretchFull(imgGo.GetComponent<RectTransform>());
        RectTransform imgRt = imgGo.GetComponent<RectTransform>();
        imgRt.offsetMin = new Vector2(4f, 4f);
        imgRt.offsetMax = new Vector2(-4f, -4f);

        Image img = imgGo.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Sprite GetPreview(int mapIndex)
    {
        if (mapPreviews == null || mapIndex < 0 || mapIndex >= mapPreviews.Length)
            return null;
        return mapPreviews[mapIndex];
    }

    private Color MapAccent(string mapId)
    {
        if (mapId == GameMode.DuskId)
            return new Color(0.45f, 0.55f, 0.95f, 1f);
        if (mapId == GameMode.GraveyardId)
            return new Color(0.72f, 0.55f, 0.9f, 1f);
        if (mapId == GameMode.ComboId)
            return accent;
        return new Color(0.45f, 0.82f, 0.4f, 1f);
    }

    private void CreateMapCard(
        Transform parent,
        string mapId,
        string title,
        string oneLiner,
        Sprite preview,
        bool splitCombo,
        int cardCount)
    {
        GameObject cardGo = new GameObject("Card_" + mapId, typeof(RectTransform));
        cardGo.transform.SetParent(parent, false);
        cardRoots[mapId] = cardGo.GetComponent<RectTransform>();

        LayoutElement le = cardGo.AddComponent<LayoutElement>();
        le.minWidth = cardCount <= 3 ? 240f : 200f;
        le.preferredWidth = cardCount <= 3 ? 300f : 250f;
        le.flexibleWidth = 1f;

        Image rim = cardGo.AddComponent<Image>();
        ApplySliced(rim, rimIdle);
        cardRims[mapId] = rim;

        GameObject inner = new GameObject("Inner", typeof(RectTransform));
        inner.transform.SetParent(cardGo.transform, false);
        StretchFull(inner.GetComponent<RectTransform>());
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        innerRt.offsetMin = new Vector2(5f, 5f);
        innerRt.offsetMax = new Vector2(-5f, -5f);

        Image cardBg = inner.AddComponent<Image>();
        ApplySliced(cardBg, cardIdle);
        cardBackgrounds[mapId] = cardBg;

        // Preview fills most of the card
        GameObject previewGo = new GameObject("Preview", typeof(RectTransform));
        previewGo.transform.SetParent(inner.transform, false);
        RectTransform previewRt = previewGo.GetComponent<RectTransform>();
        previewRt.anchorMin = new Vector2(0.06f, 0.30f);
        previewRt.anchorMax = new Vector2(0.94f, 0.94f);
        previewRt.offsetMin = Vector2.zero;
        previewRt.offsetMax = Vector2.zero;

        Image previewFrame = previewGo.AddComponent<Image>();
        previewFrame.color = new Color(0.05f, 0.06f, 0.05f, 1f);
        previewFrame.raycastTarget = false;

        GameObject strip = new GameObject("AccentStrip", typeof(RectTransform));
        strip.transform.SetParent(previewGo.transform, false);
        RectTransform stripRt = strip.GetComponent<RectTransform>();
        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.sizeDelta = new Vector2(0f, 5f);
        Image stripImg = strip.AddComponent<Image>();
        stripImg.color = MapAccent(mapId);
        stripImg.raycastTarget = false;

        if (splitCombo)
            BuildSplitPreview(previewGo.transform);
        else
            BuildSinglePreview(previewGo.transform, preview);

        TextMeshProUGUI titleTmp = CreateTmp(inner.transform, "Title", title, 28f,
            accent, TextAlignmentOptions.Center);
        RectTransform titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0.06f, 0.14f);
        titleRt.anchorMax = new Vector2(0.94f, 0.28f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        titleTmp.raycastTarget = false;
        titleTmp.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI lineTmp = CreateTmp(inner.transform, "OneLiner", oneLiner, 15f,
            MutedText, TextAlignmentOptions.Center);
        RectTransform lineRt = lineTmp.rectTransform;
        lineRt.anchorMin = new Vector2(0.08f, 0.02f);
        lineRt.anchorMax = new Vector2(0.92f, 0.14f);
        lineRt.offsetMin = Vector2.zero;
        lineRt.offsetMax = Vector2.zero;
        lineTmp.raycastTarget = false;
        lineTmp.textWrappingMode = TextWrappingModes.Normal;
        lineTmp.overflowMode = TextOverflowModes.Ellipsis;

        Button button = cardGo.AddComponent<Button>();
        button.targetGraphic = rim;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        string captured = mapId;
        button.onClick.AddListener(() => PreviewMap(captured));
    }

    private void BuildSinglePreview(Transform parent, Sprite preview)
    {
        GameObject imgGo = new GameObject("Image", typeof(RectTransform));
        imgGo.transform.SetParent(parent, false);
        StretchFull(imgGo.GetComponent<RectTransform>());
        RectTransform rt = imgGo.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(8f, 8f);
        rt.offsetMax = new Vector2(-8f, -10f);

        Image img = imgGo.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = false;
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
            rt.offsetMax = new Vector2(-padR, -10f);

            Image img = slice.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;

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

    private void PreviewMap(string mapId)
    {
        pendingMapId = mapId;
        HideHover();

        foreach (var kv in cardBackgrounds)
        {
            bool selected = kv.Key == mapId;
            kv.Value.color = selected ? cardSelected : cardIdle;
            if (cardRims.TryGetValue(kv.Key, out Image rim))
                rim.color = selected ? rimSelected : rimIdle;
            if (cardRoots.TryGetValue(kv.Key, out RectTransform root))
                root.localScale = selected ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
        }

        string label = mapId;
        for (int i = 0; i < GameMode.Maps.Length; i++)
        {
            if (GameMode.Maps[i].Id == mapId)
            {
                label = GameMode.Maps[i].DisplayName;
                break;
            }
        }

        if (mapId == GameMode.ComboId)
            label = "COMBO";

        if (rosterTitle != null)
            rosterTitle.text = chaosMode ? label + "  ·  CHAOS THREATS" : label + "  ·  FLOCK PREVIEW";

        PopulateRoster(mapId);
        ShowRosterContent();
    }

    private void ShowPlaceholder()
    {
        if (rosterPlaceholder != null)
            rosterPlaceholder.SetActive(true);
        if (rosterContent != null)
            rosterContent.SetActive(false);
        HideHover();
    }

    private void ShowRosterContent()
    {
        if (rosterPlaceholder != null)
            rosterPlaceholder.SetActive(false);
        if (rosterContent != null)
            rosterContent.SetActive(true);
    }

    private void PopulateRoster(string mapId)
    {
        ClearChildren(allyIcons, keepNamed: "Empty");
        ClearChildren(enemyIcons, keepNamed: "Empty");

        IReadOnlyList<MapRoster.Slot> slots = MapRoster.GetSlots(mapId, chaosMode);
        int allyCount = 0;
        int enemyCount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            MapRoster.Slot slot = slots[i];
            ChickenDirectoryEntry entry = catalog != null ? catalog.FindByDisplayName(slot.DirectoryName) : null;
            if (entry == null)
                continue;

            bool ally = entry.role == ChickenDirectoryRole.Ally;
            RectTransform parent = ally ? allyIcons : enemyIcons;
            if (ally)
                allyCount++;
            else
                enemyCount++;

            CreateMobIcon(parent, entry, slot);
        }

        if (allyEmpty != null)
            allyEmpty.gameObject.SetActive(allyCount == 0);
        if (enemyEmpty != null)
            enemyEmpty.gameObject.SetActive(enemyCount == 0);
    }

    private void CreateMobIcon(Transform parent, ChickenDirectoryEntry entry, MapRoster.Slot slot)
    {
        GameObject iconGo = new GameObject(entry.displayName, typeof(RectTransform));
        iconGo.transform.SetParent(parent, false);
        RectTransform rt = iconGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(IconSize, IconSize);

        LayoutElement le = iconGo.AddComponent<LayoutElement>();
        le.preferredWidth = IconSize;
        le.preferredHeight = IconSize;
        le.minWidth = IconSize;
        le.minHeight = IconSize;

        Image frame = iconGo.AddComponent<Image>();
        ApplySliced(frame, entry.role == ChickenDirectoryRole.Ally
            ? new Color(0.14f, 0.28f, 0.18f, 1f)
            : new Color(0.3f, 0.12f, 0.12f, 1f));

        GameObject imgGo = new GameObject("Portrait", typeof(RectTransform));
        imgGo.transform.SetParent(iconGo.transform, false);
        StretchFull(imgGo.GetComponent<RectTransform>());
        RectTransform imgRt = imgGo.GetComponent<RectTransform>();
        imgRt.offsetMin = new Vector2(6f, 6f);
        imgRt.offsetMax = new Vector2(-6f, -6f);

        Image img = imgGo.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.sprite = entry.portrait;
        img.color = entry.portrait != null ? entry.portraitColor : new Color(0.4f, 0.45f, 0.4f, 1f);

        GameObject badge = new GameObject("Badge", typeof(RectTransform));
        badge.transform.SetParent(iconGo.transform, false);
        RectTransform badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(1f, 0f);
        badgeRt.anchorMax = new Vector2(1f, 0f);
        badgeRt.pivot = new Vector2(1f, 0f);
        badgeRt.anchoredPosition = new Vector2(-2f, 2f);
        badgeRt.sizeDelta = new Vector2(32f, 20f);
        Image badgeBg = badge.AddComponent<Image>();
        ApplySliced(badgeBg, chaosMode
            ? new Color(0.55f, 0.22f, 0.12f, 0.95f)
            : new Color(0.12f, 0.2f, 0.12f, 0.95f));
        badgeBg.raycastTarget = false;

        string badgeText = slot.ChaosEndless ? "∞" : "W" + Mathf.Max(1, slot.UnlockWave);
        TextMeshProUGUI badgeTmp = CreateTmp(badge.transform, "Label", badgeText, 13f, accent, TextAlignmentOptions.Center);
        StretchFull(badgeTmp.rectTransform);
        badgeTmp.raycastTarget = false;

        EventTrigger trigger = iconGo.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowHover(entry, slot, rt));
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => HideHover());
    }

    private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => callback(data));
        trigger.triggers.Add(entry);
    }

    private void ShowHover(ChickenDirectoryEntry entry, MapRoster.Slot slot, RectTransform iconRt)
    {
        if (hoverCard == null || entry == null)
            return;

        hoverCard.gameObject.SetActive(true);
        hoverCard.SetAsLastSibling();
        hoverTitle.text = entry.displayName;
        hoverRole.text = RoleLabel(entry.role);
        hoverRole.color = entry.role == ChickenDirectoryRole.Ally ? AllyAccent : EnemyAccent;
        hoverWave.text = slot.ChaosEndless
            ? "CHAOS · Endless"
            : "Expect at Wave " + Mathf.Max(1, slot.UnlockWave);
        hoverBody.text = string.IsNullOrEmpty(entry.shortDescription)
            ? entry.story
            : entry.shortDescription;
        hoverPortrait.sprite = entry.portrait;
        hoverPortrait.color = entry.portrait != null ? entry.portraitColor : Color.white;

        PositionHoverNear(iconRt);
    }

    private void FollowHover()
    {
        if (hoverCard == null || !hoverCard.gameObject.activeSelf || Mouse.current == null)
            return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform,
            Mouse.current.position.ReadValue(),
            null,
            out local);
        hoverCard.anchoredPosition = ClampHover(local + new Vector2(18f, 18f));
    }

    private void PositionHoverNear(RectTransform iconRt)
    {
        Vector3 world = iconRt.TransformPoint(new Vector3(iconRt.rect.xMax, iconRt.rect.yMax, 0f));
        Vector2 local = ((RectTransform)transform).InverseTransformPoint(world);
        hoverCard.anchoredPosition = ClampHover(local + new Vector2(12f, 12f));
    }

    private Vector2 ClampHover(Vector2 pos)
    {
        RectTransform root = (RectTransform)transform;
        float halfW = root.rect.width * 0.5f;
        float halfH = root.rect.height * 0.5f;
        float cardW = hoverCard.sizeDelta.x;
        float cardH = hoverCard.sizeDelta.y;
        pos.x = Mathf.Clamp(pos.x, -halfW + 8f, halfW - cardW - 8f);
        pos.y = Mathf.Clamp(pos.y, -halfH + 8f, halfH - cardH - 8f);
        return pos;
    }

    private void HideHover()
    {
        if (hoverCard != null)
            hoverCard.gameObject.SetActive(false);
    }

    private static string RoleLabel(ChickenDirectoryRole role)
    {
        switch (role)
        {
            case ChickenDirectoryRole.Ally: return "ALLY";
            case ChickenDirectoryRole.Boss: return "BOSS";
            default: return "ENEMY";
        }
    }

    private void Confirm()
    {
        if (string.IsNullOrEmpty(pendingMapId))
            return;

        string mapId = pendingMapId;
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

    private static void ClearChildren(Transform parent, string keepNamed)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (keepNamed != null && child.name == keepNamed)
                continue;
            Destroy(child.gameObject);
        }
    }

    private Button CreateLabeledButton(Transform parent, string name, string label, float width, float height, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = buttonGo.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;

        Image img = buttonGo.AddComponent<Image>();
        ApplySliced(img, chaosMode
            ? new Color(0.78f, 0.36f, 0.2f, 1f)
            : new Color(0.36f, 0.62f, 0.3f, 1f));

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI tmp = CreateTmp(buttonGo.transform, "Label", label, 26f, Color.white, TextAlignmentOptions.Center);
        StretchFull(tmp.rectTransform);
        RectTransform labelRt = tmp.rectTransform;
        labelRt.offsetMin = new Vector2(14f, 4f);
        labelRt.offsetMax = new Vector2(-14f, -4f);
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return button;
    }

    private void CreateHeaderButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.2f);
        rt.anchorMax = new Vector2(0f, 0.8f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(24f, 0f);
        rt.sizeDelta = new Vector2(140f, 0f);

        Image img = buttonGo.AddComponent<Image>();
        ApplySliced(img, chaosMode
            ? new Color(0.32f, 0.16f, 0.14f, 1f)
            : new Color(0.2f, 0.32f, 0.2f, 1f));

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI tmp = CreateTmp(buttonGo.transform, "Label", label, 24f, Color.white, TextAlignmentOptions.Center);
        StretchFull(tmp.rectTransform);
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void ApplySliced(Image img, Color tint)
    {
        if (img == null)
            return;

        if (nineSlice != null)
        {
            img.sprite = nineSlice;
            img.type = Image.Type.Sliced;
        }

        img.color = tint;
    }

    private static Sprite LoadNineSlice()
    {
        Sprite single = Resources.Load<Sprite>("UI/9SliceUI");
        if (single != null)
            return single;

        Sprite[] all = Resources.LoadAll<Sprite>("UI/9SliceUI");
        if (all != null && all.Length > 0)
            return all[0];

        return null;
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
