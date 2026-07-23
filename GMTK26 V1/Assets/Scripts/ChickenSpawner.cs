using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickenSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] public GameObject[] normalChickenPrefabs;
    [SerializeField] public GameObject[] bombChickenPrefabs;

    [Header("Flee Target")]
    [Tooltip("Assigned to every spawned chicken so clones can flee. Prefabs cannot reference a scene Farmer themselves.")]
    [SerializeField] public Transform farmerTransform;

    [Header("Spawn Area")]
    [SerializeField] public Vector2 spawnAreaMin = new Vector2(-5f, -5f);
    [SerializeField] public Vector2 spawnAreaMax = new Vector2(5f, 5f);

    [Header("Initial Batch")]
    [SerializeField] public int initialSpawnCountMin = 5;
    [SerializeField] public int initialSpawnCountMax = 10;

    [Header("Ongoing Spawns")]
    [SerializeField] public float minSpawnInterval = 3f;
    [SerializeField] public float maxSpawnInterval = 8f;
    [SerializeField] public int maxChickenCount = 30;

    [Header("Spawn Mix")]
    [Tooltip("Target bombs per 10 chickens on screen (3 => about 30% bombs, and normals stay in the majority).")]
    [SerializeField] private int bombsPerTenChickens = 3;

    private readonly List<GameObject> liveChickens = new List<GameObject>();

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

        Vector2 spawnPos = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

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

        if (!hasNormal && !hasBomb)
            return null;
        if (!hasBomb)
            return PickRandomPrefab(normalChickenPrefabs);
        if (!hasNormal)
            return PickRandomPrefab(bombChickenPrefabs);

        if (CanSpawnBomb() && Random.value < bombsPerTenChickens / 10f)
            return PickRandomPrefab(bombChickenPrefabs);

        return PickRandomPrefab(normalChickenPrefabs);
    }

    private bool CanSpawnBomb()
    {
        int total = liveChickens.Count;
        int bombs = CountLiveBombs();
        int normals = total - bombs;

        // After spawning a bomb: normals must still outnumber bombs.
        if (normals <= bombs + 1)
            return false;

        // Keep bombs at or under the target ratio (e.g. 3 per 10).
        int targetPerTen = Mathf.Max(0, bombsPerTenChickens);
        return (bombs + 1) * 10 <= (total + 1) * targetPerTen;
    }

    private int CountLiveBombs()
    {
        int bombs = 0;
        for (int i = 0; i < liveChickens.Count; i++)
        {
            GameObject chicken = liveChickens[i];
            if (chicken != null && IsBombInstance(chicken))
                bombs++;
        }
        return bombs;
    }

    private bool IsBombInstance(GameObject chicken)
    {
        if (bombChickenPrefabs == null)
            return false;

        string instanceName = chicken.name;
        const string cloneSuffix = "(Clone)";
        if (instanceName.EndsWith(cloneSuffix))
            instanceName = instanceName.Substring(0, instanceName.Length - cloneSuffix.Length);

        for (int i = 0; i < bombChickenPrefabs.Length; i++)
        {
            GameObject prefab = bombChickenPrefabs[i];
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

        // Rare null slots — retry a few times rather than failing the spawn.
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
}
