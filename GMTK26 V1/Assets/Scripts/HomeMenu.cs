using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title / home scene UI. Uses scene Hierarchy objects so you can edit them in the Inspector.
/// </summary>
public class HomeMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private ChickenDirectoryCatalog directoryCatalog;

    [Header("Scene UI (assign in Hierarchy)")]
    [SerializeField] private GameObject homeCanvas;
    [SerializeField] private GameObject homeContent;
    [SerializeField] private Button playButton;
    [SerializeField] private Button chaosButton;
    [SerializeField] private Button directoryButton;

    private bool loading;
    private ChickenDirectoryUI directoryUi;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneFader.EnsureExists();
        SceneFader.ClearBusy();
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.SetPaused(false);

        ResolveSceneRefs();
        WireButtons();
    }

    public void OnPlayPressed()
    {
        StartGame(chaos: false);
    }

    public void OnChaosPressed()
    {
        StartGame(chaos: true);
    }

    public void OnDirectoryPressed()
    {
        if (loading || directoryUi != null)
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

    private void StartGame(bool chaos)
    {
        if (loading)
            return;

        SceneFader.ClearBusy();
        loading = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (chaos)
        {
            GameMode.SetChaos();
        }
        else
        {
            GameMode.SetStory();
            GameMode.PendingHowToPlay = true;
            SceneFader.LoadHoldBlack(gameSceneName);
            return;
        }

        SceneFader.Load(gameSceneName);
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
                homeContent = homeCanvas; // fallback: hide whole canvas menu layer carefully via content
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

            // Nested under HomeContent
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
