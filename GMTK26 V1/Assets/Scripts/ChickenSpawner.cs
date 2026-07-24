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

    [Header("Flee Target")]
    [Tooltip("Assigned to every spawned chicken so clones can flee. Prefabs cannot reference a scene Farmer themselves.")]
    [SerializeField] public Transform farmerTransform;

    [Header("Spawn Area")]
    [SerializeField] public Vector2 spawnAreaMin = new Vector2(-7.5f, -4.5f);
    [SerializeField] public Vector2 spawnAreaMax = new Vector2(7.5f, 4.5f);

    [Header("Initial Batch")]
    [SerializeField] public int initialSpawnCountMin = 4;
    [SerializeField] public int initialSpawnCountMax = 8;

    [Header("Ongoing Spawns")]
    [SerializeField] public float minSpawnInterval = 1.5f;
    [SerializeField] public float maxSpawnInterval = 3.5f;
    [SerializeField] public int maxChickenCount = 24;

    [Header("Spawn Mix")]
    [Tooltip("Target bombs per 10 chickens on screen.")]
    [SerializeField] private int bombsPerTenChickens = 6;
    [Tooltip("Target mind clucks per 10 chickens on screen.")]
    [SerializeField] private int mindsPerTenChickens = 3;

    private readonly List<GameObject> liveChickens = new List<GameObject>();
    private Vector2 spawnPos;

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

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            PruneDestroyedChickens();
            if (liveChickens.Count < maxChickenCount)
                SpawnChicken();
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

        StartCoroutine("Spawn");
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

        if (!hasNormal && !hasBomb && !hasMind)
            return null;

        // Special types first (bombs, then mind), otherwise normal.
        if (hasBomb && CanSpawnSpecial(CountLiveBombs(), bombsPerTenChickens) &&
            RollSpecial(CountLiveBombs(), bombsPerTenChickens))
            return PickRandomPrefab(bombChickenPrefabs);

        if (hasMind && CanSpawnSpecial(CountLiveMinds(), mindsPerTenChickens) &&
            RollSpecial(CountLiveMinds(), mindsPerTenChickens))
            return PickRandomPrefab(mindChickenPrefabs);

        if (hasNormal)
            return PickRandomPrefab(normalChickenPrefabs);
        if (hasBomb)
            return PickRandomPrefab(bombChickenPrefabs);
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
        GameObject spawnS = Instantiate(spawnps, spawnPos, Quaternion.identity);
        yield return new WaitForSeconds(0.7f);
        Destroy(spawnS);
    }
}
