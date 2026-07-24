using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave-based chicken spawner.
/// Timer to the next wave starts when the wave starts.
/// Normals/minds spawn at wave start.
/// Wave bomb/electric counts spawn all together; extra mid-wave lethals use a cooldown.
/// </summary>
public class ChickenSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        [Tooltip("Optional label for the Inspector (e.g. Wave 1 - Bomb intro).")]
        public string name = "Wave";

        [Tooltip("Spawned at the start of the wave only.")]
        public int normals = 2;

        [Tooltip("Bombs that spawn all together when the mid-wave lethal phase starts.")]
        public int bombs;

        [Tooltip("Extra bombs during the wave — spawned one-by-one with midLethalGap.")]
        public int extraBombs;

        [Tooltip("Spawned at the start of the wave only.")]
        public int minds;

        [Tooltip("Electrics that spawn all together when the mid-wave lethal phase starts.")]
        public int electrics;

        [Tooltip("Extra electrics during the wave — spawned one-by-one with midLethalGap.")]
        public int extraElectrics;

        [Tooltip("Length of this wave. Next-wave countdown starts when the wave starts.")]
        public float breakAfterSeconds = 12f;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject[] normalChickenPrefabs;
    [SerializeField] private GameObject[] bombChickenPrefabs;
    [SerializeField] private GameObject[] mindChickenPrefabs;
    [SerializeField] private GameObject[] electricChickenPrefabs;
    [SerializeField] private GameObject spawnEffect;

    [Header("References")]
    [SerializeField] private Transform farmerTransform;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-7.5f, -4.5f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(7.5f, 4.5f);

    [Header("Opening")]
    [SerializeField] private int startingNormals = 3;
    [SerializeField] private float delayBeforeFirstWave = 5f;

    [Header("Waves (edit these)")]
    [SerializeField] private Wave[] waves =
    {
        new Wave { name = "1 - Bomb intro", normals = 2, bombs = 1, extraBombs = 0, minds = 0, electrics = 0, extraElectrics = 0, breakAfterSeconds = 14f },
        new Wave { name = "2 - More bombs", normals = 3, bombs = 2, extraBombs = 0, minds = 0, electrics = 0, extraElectrics = 0, breakAfterSeconds = 13f },
        new Wave { name = "3 - Mind unlock", normals = 4, bombs = 2, extraBombs = 1, minds = 1, electrics = 0, extraElectrics = 0, breakAfterSeconds = 12f },
        new Wave { name = "4 - Electric unlock", normals = 4, bombs = 2, extraBombs = 1, minds = 1, electrics = 1, extraElectrics = 0, breakAfterSeconds = 12f },
        new Wave { name = "5", normals = 5, bombs = 3, extraBombs = 1, minds = 1, electrics = 1, extraElectrics = 1, breakAfterSeconds = 11f },
        new Wave { name = "6", normals = 6, bombs = 3, extraBombs = 2, minds = 1, electrics = 1, extraElectrics = 1, breakAfterSeconds = 10f },
    };

    [Header("After Last Scripted Wave")]
    [SerializeField] private int maxBombsPerWave = 4;
    [SerializeField] private int maxExtraBombsPerWave = 2;
    [SerializeField] private int maxMindsPerWave = 1;
    [SerializeField] private int maxElectricsPerWave = 1;
    [SerializeField] private int maxExtraElectricsPerWave = 1;
    [SerializeField] private float minBreakSeconds = 8f;

    [Header("Spawn Timing")]
    [SerializeField] private float openingSpawnGap = 0.4f;
    [Tooltip("Cooldown between each extra mid-wave lethal spawn.")]
    [SerializeField] private float midLethalGap = 2.5f;
    [Tooltip("Pause after normals/minds before the wave bomb/electric burst.")]
    [SerializeField] private float pauseBeforeMidLethals = 2f;
    [SerializeField] private int maxChickensOnScreen = 24;

    private readonly List<GameObject> liveChickens = new List<GameObject>();
    private Vector2 lastSpawnPos;

    public int CurrentWave { get; private set; }
    public bool IsWaitingForNextWave { get; private set; }
    public float SecondsUntilNextWave { get; private set; }
    public int NextWaveNumber => CurrentWave + 1;

    private void Start()
    {
        if (farmerTransform == null)
        {
            GameObject farmer = GameObject.Find("Farmer");
            if (farmer != null)
                farmerTransform = farmer.transform;
        }

        StartCoroutine(RunGame());
    }

    private IEnumerator RunGame()
    {
        CurrentWave = 0;

        yield return SpawnBatch(BuildOpeningList(startingNormals), openingSpawnGap);

        if (delayBeforeFirstWave > 0f)
            yield return WaitCountdown(delayBeforeFirstWave);

        while (enabled)
        {
            CurrentWave++;
            yield return RunWave(GetWave(CurrentWave));
        }
    }

    private IEnumerator RunWave(Wave wave)
    {
        float duration = Mathf.Max(minBreakSeconds, wave.breakAfterSeconds);

        // Next-wave timer starts as soon as this wave starts.
        IsWaitingForNextWave = true;
        SecondsUntilNextWave = duration;

        // Normals + minds at the start (timer already ticking).
        yield return SpawnBatchWhileTicking(BuildOpeningList(wave.normals, wave.minds), openingSpawnGap);

        // Short pause, then spawn the wave's bomb/electric counts all together.
        yield return TickForSeconds(pauseBeforeMidLethals);
        SpawnLethalBurst(wave.bombs, wave.electrics);

        int extraBombsSpawned = 0;
        int extraElectricsSpawned = 0;
        float lethalCooldown = midLethalGap;

        while (SecondsUntilNextWave > 0f)
        {
            float dt = Time.deltaTime;
            SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - dt);
            lethalCooldown -= dt;

            Prune();

            bool needExtraBomb = extraBombsSpawned < wave.extraBombs;
            bool needExtraElectric = extraElectricsSpawned < wave.extraElectrics;

            // Excess mid-wave lethals: one at a time with cooldown.
            if ((needExtraBomb || needExtraElectric) && lethalCooldown <= 0f &&
                liveChickens.Count < maxChickensOnScreen)
            {
                if (needExtraBomb && TrySpawnFrom(bombChickenPrefabs))
                    extraBombsSpawned++;
                else if (needExtraElectric && TrySpawnFrom(electricChickenPrefabs))
                    extraElectricsSpawned++;

                lethalCooldown = midLethalGap;
            }

            yield return null;
        }

        IsWaitingForNextWave = false;
        SecondsUntilNextWave = 0f;
    }

    private void SpawnLethalBurst(int bombs, int electrics)
    {
        // All wave-count bombs/electrics spawn together (same moment).
        for (int i = 0; i < bombs; i++)
        {
            Prune();
            if (liveChickens.Count >= maxChickensOnScreen)
                break;
            TrySpawnFrom(bombChickenPrefabs);
        }

        for (int i = 0; i < electrics; i++)
        {
            Prune();
            if (liveChickens.Count >= maxChickensOnScreen)
                break;
            TrySpawnFrom(electricChickenPrefabs);
        }
    }

    private IEnumerator TickForSeconds(float seconds)
    {
        float left = seconds;
        while (left > 0f && SecondsUntilNextWave > 0f)
        {
            float dt = Time.deltaTime;
            left -= dt;
            SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - dt);
            yield return null;
        }
    }

    private IEnumerator WaitCountdown(float seconds)
    {
        IsWaitingForNextWave = true;
        SecondsUntilNextWave = Mathf.Max(0f, seconds);

        while (SecondsUntilNextWave > 0f)
        {
            yield return null;
            SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - Time.deltaTime);
        }

        IsWaitingForNextWave = false;
        SecondsUntilNextWave = 0f;
    }

    private IEnumerator SpawnBatchWhileTicking(List<GameObject> prefabs, float gap)
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            Prune();
            if (liveChickens.Count < maxChickensOnScreen)
                SpawnOne(prefabs[i]);

            float wait = gap;
            while (wait > 0f && SecondsUntilNextWave > 0f)
            {
                float dt = Time.deltaTime;
                wait -= dt;
                SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - dt);
                yield return null;
            }
        }
    }

    private Wave GetWave(int waveNumber)
    {
        if (waves != null && waveNumber >= 1 && waveNumber <= waves.Length)
            return waves[waveNumber - 1];

        Wave last = (waves != null && waves.Length > 0)
            ? waves[waves.Length - 1]
            : new Wave { normals = 4, bombs = 2, extraBombs = 1, minds = 1, electrics = 1, extraElectrics = 0, breakAfterSeconds = 12f };

        int extra = waveNumber - Mathf.Max(1, waves != null ? waves.Length : 1);

        return new Wave
        {
            name = waveNumber.ToString(),
            normals = last.normals + extra,
            bombs = Mathf.Min(maxBombsPerWave, last.bombs + extra / 2),
            extraBombs = Mathf.Min(maxExtraBombsPerWave, last.extraBombs + extra / 3),
            minds = Mathf.Min(maxMindsPerWave, Mathf.Max(last.minds, 1)),
            electrics = Mathf.Min(maxElectricsPerWave, Mathf.Max(last.electrics, 1)),
            extraElectrics = Mathf.Min(maxExtraElectricsPerWave, last.extraElectrics),
            breakAfterSeconds = Mathf.Max(minBreakSeconds, last.breakAfterSeconds - extra * 0.25f)
        };
    }

    private List<GameObject> BuildOpeningList(int normals, int minds = 0)
    {
        var list = new List<GameObject>();
        Add(list, normalChickenPrefabs, normals);
        Add(list, mindChickenPrefabs, minds);
        Shuffle(list);
        return list;
    }

    private static void Add(List<GameObject> list, GameObject[] prefabs, int count)
    {
        GameObject prefab = FirstValid(prefabs);
        if (prefab == null || count <= 0)
            return;

        for (int i = 0; i < count; i++)
            list.Add(prefab);
    }

    private static GameObject FirstValid(GameObject[] prefabs)
    {
        if (prefabs == null)
            return null;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
                return prefabs[i];
        }

        return null;
    }

    private static void Shuffle(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            GameObject tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private IEnumerator SpawnBatch(List<GameObject> prefabs, float gap)
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            Prune();
            if (liveChickens.Count < maxChickensOnScreen)
                SpawnOne(prefabs[i]);

            if (gap > 0f)
                yield return new WaitForSeconds(gap);
        }
    }

    private bool TrySpawnFrom(GameObject[] prefabs)
    {
        GameObject prefab = FirstValid(prefabs);
        if (prefab == null)
            return false;

        SpawnOne(prefab);
        return true;
    }

    private void SpawnOne(GameObject prefab)
    {
        if (prefab == null)
            return;

        lastSpawnPos = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        if (spawnEffect != null)
            StartCoroutine(PlaySpawnEffect(lastSpawnPos));

        GameObject chicken = Instantiate(prefab, lastSpawnPos, Quaternion.identity);

        ChickenWander wander = chicken.GetComponent<ChickenWander>();
        if (wander != null)
        {
            wander.SetWanderArea(spawnAreaMin, spawnAreaMax);
            wander.farmerTransform = farmerTransform;
        }

        liveChickens.Add(chicken);
    }

    private IEnumerator PlaySpawnEffect(Vector2 pos)
    {
        GameObject fx = Instantiate(spawnEffect, pos, Quaternion.identity);
        yield return new WaitForSeconds(0.7f);
        if (fx != null)
            Destroy(fx);
    }

    private void Prune()
    {
        for (int i = liveChickens.Count - 1; i >= 0; i--)
        {
            if (liveChickens[i] == null)
                liveChickens.RemoveAt(i);
        }
    }
}
