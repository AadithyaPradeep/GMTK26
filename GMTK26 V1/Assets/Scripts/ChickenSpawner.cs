using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickenSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] public GameObject[] chickenPrefabs;

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

    private readonly List<GameObject> liveChickens = new List<GameObject>();

    private void Start()
    {
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
        if (chickenPrefabs == null || chickenPrefabs.Length == 0)
            return;

        GameObject prefab = chickenPrefabs[Random.Range(0, chickenPrefabs.Length)];
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
        }

        liveChickens.Add(chicken);
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
