using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title / home scene UI. Uses scene Hierarchy objects so you can edit them in the Inspector.
/// Play / Chaos open MapSelectUI; assign Farm / Dusk / Graveyard preview sprites below.
/// </summary>
public class HomeMenu : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private ChickenDirectoryCatalog directoryCatalog;

    [Header("Map Select Previews")]
    [SerializeField] private Sprite farmPreview;
    [SerializeField] private Sprite duskPreview;
    [SerializeField] private Sprite graveyardPreview;

    [Header("Scene UI (assign in Hierarchy)")]
    [SerializeField] private GameObject homeCanvas;
    [SerializeField] private GameObject homeContent;
    [SerializeField] private Button playButton;
    [SerializeField] private Button chaosButton;
    [SerializeField] private Button directoryButton;

    private bool loading;
    private ChickenDirectoryUI directoryUi;
    private MapSelectUI mapSelectUi;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameAudio.HoldBgmForIntro = false;
        GameAudio.EnsureExists();
        SceneFader.EnsureExists();
        SceneFader.ClearBusy();
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.SetPaused(false);

        ResolveSceneRefs();
        WireButtons();
    }

    public void OnPlayPressed()
    {
        OpenMapSelect(chaos: false);
    }

    public void OnChaosPressed()
    {
        OpenMapSelect(chaos: true);
    }

    public void OnDirectoryPressed()
    {
        if (loading || directoryUi != null || mapSelectUi != null)
            return;

        if (homeCanvas == null)
            return;

        if (directoryCatalog == null)
            directoryCatalog = ChickenDirectoryCatalog.LoadOrCreateDefaults();

        directoryUi = ChickenDirectoryUI.Show(
            homeCanvas.transform,
            font,
            directoryCatalog,
            homeContent,
            () => directoryUi = null);
    }

    private void OpenMapSelect(bool chaos)
    {
        if (loading || mapSelectUi != null || directoryUi != null)
            return;

        if (homeCanvas == null)
            return;

        Sprite[] previews = { farmPreview, duskPreview, graveyardPreview };
        mapSelectUi = MapSelectUI.Show(
            homeCanvas.transform,
            font,
            previews,
            homeContent,
            chaos,
            mapId => StartGame(chaos, mapId),
            () => mapSelectUi = null);
    }

    private void StartGame(bool chaos, string mapId)
    {
        if (loading)
            return;

        SceneFader.ClearBusy();
        loading = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (mapId == GameMode.ComboId)
            GameMode.SetCombo();
        else
            GameMode.SetSingleMap(mapId);

        string scene = GameMode.StartSceneName;

        if (chaos)
        {
            GameMode.SetChaos();
            GameMode.PendingStartScene = null;
            GameAudio.HoldBgmForIntro = false;
            SceneFader.Load(scene);
            return;
        }

        GameMode.SetStory();
        GameMode.PendingHowToPlay = true;
        GameMode.PendingStartScene = null;
        GameAudio.HoldBgmForIntro = true;

        // Load the selected map directly (HTP lives on each story scene).
        SceneFader.LoadHoldBlack(scene);
    }

    private void ResolveSceneRefs()
    {
        if (homeCanvas == null)
        {
            Transform found = transform.root.Find("HomeMenuCanvas");
            if (found == null)
            {
                GameObject go = GameObject.Find("HomeMenuCanvas");
                if (go != null)
                    found = go.transform;
            }

            if (found != null)
                homeCanvas = found.gameObject;
        }

        if (homeCanvas == null)
            return;

        Canvas canvas = homeCanvas.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 500);

        if (homeContent == null)
        {
            Transform content = homeCanvas.transform.Find("HomeContent");
            if (content != null)
                homeContent = content.gameObject;
            else
                homeContent = homeCanvas;
        }

        if (playButton == null)
            playButton = FindButton("PlayButton");
        if (chaosButton == null)
            chaosButton = FindButton("ChaosButton");
        if (directoryButton == null)
            directoryButton = FindButton("DirectoryButton");
    }

    private Button FindButton(string name)
    {
        if (homeContent != null)
        {
            Transform t = homeContent.transform.Find(name);
            if (t != null)
                return t.GetComponent<Button>();
        }

        if (homeCanvas != null)
        {
            Transform t = homeCanvas.transform.Find(name);
            if (t != null)
                return t.GetComponent<Button>();

            t = homeCanvas.transform.Find("HomeContent/" + name);
            if (t != null)
                return t.GetComponent<Button>();
        }

        return null;
    }

    private void WireButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayPressed);
        }

        if (chaosButton != null)
        {
            chaosButton.onClick.RemoveAllListeners();
            chaosButton.onClick.AddListener(OnChaosPressed);
        }

        if (directoryButton != null)
        {
            directoryButton.onClick.RemoveAllListeners();
            directoryButton.onClick.AddListener(OnDirectoryPressed);
        }
    }
}
