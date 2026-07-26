using UnityEngine;

public enum ChickenDirectoryRole
{
    Ally,
    Threat,
    Boss
}

[CreateAssetMenu(fileName = "ChickenEntry", menuName = "Exploding Chickens/Directory Entry", order = 1)]
public class ChickenDirectoryEntry : ScriptableObject
{
    public string displayName = "Cluck";
    public Sprite portrait;
    public Color portraitColor = Color.white;
    [TextArea(2, 4)] public string shortDescription = "Placeholder description.";
    [TextArea(6, 20)] public string story = "Placeholder story. Replace me.";
    public ChickenDirectoryRole role = ChickenDirectoryRole.Ally;
    public string worldHint = "";
}
