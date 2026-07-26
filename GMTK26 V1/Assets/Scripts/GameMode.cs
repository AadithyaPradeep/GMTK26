using UnityEngine;

/// <summary>
/// Menu selection: Story vs Chaos, plus map pick (single map or Combo cycle).
/// Map catalog is ordered so a future Map 3 only needs a new entry.
/// </summary>
public static class GameMode
{
    public const string PrefsKey = "GMTK26_GameMode";
    public const string MapPrefsKey = "GMTK26_MapMode";
    public const string Story = "story";
    public const string Chaos = "chaos";

    public const string ComboId = "combo";
    public const string FarmId = "farm";
    public const string DuskId = "dusk";

    /// <summary>Set by Home → Play; consumed once when the first story world boots.</summary>
    public static bool PendingHowToPlay { get; set; }

    public static bool IsChaos => PlayerPrefs.GetString(PrefsKey, Story) == Chaos;

    public static bool IsCombo => CurrentMapId == ComboId;

    public static string CurrentMapId
    {
        get
        {
            string id = PlayerPrefs.GetString(MapPrefsKey, ComboId);
            if (string.IsNullOrEmpty(id))
                return ComboId;
            return id;
        }
    }

    /// <summary>Ordered story / chaos maps. Append here when adding Map 3.</summary>
    public static readonly MapInfo[] Maps =
    {
        new MapInfo(FarmId, "FARM", "SampleScene", "Daytime fields", chaosEligible: true),
        new MapInfo(DuskId, "DUSK", "World2", "Night tileset", chaosEligible: true),
    };

    public static void SetStory()
    {
        PlayerPrefs.SetString(PrefsKey, Story);
        PlayerPrefs.Save();
    }

    public static void SetChaos()
    {
        PlayerPrefs.SetString(PrefsKey, Chaos);
        PlayerPrefs.Save();
        PendingHowToPlay = false;
    }

    public static void SetMap(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            mapId = ComboId;

        PlayerPrefs.SetString(MapPrefsKey, mapId);
        PlayerPrefs.Save();
    }

    public static void SetSingleMap(string mapId)
    {
        if (!TryFindMap(mapId, out _))
            mapId = FarmId;
        SetMap(mapId);
    }

    public static void SetCombo()
    {
        SetMap(ComboId);
    }

    public static bool TryFindMap(string mapId, out MapInfo map)
    {
        map = default;
        if (string.IsNullOrEmpty(mapId) || mapId == ComboId)
            return false;

        for (int i = 0; i < Maps.Length; i++)
        {
            if (Maps[i].Id == mapId)
            {
                map = Maps[i];
                return true;
            }
        }

        return false;
    }

    public static bool TryFindMapByScene(string sceneName, out MapInfo map)
    {
        map = default;
        if (string.IsNullOrEmpty(sceneName))
            return false;

        for (int i = 0; i < Maps.Length; i++)
        {
            if (Maps[i].SceneName == sceneName)
            {
                map = Maps[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>Scene to load when leaving Home after a map pick.</summary>
    public static string StartSceneName
    {
        get
        {
            if (IsCombo)
                return Maps.Length > 0 ? Maps[0].SceneName : "SampleScene";

            return TryFindMap(CurrentMapId, out MapInfo map) ? map.SceneName : "SampleScene";
        }
    }

    /// <summary>
    /// Story finish-portal target for the active selection.
    /// Single map → same scene; Combo → next in catalog (wraps).
    /// </summary>
    public static string PortalTargetScene(string currentSceneName)
    {
        if (!IsCombo)
        {
            if (TryFindMap(CurrentMapId, out MapInfo single))
                return single.SceneName;

            if (TryFindMapByScene(currentSceneName, out MapInfo byScene))
                return byScene.SceneName;

            return "SampleScene";
        }

        if (Maps.Length == 0)
            return "SampleScene";

        int index = 0;
        for (int i = 0; i < Maps.Length; i++)
        {
            if (Maps[i].SceneName == currentSceneName)
            {
                index = i;
                break;
            }
        }

        int next = (index + 1) % Maps.Length;
        return Maps[next].SceneName;
    }

    public readonly struct MapInfo
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string SceneName;
        public readonly string OneLiner;
        public readonly bool ChaosEligible;

        public MapInfo(string id, string displayName, string sceneName, string oneLiner, bool chaosEligible)
        {
            Id = id;
            DisplayName = displayName;
            SceneName = sceneName;
            OneLiner = oneLiner;
            ChaosEligible = chaosEligible;
        }
    }
}
