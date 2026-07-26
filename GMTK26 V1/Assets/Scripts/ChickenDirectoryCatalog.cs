using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChickenDirectoryCatalog", menuName = "Exploding Chickens/Directory Catalog", order = 0)]
public class ChickenDirectoryCatalog : ScriptableObject
{
    [SerializeField] private List<ChickenDirectoryEntry> entries = new List<ChickenDirectoryEntry>();

    public IReadOnlyList<ChickenDirectoryEntry> Entries => entries;

    public ChickenDirectoryEntry FindByDisplayName(string displayName)
    {
        if (entries == null || string.IsNullOrEmpty(displayName))
            return null;

        for (int i = 0; i < entries.Count; i++)
        {
            ChickenDirectoryEntry e = entries[i];
            if (e != null && e.displayName == displayName)
                return e;
        }

        return null;
    }

    public static ChickenDirectoryCatalog LoadOrCreateDefaults()
    {
        ChickenDirectoryCatalog fromResources = Resources.Load<ChickenDirectoryCatalog>("ChickenDirectory/Catalog");
        if (fromResources != null && fromResources.entries != null && fromResources.entries.Count > 0)
            return fromResources;

        return CreateRuntimeDefaults();
    }

    public static ChickenDirectoryCatalog CreateRuntimeDefaults()
    {
        ChickenDirectoryCatalog catalog = CreateInstance<ChickenDirectoryCatalog>();
        catalog.name = "RuntimeChickenDirectoryCatalog";
        catalog.hideFlags = HideFlags.HideAndDontSave;
        catalog.entries = new List<ChickenDirectoryEntry>
        {
            Make("Regular Cluck", "CluckIdle", ChickenDirectoryRole.Ally, "World 1",
                "The classic farm bird. Protect these: if they all die, you lose.",
                "Nobody remembers who first called them Clucks. They just showed up one morning, pecking the dirt like they owned the place."),

            Make("Panic Cluck", "PanicHIdle", ChickenDirectoryRole.Ally, "World 1",
                "Always sprinting. Still counts as a regular chicken.",
                "Some Clucks never learned to idle. They run, and they keep running."),

            Make("Bomb Cluck", "BombCluck", ChickenDirectoryRole.Threat, "World 1",
                "Lit fuse. Explodes and takes nearby chickens with it.",
                "Someone strapped a fuse to a perfectly good chicken. Rude.\n\nSome say it once ate a bomb for lunch and just... committed to the bit. Scholars still argue which story is worse."),

            Make("Rogue Cluck", "BombCluck", ChickenDirectoryRole.Threat, "World 1",
                "A bomb that sprints like a panic: hard to catch, louder to ignore.",
                "Bomb Clucks that caught the panic bug. Worst of both worlds."),

            Make("Mind Cluck", "MCCLuckIdle", ChickenDirectoryRole.Threat, "World 1",
                "Pulls other chickens into its weird little gravity well.",
                "Too much thought for a bird. The others orbit whether they like it or not."),

            Make("Electric Cluck", "ECIdle", ChickenDirectoryRole.Threat, "World 1",
                "Charges up, then zaps everything in range.",
                "Got hit by lightning one day and bro became The Electro. Hasn't shut up about it since."),

            Make("Laser Cluck", "LaserIdle", ChickenDirectoryRole.Threat, "World 1",
                "Fires a deadly beam. Sometimes you need one on your side.",
                "Farm tech went too far. Or exactly far enough."),

            Make("Fire Cluck", "FireChickenHIdle", ChickenDirectoryRole.Threat, "World 2",
                "Lobbs fireballs. Hot temper, hotter aim.",
                "World 2 runs warmer. These birds brought matches."),

            Make("Blue Fire Cluck", "BlueFireChickenHIdle", ChickenDirectoryRole.Threat, "World 2",
                "Colder flame, same bad attitude.",
                "Blue means business. Or at least a different flavor of boom."),

            Make("Alien Cluck", "AlienHIdle", ChickenDirectoryRole.Threat, "World 2",
                "Abducts chickens into the sky. Friends and foes alike.",
                "Not from around here. The tractor beam is a dead giveaway."),

            Make("Zombie Cluck (looks like a christmas chicken tho)", "ZCluckIdle", ChickenDirectoryRole.Threat, "World 3",
                "Graveyard bomb bird. Counts down, then goes boom.",
                "Rose from the coop after a bad night and a worse diet. Still ticking. Still exploding. The festive colorway is an unfortunate coincidence, and it will not be taking questions."),

            Make("Rogue Zombie Cluck", "HighlighZCluckIdle", ChickenDirectoryRole.Threat, "World 3",
                "A zombie bomb that sprints. Catch it if you can. Survive it if you can't.",
                "Same undead fuse, plus cardio. Apparently dying once was not enough motivation, so now it jogs. You will not enjoy the group fitness class."),

            Make("Ghost Cluck", "GhostCluckIdle", ChickenDirectoryRole.Threat, "World 3",
                "Haunts the farmer with lights-out. Two scares, then it fades.",
                "Floats around like it pays rent in vibes. When the timer hits zero it turns the farm into a nightlight situation: you, a tiny circle of hope, and whatever is chewing on your ankles outside it. Then it peaces out forever. Rude, but iconic.",
                new Color(0f, 253f / 255f, 1f, 0.35f)),

            Make("Skele Cluck", "SkeleCluckIdle", ChickenDirectoryRole.Threat, "World 3",
                "Fires arrow volleys. Bones with aim.",
                "All rattle, no feathers. The arrows are the point."),
        };

        return catalog;
    }

    private static ChickenDirectoryEntry Make(
        string displayName,
        string portraitResourceName,
        ChickenDirectoryRole role,
        string worldHint,
        string shortDescription,
        string story,
        Color? portraitColor = null)
    {
        ChickenDirectoryEntry entry = CreateInstance<ChickenDirectoryEntry>();
        entry.hideFlags = HideFlags.HideAndDontSave;
        entry.name = displayName.Replace(" ", "");
        entry.displayName = displayName;
        entry.role = role;
        entry.worldHint = worldHint;
        entry.shortDescription = shortDescription;
        entry.story = story;
        entry.portrait = LoadPortrait(portraitResourceName);
        entry.portraitColor = portraitColor ?? Color.white;
        return entry;
    }

    private static Sprite LoadPortrait(string resourceName)
    {
        Sprite single = Resources.Load<Sprite>("ChickenDirectory/" + resourceName);
        if (single != null)
            return single;

        Sprite[] all = Resources.LoadAll<Sprite>("ChickenDirectory/" + resourceName);
        if (all != null && all.Length > 0)
            return all[0];

        // Fallback: any sprite whose name starts with the resource name.
        Sprite[] folder = Resources.LoadAll<Sprite>("ChickenDirectory");
        if (folder != null)
        {
            for (int i = 0; i < folder.Length; i++)
            {
                if (folder[i] != null && folder[i].name.StartsWith(resourceName))
                    return folder[i];
            }
        }

        return null;
    }
}
