using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Starts with protected normals, then endless waves.
/// Lethals (bombs / electrics) share a threat cap.
/// Minds are non-lethal with their own cap.
/// Panics are flock chickens: they count toward game over when killed.
/// Spawn chance uses percentages among unlocked types.
/// </summary>
public class ChickenSpawner : MonoBehaviour
{
    private enum ThreatKind
    {
        Bomb,
        Mind,
        Electric,
        Panic
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject[] normalChickenPrefabs;
    [SerializeField] private GameObject[] bombChickenPrefabs;
    [SerializeField] private GameObject[] mindChickenPrefabs;
    [SerializeField] private GameObject[] electricChickenPrefabs;
    [SerializeField] private GameObject[] panicChickenPrefabs;
    [SerializeField] private GameObject spawnEffect;

    [Header("References")]
    [SerializeField] private Transform farmerTransform;
    [SerializeField] private GameObject introBanner;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-7.5f, -4.5f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(7.5f, 4.5f);

    [Header("Opening")]
    [SerializeField] private int startingNormals = 8;
    [SerializeField] private float openingSpawnGap = 0.25f;
    [SerializeField] private float delayBeforeFirstWave = 3f;

    [Header("Waves")]
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private int mindUnlockWave = 2;
    [SerializeField] private int electricUnlockWave = 3;
    [SerializeField] private int panicUnlockWave = 1;
    [SerializeField] private int normalsAfterEachWave = 2;

    [Header("Spawn Chances %")]
    [SerializeField] [Range(0f, 100f)] private float bombSpawnPercent = 60f;
    [SerializeField] [Range(0f, 100f)] private float mindSpawnPercent = 15f;
    [SerializeField] [Range(0f, 100f)] private float electricSpawnPercent = 15f;
    [SerializeField] [Range(0f, 100f)] private float panicSpawnPercent = 15f;

    [Header("Difficulty")]
    [SerializeField] private float startSpawnInterval = 4f;
    [SerializeField] private float minSpawnInterval = 1.2f;
    [SerializeField] private float intervalDecreasePerWave = 0.3f;
    [SerializeField] private int startMaxThreats = 4;
    [SerializeField] private int maxThreatIncreasePerWave = 1;
    [SerializeField] private int hardMaxThreats = 18;
    [SerializeField] private int maxMindsOnScreen = 2;
    [SerializeField] private int maxPanicsPerWave = 2;
    [SerializeField] private int startSpawnBurst = 1;
    [SerializeField] private int wavesPerBurstIncrease = 3;
    [SerializeField] private int hardMaxSpawnBurst = 4;

    private readonly List<GameObject> protectedNormals = new List<GameObject>();
    private readonly List<GameObject> lethals = new List<GameObject>();
    private readonly List<GameObject> minds = new List<GameObject>();
    private readonly List<GameObject> panics = new List<GameObject>();

    public int CurrentWave { get; private set; }
    public float SecondsUntilNextWave { get; private set; }
    public bool IsWaveActive { get; private set; }
    public bool IsGameOver { get; private set; }
    public int ProtectedAlive => CountAlive(protectedNormals);

    public bool IsWaitingForNextWave => IsWaveActive && !IsGameOver;

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

    private void Update()
    {
        if (IsGameOver || protectedNormals.Count == 0)
            return;

        Prune(protectedNormals);
        if (ProtectedAlive == 0)
            EndGame();
    }

    private IEnumerator RunGame()
    {
        StartCoroutine(HideIntroBannerAfterDelay(2f));

        yield return SpawnProtectedNormals(startingNormals);

        if (delayBeforeFirstWave > 0f)
            yield return new WaitForSeconds(delayBeforeFirstWave);

        while (!IsGameOver)
        {
            CurrentWave++;
            yield return RunWave(CurrentWave);

            if (IsGameOver)
                yield break;

            if (normalsAfterEachWave > 0)
                yield return SpawnProtectedNormals(normalsAfterEachWave);
        }
    }

