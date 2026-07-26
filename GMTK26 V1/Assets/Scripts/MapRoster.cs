using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Home-side roster of chickens per map for the map-select preview.
/// Mirrors ChickenSpawner unlock waves; portraits/text come from ChickenDirectoryCatalog.
/// </summary>
public static class MapRoster
{
    public readonly struct Slot
    {
        public readonly string DirectoryName;
        public readonly int UnlockWave;
        /// <summary>0 = story has no single wave (e.g. chaos endless).</summary>
        public readonly bool ChaosEndless;

        public Slot(string directoryName, int unlockWave, bool chaosEndless = false)
        {
            DirectoryName = directoryName;
            UnlockWave = unlockWave;
            ChaosEndless = chaosEndless;
        }
    }

    public static IReadOnlyList<Slot> GetSlots(string mapId, bool chaos)
    {
        if (chaos)
            return ChaosSlots;

        if (mapId == GameMode.ComboId)
            return ComboSlots;

        if (mapId == GameMode.DuskId)
            return DuskSlots;

        if (mapId == GameMode.GraveyardId)
            return GraveyardSlots;

        return FarmSlots;
    }

    private static readonly Slot[] ChaosSlots =
    {
        new Slot("Bomb Cluck", 0, chaosEndless: true),
        new Slot("Electric Cluck", 0, chaosEndless: true),
    };

    // SampleScene ChickenSpawner unlocks.
    private static readonly Slot[] FarmSlots =
    {
        new Slot("Regular Cluck", 1),
        new Slot("Panic Cluck", 2),
        new Slot("Bomb Cluck", 1),
        new Slot("Rogue Cluck", 2),
        new Slot("Mind Cluck", 2),
        new Slot("Electric Cluck", 3),
        new Slot("Laser Cluck", 4),
    };

    // World2 ChickenSpawner unlocks.
    private static readonly Slot[] DuskSlots =
    {
        new Slot("Regular Cluck", 1),
        new Slot("Panic Cluck", 2),
        new Slot("Blue Fire Cluck", 1),
        new Slot("Rogue Cluck", 2),
        new Slot("Alien Cluck", 3),
        new Slot("Fire Cluck", 3),
    };

    // World3 ChickenSpawner unlocks.
    private static readonly Slot[] GraveyardSlots =
    {
        new Slot("Regular Cluck", 1),
        new Slot("Panic Cluck", 2),
        new Slot("Zombie Cluck (looks like a christmas chicken tho)", 1),
        new Slot("Rogue Zombie Cluck", 2),
        new Slot("Skele Cluck", 3),
        new Slot("Ghost Cluck", 4),
    };

    private static readonly Slot[] ComboSlots = BuildCombo();

    private static Slot[] BuildCombo()
    {
        var byName = new Dictionary<string, Slot>();
        MergeEarliest(byName, FarmSlots);
        MergeEarliest(byName, DuskSlots);
        MergeEarliest(byName, GraveyardSlots);

        var list = new List<Slot>(byName.Count);
        foreach (var kv in byName)
            list.Add(kv.Value);

        list.Sort((a, b) =>
        {
            int wave = a.UnlockWave.CompareTo(b.UnlockWave);
            return wave != 0 ? wave : string.CompareOrdinal(a.DirectoryName, b.DirectoryName);
        });
        return list.ToArray();
    }

    private static void MergeEarliest(Dictionary<string, Slot> into, Slot[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            if (!into.TryGetValue(s.DirectoryName, out Slot existing) || s.UnlockWave < existing.UnlockWave)
                into[s.DirectoryName] = s;
        }
    }
}
