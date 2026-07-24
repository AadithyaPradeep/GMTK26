using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickenSpawner : MonoBehaviour
{
    public GameObject spawnps;

    [Header("Prefabs")]
    [SerializeField] public GameObject[] normalChickenPrefabs;
    [SerializeField] public GameObject[] bombChickenPrefabs;
    [SerializeField] public GameObject[] mindChickenPrefabs;
    [SerializeField] public GameObject[] electricChickenPrefabs;

    [Header("Flee Target")]
    [Tooltip("Assigned to every spawned chicken so clones can flee. Prefabs cannot reference a scene Farmer themselves.")]
    [SerializeField] public Transform farmerTransform;

    [Header("Spawn Area")]
    [SerializeField] public Vector2 spawnAreaMin = new Vector2(-7.5f, -4.5f);
    [SerializeField] public Vector2 spawnAreaMax = new Vector2(7.5f, 4.5f);

    [Header("Initial Batch")]
    [SerializeField] public int initialSpawnCountMin = 4;
    [SerializeField] public int initialSpawnCountMax = 8;
    [SerializeField] public int maxChickenCount = 24;

    [Header("Timed Waves")]
    [Tooltip("Pause after the initial batch before wave 1.")]
    [SerializeField] private float delayBeforeFirstWave = 2f;
    [SerializeField] private int baseWaveSpawnMin = 3;
    [SerializeField] private int baseWaveSpawnMax = 5;
    [Tooltip("Extra chickens added to each later wave.")]
    [SerializeField] private int extraSpawnsPerWave = 1;
    [SerializeField] private float burstSpawnGapMin = 0.25f;
    [SerializeField] private float burstSpawnGapMax = 0.55f;
    [SerializeField] private float breakDurationStart = 8f;
    [SerializeField] private float breakShortenPerWave = 0.5f;
    [SerializeField] private float minBreakDuration = 3f;

    [Header("Spawn Mix")]
    [Tooltip("Target bombs per 10 chickens on screen.")]
    [SerializeField] private int bombsPerTenChickens = 6;
    [Tooltip("Target mind clucks per 10 chickens on screen.")]
    [SerializeField] private int mindsPerTenChickens = 3;
    [Tooltip("Target electric chickens per 10 chickens on screen.")]
    [SerializeField] private int electricsPerTenChickens = 2;

    private readonly List<GameObject> liveChickens = new List<GameObject>();
    private Vector2 spawnPos;

    /// <summary>1-based wave index. 0 before the first wave starts.</summary>
    public int CurrentWave { get; private set; }

    private void Start()
    {
        if (farmerTransform == null)
        {
            GameObject farmer = GameObject.Find("Farmer");
            if (farmer != null)
                farmerTransform = farmer.transform;
            else
                Debug.LogWarning("ChickenSpawner: Farmer Transform is not assigned — chickens will not flee.", this);
        }

        int initialCount = Random.Range(initialSpawnCountMin, initialSpawnCountMax + 1);
        for (int i = 0; i < initialCount; i++)
            SpawnChicken();

        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        CurrentWave = 0;

        if (delayBeforeFirstWave > 0f)
            yield return new WaitForSeconds(delayBeforeFirstWave);

        while (enabled)
        {
            CurrentWave++;

            int toSpawn = Random.Range(baseWaveSpawnMin, baseWaveSpawnMax + 1)
                          + (CurrentWave - 1) * Mathf.Max(0, extraSpawnsPerWave);

            for (int i = 0; i < toSpawn; i++)
            {
                PruneDestroyedChickens();
                if (liveChickens.Count < maxChickenCount)
                    SpawnChicken();

                float gap = Random.Range(burstSpawnGapMin, burstSpawnGapMax);
                if (gap > 0f)
                    yield return new WaitForSeconds(gap);
            }

            float breakTime = Mathf.Max(
                minBreakDuration,
                breakDurationStart - (CurrentWave - 1) * breakShortenPerWave
            );
            yield return new WaitForSeconds(breakTime);
        }
    }

    private void SpawnChicken()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null)
            return;

        spawnPos = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        StartCoroutine(Spawn());
        GameObject chicken = Instantiate(prefab, spawnPos, Quaternion.identity);

        ChickenWander wander = chicken.GetComponent<ChickenWander>();
        if (wander != null)
        {
            wander.SetWanderArea(spawnAreaMin, spawnAreaMax);
            wander.farmerTransform = farmerTransform;
        }

        liveChickens.Add(chicken);
    }

    private GameObject PickPrefab()
    {
        bool hasNormal = HasAnyPrefab(normalChickenPrefabs);
        bool hasBomb = HasAnyPrefab(bombChickenPrefabs);
        bool hasMind = HasAnyPrefab(mindChickenPrefabs);
        bool hasElectric = HasAnyPrefab(electricChickenPrefabs);

        if (!hasNormal && !hasBomb && !hasMind && !hasElectric)
            return null;

        // Special types: bombs, electric, then mind — otherwise normal.
        if (hasBomb && CanSpawnSpecial(CountLiveBombs(), bombsPerTenChickens) &&
            RollSpecial(CountLiveBombs(), bombsPerTenChickens))
            return PickRandomPrefab(bombChickenPrefabs);

        if (hasElectric && CanSpawnSpecial(CountLiveElectrics(), electricsPerTenChickens) &&
            RollSpecial(CountLiveElectrics(), electricsPerTenChickens))
            return PickRandomPrefab(electricChickenPrefabs);

        if (hasMind && CanSpawnSpecial(CountLiveMinds(), mindsPerTenChickens) &&
            RollSpecial(CountLiveMinds(), mindsPerTenChickens))
            return PickRandomPrefab(mindChickenPrefabs);

        if (hasNormal)
            return PickRandomPrefab(normalChickenPrefabs);
        if (hasBomb)
            return PickRandomPrefab(bombChickenPrefabs);
        if (hasElectric)
            return PickRandomPrefab(electricChickenPrefabs);
        return PickRandomPrefab(mindChickenPrefabs);
    }

    private bool CanSpawnSpecial(int currentCount, int perTen)
    {
        int total = liveChickens.Count;
        int targetPerTen = Mathf.Max(0, perTen);
        return (currentCount + 1) * 10 <= (total + 1) * targetPerTen;
    }

    private bool RollSpecial(int currentCount, int perTen)
    {
        int total = liveChickens.Count;
        float targetRatio = perTen / 10f;

        float chance = targetRatio;
        if (total == 0 || (float)currentCount / Mathf.Max(1, total) < targetRatio)
            chance = Mathf.Min(0.9f, targetRatio + 0.35f);

        return Random.value < chance;
    }

    private int CountLiveBombs()
    {
        return CountLiveMatching(bombChickenPrefabs);
    }

    private int CountLiveMinds()
    {
        return CountLiveMatching(mindChickenPrefabs);
    }

    private int CountLiveElectrics()
    {
        return CountLiveMatching(electricChickenPrefabs);
    }

    private int CountLiveMatching(GameObject[] prefabs)
    {
        int count = 0;
        for (int i = 0; i < liveChickens.Count; i++)
        {
            GameObject chicken = liveChickens[i];
            if (chicken != null && MatchesPrefabList(chicken, prefabs))
                count++;
        }
        return count;
    }

    private static bool MatchesPrefabList(GameObject chicken, GameObject[] prefabs)
    {
        if (prefabs == null)
            return false;

        string instanceName = chicken.name;
        const string cloneSuffix = "(Clone)";
        if (instanceName.EndsWith(cloneSuffix))
            instanceName = instanceName.Substring(0, instanceName.Length - cloneSuffix.Length);

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab != null && prefab.name == instanceName)
                return true;
        }

        return false;
    }

    private static bool HasAnyPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return false;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
                return true;
        }

        return false;
    }

    private static GameObject PickRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab != null)
                return prefab;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
                return prefabs[i];
        }

        return null;
    }

    private void PruneDestroyedChickens()
    {
        for (int i = liveChickens.Count - 1; i >= 0; i--)
        {
            if (liveChickens[i] == null)
                liveChickens.RemoveAt(i);
        }
    }

    private IEnumerator Spawn()
    {
        if (spawnps == null)
            yield break;

        GameObject spawnS = Instantiate(spawnps, spawnPos, Quaternion.identity);
        yield return new WaitForSeconds(0.7f);
        Destroy(spawnS);
    }
}
