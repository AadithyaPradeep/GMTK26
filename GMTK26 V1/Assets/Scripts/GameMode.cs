using UnityEngine;

/// <summary>
/// Menu selection: Story (normal waves) vs Chaos (bomb rush + gun).
/// </summary>
public static class GameMode
{
    public const string PrefsKey = "GMTK26_GameMode";
    public const string Story = "story";
    public const string Chaos = "chaos";

    public static bool IsChaos => PlayerPrefs.GetString(PrefsKey, Story) == Chaos;

    public static void SetStory()
    {
        PlayerPrefs.SetString(PrefsKey, Story);
        PlayerPrefs.Save();
    }

    public static void SetChaos()
    {
        PlayerPrefs.SetString(PrefsKey, Chaos);
        PlayerPrefs.Save();
    }
}