    private IEnumerator RunWave(int wave)
    {
        IsWaveActive = true;
        SecondsUntilNextWave = waveDuration;

        float interval = Mathf.Max(minSpawnInterval, startSpawnInterval - (wave - 1) * intervalDecreasePerWave);
        int maxThreats = Mathf.Min(hardMaxThreats, startMaxThreats + (wave - 1) * maxThreatIncreasePerWave);
        int burst = Mathf.Min(hardMaxSpawnBurst, startSpawnBurst + (wave - 1) / Mathf.Max(1, wavesPerBurstIncrease));
        float spawnCooldown = 0f;
        int panicsSpawnedThisWave = 0;

        while (SecondsUntilNextWave > 0f && !IsGameOver)
        {
            float dt = Time.deltaTime;
            SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - dt);
            spawnCooldown -= dt;

            Prune(lethals);
            Prune(minds);
            Prune(panics);

            bool canLethal = lethals.Count < maxThreats;
            bool canMind = wave >= mindUnlockWave && minds.Count < maxMindsOnScreen;
            bool canPanic = wave >= panicUnlockWave && panicsSpawnedThisWave < maxPanicsPerWave;

            if (spawnCooldown <= 0f && (canLethal || canMind || canPanic))
            {
                for (int i = 0; i < burst; i++)
                {
                    canLethal = lethals.Count < maxThreats;
                    canMind = wave >= mindUnlockWave && minds.Count < maxMindsOnScreen;
                    canPanic = wave >= panicUnlockWave && panicsSpawnedThisWave < maxPanicsPerWave;
                    if (!canLethal && !canMind && !canPanic)
                        break;

                    ThreatKind kind = PickThreatKind(wave, canLethal, canMind, canPanic);
                    GameObject chicken = Spawn(PrefabFor(kind));
                    if (chicken == null)
                        continue;

                    if (kind == ThreatKind.Mind)
                        minds.Add(chicken);
                    else if (kind == ThreatKind.Panic)
                    {
                        panics.Add(chicken);
                        protectedNormals.Add(chicken);
                        panicsSpawnedThisWave++;
                    }
                    else
                        lethals.Add(chicken);
                }

                spawnCooldown = interval;
            }

            yield return null;
        }

        IsWaveActive = false;
        SecondsUntilNextWave = 0f;
    }

    private IEnumerator SpawnProtectedNormals(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (IsGameOver)
                yield break;

            GameObject chicken = Spawn(Pick(normalChickenPrefabs));
            if (chicken != null)
                protectedNormals.Add(chicken);

            if (openingSpawnGap > 0f)
                yield return new WaitForSeconds(openingSpawnGap);
        }
    }

    private ThreatKind PickThreatKind(int wave, bool canLethal, bool canMind, bool canPanic)
    {
        float bombW = canLethal && Pick(bombChickenPrefabs) != null ? bombSpawnPercent : 0f;
        float mindW = canMind && Pick(mindChickenPrefabs) != null ? mindSpawnPercent : 0f;
        float electricW = canLethal && wave >= electricUnlockWave && Pick(electricChickenPrefabs) != null
            ? electricSpawnPercent
            : 0f;
        float panicW = canPanic && Pick(panicChickenPrefabs) != null ? panicSpawnPercent : 0f;

        float total = bombW + mindW + electricW + panicW;
        if (total <= 0f)
        {
            if (canLethal) return ThreatKind.Bomb;
            if (canPanic) return ThreatKind.Panic;
            return ThreatKind.Mind;
        }

        float roll = Random.Range(0f, total);
        if (roll < bombW) return ThreatKind.Bomb;
        roll -= bombW;
        if (roll < mindW) return ThreatKind.Mind;
        roll -= mindW;
        if (roll < electricW) return ThreatKind.Electric;
        return ThreatKind.Panic;
    }

    private GameObject PrefabFor(ThreatKind kind)
    {
        switch (kind)
        {
            case ThreatKind.Mind: return Pick(mindChickenPrefabs);
            case ThreatKind.Electric: return Pick(electricChickenPrefabs);
            case ThreatKind.Panic: return Pick(panicChickenPrefabs);
            default: return Pick(bombChickenPrefabs);
        }
    }

    private void OnDestroy()
    {
        if (IsGameOver)
            Time.timeScale = 1f;
    }

    private void EndGame()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        IsWaveActive = false;
        SecondsUntilNextWave = 0f;
        StopAllCoroutines();

        if (farmerTransform != null)
        {
            var move = farmerTransform.GetComponent<PlayerMovement>();
            if (move != null)
                move.enabled = false;

            var grab = farmerTransform.GetComponent<GrabCluck>();
            if (grab != null)
                grab.enabled = false;
        }

        Time.timeScale = 0f;
    }

    private IEnumerator HideIntroBannerAfterDelay(float seconds)
    {
        if (introBanner == null)
            yield break;

        yield return new WaitForSeconds(seconds);
        if (introBanner != null)
            introBanner.SetActive(false);
    }

    private GameObject Spawn(GameObject prefab)
    {
        if (prefab == null)
            return null;

        Vector2 pos = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        if (spawnEffect != null)
            StartCoroutine(PlaySpawnEffect(pos));

        GameObject chicken = Instantiate(prefab, pos, Quaternion.identity);

        ChickenWander wander = chicken.GetComponent<ChickenWander>();
        if (wander != null)
        {
            wander.SetWanderArea(spawnAreaMin, spawnAreaMax);
            wander.farmerTransform = farmerTransform;
        }

        return chicken;
    }

    private IEnumerator PlaySpawnEffect(Vector2 pos)
    {
        GameObject fx = Instantiate(spawnEffect, pos, Quaternion.identity);
        yield return new WaitForSeconds(0.7f);
        if (fx != null)
            Destroy(fx);
    }

    private static void Prune(List<GameObject> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null)
                list.RemoveAt(i);
        }
    }

    private static int CountAlive(List<GameObject> list)
    {
        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                n++;
        }
        return n;
    }

    private static GameObject Pick(GameObject[] prefabs)
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
}
