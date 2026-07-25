using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Starts with protected normals, then story/chaos waves.
/// Lethals (bombs / electrics / lasers / rogues) share a threat cap.
/// Minds are non-lethal with their own cap.
/// Panics are flock chickens: they count toward game over when killed.
/// Rogues are bombs that sprint like panics (max per wave, unlock wave).
/// Lasers: from unlock wave onward, spawn on a fixed interval.
/// Spawn chance uses percentages among unlocked types.
/// </summary>
public class ChickenSpawner : MonoBehaviour
{
    private enum ThreatKind
    {
        Bomb,
        Mind,
        Electric,
        Panic,
        Rogue
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject[] normalChickenPrefabs;
    [SerializeField] private GameObject[] bombChickenPrefabs;
    [SerializeField] private GameObject[] mindChickenPrefabs;
    [SerializeField] private GameObject[] electricChickenPrefabs;
    [SerializeField] private GameObject[] panicChickenPrefabs;
    [SerializeField] private GameObject[] rogueChickenPrefabs;
    [SerializeField] private GameObject[] laserChickenPrefabs;
    [SerializeField] private GameObject[] bossChickenPrefabs;
    [SerializeField] private GameObject bossGunPrefab;
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private GameObject livesPrefab;
    [SerializeField] private GameObject spawnEffect;
    [SerializeField] private GameObject levelPortalPrefab;
    [Tooltip("If true, places the farmer at the center of spawnArea on Start (World2).")]
    [SerializeField] private bool centerFarmerOnStart;
    [Tooltip("After final story wave: explode mobs, show finished text, open portal.")]
    [SerializeField] private bool spawnPortalOnFinish = true;

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
    [SerializeField] private float wave4Duration = 20f;
    [SerializeField] private int mindUnlockWave = 2;
    [SerializeField] private int electricUnlockWave = 3;
    [Tooltip("Last wave electrics/fire can spawn (0 = no upper limit).")]
    [SerializeField] private int electricMaxWave = 0;
    [SerializeField] private int panicUnlockWave = 1;
    [SerializeField] private int rogueUnlockWave = 2;
    [SerializeField] private int laserUnlockWave = 4;
    [SerializeField] private float laserSpawnInterval = 3f;
    [SerializeField] private int bossUnlockWave = 6;
    [Tooltip("Testing: start story at this wave (0 = normal from wave 1).")]
    [SerializeField] private int startAtWaveForTesting = 5;
    [SerializeField] private bool startAtBossWaveForTesting = false;
    [SerializeField] private int maxStoryWave = 5;
    [SerializeField] private int storyBossNormalCount = 8;
    [SerializeField] private float storyBossLaserTimer = 5f;
    [SerializeField] private int bossWaveNormalCount = 12;
    [SerializeField] private int bossWaveBossCount = 60; // unused total; kept for inspector compat
    [SerializeField] private float bossWaveDuration = 20f;
    [SerializeField] private float bossSpawnInterval = 0.25f;
    [SerializeField] private int bossSpawnBurst = 2;
    [SerializeField] private int bossMaxBombsOnScreen = 18;
    [SerializeField] private float bossBombFuseMin = 10f;
    [SerializeField] private float bossBombFuseMax = 10f;
    [SerializeField] private float bossWaveLaserCooldown = 3f;
    [SerializeField] private int normalsAfterEachWave = 2;

    [Header("Spawn Chances %")]
    [SerializeField] [Range(0f, 100f)] private float bombSpawnPercent = 60f;
    [SerializeField] [Range(0f, 100f)] private float mindSpawnPercent = 15f;
    [SerializeField] [Range(0f, 100f)] private float electricSpawnPercent = 15f;
    [SerializeField] [Range(0f, 100f)] private float panicSpawnPercent = 15f;
    [SerializeField] [Range(0f, 100f)] private float rogueSpawnPercent = 20f;

    [Header("Difficulty")]
    [SerializeField] private float startSpawnInterval = 4f;
    [SerializeField] private float minSpawnInterval = 1.2f;
    [SerializeField] private float intervalDecreasePerWave = 0.3f;
    [SerializeField] private int startMaxThreats = 4;
    [SerializeField] private int maxThreatIncreasePerWave = 1;
    [SerializeField] private int hardMaxThreats = 18;
    [SerializeField] private int maxMindsOnScreen = 2;
    [SerializeField] private int maxPanicsPerWave = 2;
    [SerializeField] private int maxRoguesPerWave = 2;
    [SerializeField] private int startSpawnBurst = 1;
    [SerializeField] private int wavesPerBurstIncrease = 3;
    [SerializeField] private int hardMaxSpawnBurst = 4;

    private readonly List<GameObject> protectedNormals = new List<GameObject>();
    private readonly List<GameObject> lethals = new List<GameObject>();
    private readonly List<GameObject> minds = new List<GameObject>();
    private readonly List<GameObject> panics = new List<GameObject>();
    private bool bossWaveNoFlock;
    private GameObject bossGunInstance;
    private GameObject storyBossLaser;
    private bool storyBossProtectLaser;

    public int CurrentWave { get; private set; }
    public float SecondsUntilNextWave { get; private set; }
    public bool IsWaveActive { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsFinished { get; private set; }
    public int ProtectedAlive => CountAlive(protectedNormals);
    public float MapWidth => Mathf.Abs(spawnAreaMax.x - spawnAreaMin.x);

    public bool IsWaitingForNextWave => IsWaveActive && !IsGameOver && !IsFinished;
    public bool HasStarted { get; private set; }

    private void Start()
    {
        if (farmerTransform == null)
        {
            GameObject farmer = GameObject.Find("Farmer");
            if (farmer != null)
                farmerTransform = farmer.transform;
        }

        if (centerFarmerOnStart)
            PlaceFarmerAtAreaCenter();

        StartGame();
    }

    private void PlaceFarmerAtAreaCenter()
    {
        if (farmerTransform == null)
            return;

        Vector3 pos = farmerTransform.position;
        pos.x = (spawnAreaMin.x + spawnAreaMax.x) * 0.5f;
        pos.y = (spawnAreaMin.y + spawnAreaMax.y) * 0.5f;
        farmerTransform.position = pos;
    }

    /// <summary>
    /// Begins spawning / waves. Safe to call once.
    /// </summary>
    public void StartGame()
    {
        if (HasStarted || IsGameOver || IsFinished)
            return;

        HasStarted = true;
        StartCoroutine(RunGame());
    }

    private void Update()
    {
        if (IsGameOver || IsFinished)
            return;

        // Story boss: losing the held laser chicken ends the run.
        if (storyBossProtectLaser)
        {
            if (storyBossLaser == null)
            {
                var lostUi = FindFirstObjectByType<WaveTimerUI>();
                if (lostUi != null)
                    lostUi.SetLaserLostGameOver();
                EndGame();
                return;
            }
        }

        if (bossWaveNoFlock)
            return;

        if (protectedNormals.Count == 0)
            return;

        Prune(protectedNormals);
        if (ProtectedAlive == 0)
            EndGame();
    }

    private IEnumerator RunGame()
    {
        StartCoroutine(HideIntroBannerAfterDelay(2f));

        // CHAOS mode: separate home-button mode (bomb rush + gun).
        if (GameMode.IsChaos)
        {
            CurrentWave = 1;
            yield return RunChaosWave();
            yield break;
        }

        yield return SpawnProtectedNormals(startingNormals);

        if (delayBeforeFirstWave > 0f)
            yield return new WaitForSeconds(delayBeforeFirstWave);

        // Testing shortcut: jump into a later wave (CurrentWave++ happens in the loop).
        if (startAtWaveForTesting > 0)
            CurrentWave = Mathf.Clamp(startAtWaveForTesting, 1, maxStoryWave) - 1;

        while (!IsGameOver && !IsFinished)
        {
            CurrentWave++;
            if (CurrentWave > maxStoryWave)
                break;

            yield return RunWave(CurrentWave);

            if (IsGameOver)
                yield break;

            if (CurrentWave >= maxStoryWave)
            {
                yield return FinishLevel1();
                yield break;
            }

            if (normalsAfterEachWave > 0)
                yield return SpawnProtectedNormals(normalsAfterEachWave);
        }
    }

    /// <summary>Wave 5 clear: wipe remaining mobs, banner, open portal to World2.</summary>
    private IEnumerator FinishLevel1()
    {
        IsFinished = true;
        IsWaveActive = false;
        SecondsUntilNextWave = 0f;

        yield return ExplodeAllRemainingMobs();

        var ui = FindFirstObjectByType<WaveTimerUI>();
        if (ui != null)
            ui.ShowFinished();

        if (spawnPortalOnFinish)
            OpenLevelPortal();
    }

    private IEnumerator ExplodeAllRemainingMobs()
    {
        var toExplode = new List<GameObject>();

        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander c = chickens[i];
            if (c == null)
                continue;
            toExplode.Add(c.gameObject);
        }

        for (int i = 0; i < toExplode.Count; i++)
        {
            ExplodeChicken(toExplode[i]);
            if (openingSpawnGap > 0f)
                yield return new WaitForSeconds(Mathf.Min(0.06f, openingSpawnGap));
        }

        protectedNormals.Clear();
        panics.Clear();
        minds.Clear();
        lethals.Clear();
    }

    private void OpenLevelPortal()
    {
        Vector3 pos = new Vector3(
            spawnAreaMax.x,
            (spawnAreaMin.y + spawnAreaMax.y) * 0.5f,
            0f);

        GameObject portal = null;
        if (levelPortalPrefab != null)
        {
            portal = Instantiate(levelPortalPrefab, pos, Quaternion.identity);
            portal.name = "Gate";
        }
        else
        {
            GameObject existing = GameObject.Find("Gate");
            if (existing != null)
            {
                portal = existing;
                portal.transform.position = pos;
                portal.SetActive(true);
            }
        }

        if (portal == null)
            return;

        if (portal.GetComponent<LevelPortal>() == null)
            portal.AddComponent<LevelPortal>();

        Collider2D col = portal.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = portal.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 1.8f);
            col = box;
        }
        else
        {
            col.isTrigger = true;
        }

        Animator anim = portal.GetComponent<Animator>();
        if (anim != null)
            anim.Play(0, 0, 0f);
    }

    private IEnumerator RunWave(int wave)
    {
        IsWaveActive = true;
        SecondsUntilNextWave = GetWaveDuration(wave);

        float interval = Mathf.Max(minSpawnInterval, startSpawnInterval - (wave - 1) * intervalDecreasePerWave);
        int maxThreats = Mathf.Min(hardMaxThreats, startMaxThreats + (wave - 1) * maxThreatIncreasePerWave);
        int burst = Mathf.Min(hardMaxSpawnBurst, startSpawnBurst + (wave - 1) / Mathf.Max(1, wavesPerBurstIncrease));
        float spawnCooldown = 0f;
        float laserCooldown = 0f;
        int panicsSpawnedThisWave = 0;
        int roguesSpawnedThisWave = 0;
        bool lasersUnlocked = wave >= laserUnlockWave && Pick(laserChickenPrefabs) != null;

        while (SecondsUntilNextWave > 0f && !IsGameOver)
        {
            float dt = Time.deltaTime;
            SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - dt);
            spawnCooldown -= dt;

            Prune(lethals);
            Prune(minds);
            Prune(panics);

            if (lasersUnlocked)
            {
                laserCooldown -= dt;
                if (laserCooldown <= 0f)
                {
                    GameObject laser = Spawn(Pick(laserChickenPrefabs));
                    if (laser != null)
                        lethals.Add(laser);
                    laserCooldown = laserSpawnInterval;
                }
            }

            bool canLethal = lethals.Count < maxThreats;
            bool canMind = wave >= mindUnlockWave && minds.Count < maxMindsOnScreen;
            bool canPanic = wave >= panicUnlockWave && panicsSpawnedThisWave < maxPanicsPerWave;
            bool canRogue = wave >= rogueUnlockWave && roguesSpawnedThisWave < maxRoguesPerWave
                && (Pick(rogueChickenPrefabs) != null || Pick(bombChickenPrefabs) != null);

            if (spawnCooldown <= 0f && (canLethal || canMind || canPanic || canRogue))
            {
                for (int i = 0; i < burst; i++)
                {
                    canLethal = lethals.Count < maxThreats;
                    canMind = wave >= mindUnlockWave && minds.Count < maxMindsOnScreen;
                    canPanic = wave >= panicUnlockWave && panicsSpawnedThisWave < maxPanicsPerWave;
                    canRogue = wave >= rogueUnlockWave && roguesSpawnedThisWave < maxRoguesPerWave
                        && (Pick(rogueChickenPrefabs) != null || Pick(bombChickenPrefabs) != null);
                    if (!canLethal && !canMind && !canPanic && !canRogue)
                        break;

                    ThreatKind kind = PickThreatKind(wave, canLethal, canMind, canPanic, canRogue);
                    GameObject chicken = Spawn(PrefabFor(kind));
                    if (chicken == null)
                        continue;

                    if (kind == ThreatKind.Rogue)
                        EnsureRogue(chicken);

                    if (kind == ThreatKind.Mind)
                        minds.Add(chicken);
                    else if (kind == ThreatKind.Panic)
                    {
                        panics.Add(chicken);
                        protectedNormals.Add(chicken);
                        panicsSpawnedThisWave++;
                    }
                    else if (kind == ThreatKind.Rogue)
                    {
                        lethals.Add(chicken);
                        roguesSpawnedThisWave++;
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

    /// <summary>Story wave 6: wipe field, 5 normals + 1 laser, single boss with missile salvos.</summary>
    private IEnumerator RunStoryBossWave()
    {
        IsWaveActive = true;
        SecondsUntilNextWave = 999f;
        bossWaveNoFlock = true;
        storyBossProtectLaser = false;
        storyBossLaser = null;

        yield return PrepareStoryBossWave();

        // Only arm laser-death game-over after the held laser actually exists.
        if (storyBossLaser == null)
        {
            bossWaveNoFlock = false;
            IsWaveActive = false;
            SecondsUntilNextWave = 0f;
            yield break;
        }

        storyBossProtectLaser = true;

        GameObject bossGo = SpawnStoryBoss();
        if (bossGo == null)
        {
            storyBossProtectLaser = false;
            storyBossLaser = null;
            bossWaveNoFlock = false;
            IsWaveActive = false;
            SecondsUntilNextWave = 0f;
            yield break;
        }

        BossChicken boss = bossGo.GetComponent<BossChicken>();
        while (!IsGameOver && boss != null && !boss.IsDead)
        {
            if (storyBossLaser == null)
            {
                var lostUi = FindFirstObjectByType<WaveTimerUI>();
                if (lostUi != null)
                    lostUi.SetLaserLostGameOver();
                EndGame();
                break;
            }

            Prune(protectedNormals);
            Prune(lethals);
            yield return null;
        }

        for (int i = BossMissile.ActiveMissiles.Count - 1; i >= 0; i--)
        {
            BossMissile m = BossMissile.ActiveMissiles[i];
            if (m != null)
                m.Explode();
        }

        var ui = FindFirstObjectByType<WaveTimerUI>();
        if (ui != null)
            ui.ClearHint();

        ClearLaserBossBuffs();

        if (farmerTransform != null)
        {
            var grab = farmerTransform.GetComponent<GrabCluck>();
            if (grab != null)
                grab.ClearBossLaserLock();
        }

        storyBossProtectLaser = false;
        storyBossLaser = null;
        bossWaveNoFlock = false;
        IsWaveActive = false;
        SecondsUntilNextWave = 0f;
    }

    private IEnumerator PrepareStoryBossWave()
    {
        Prune(protectedNormals);
        Prune(lethals);
        Prune(minds);
        Prune(panics);

        yield return ExplodeEverything();

        GameObject laserGo = Spawn(Pick(laserChickenPrefabs));
        if (laserGo != null)
        {
            storyBossLaser = laserGo;
            lethals.Add(laserGo);
            LaserChicken laser = laserGo.GetComponent<LaserChicken>();
            if (laser != null)
                laser.ConfigureHeldBossLaser(cooldownSeconds: 2f, beamDurationSeconds: 0.9f);

            if (farmerTransform != null)
            {
                var grab = farmerTransform.GetComponent<GrabCluck>();
                if (grab != null)
                    grab.ForceGrab(laserGo.transform, lockAsManualLaser: true);
            }
        }

        var ui = FindFirstObjectByType<WaveTimerUI>();
        if (ui != null)
            ui.ShowHint("PROTECT THE LASER CHICKEN", durationSeconds: -1f);

        yield return null;
    }

    private IEnumerator ExplodeEverything()
    {
        var toExplode = new List<GameObject>();
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        for (int i = 0; i < chickens.Length; i++)
        {
            if (chickens[i] == null)
                continue;
            if (chickens[i].GetComponent<BossChicken>() != null)
                continue;
            toExplode.Add(chickens[i].gameObject);
        }

        for (int i = 0; i < toExplode.Count; i++)
        {
            ExplodeChicken(toExplode[i]);
            if (openingSpawnGap > 0f)
                yield return new WaitForSeconds(Mathf.Min(0.08f, openingSpawnGap));
        }

        protectedNormals.Clear();
        panics.Clear();
        minds.Clear();
        lethals.Clear();
    }

    private GameObject SpawnStoryBoss()
    {
        GameObject prefab = Pick(bossChickenPrefabs);
        if (prefab == null)
            return null;

        Vector2 pos = new Vector2(
            (spawnAreaMin.x + spawnAreaMax.x) * 0.5f,
            (spawnAreaMin.y + spawnAreaMax.y) * 0.5f);

        if (spawnEffect != null)
            StartCoroutine(PlaySpawnEffect(pos));

        GameObject bossGo = Instantiate(prefab, pos, Quaternion.identity);
        BossChicken boss = bossGo.GetComponent<BossChicken>();
        if (boss == null)
            boss = bossGo.AddComponent<BossChicken>();

        boss.ConfigurePrefabs(missilePrefab, livesPrefab);
        boss.ConfigureSideLaserPrefab(Pick(laserChickenPrefabs));
        GameObject boom = FindExplosionPrefab();
        if (boom != null)
            boss.ConfigureExplosion(boom);

        ChickenWander wander = bossGo.GetComponent<ChickenWander>();
        if (wander != null)
            wander.enabled = false;

        boss.Begin(spawnAreaMin, spawnAreaMax, runOpening: false);
        lethals.Add(bossGo);
        return bossGo;
    }

    /// <summary>CHAOS mode: short-fuse bombs from the right + gun in farmer hands.</summary>
    private IEnumerator RunChaosWave()
    {
        IsWaveActive = true;
        SecondsUntilNextWave = bossWaveDuration;
        bossWaveNoFlock = true;

        yield return PrepareChaosWave();

        float spawnCooldown = 0f;

        while (SecondsUntilNextWave > 0f && !IsGameOver)
        {
            float dt = Time.deltaTime;
            SecondsUntilNextWave = Mathf.Max(0f, SecondsUntilNextWave - dt);
            spawnCooldown -= dt;
            Prune(lethals);

            if (spawnCooldown <= 0f && CountAliveBossBombs() < Mathf.Max(1, bossMaxBombsOnScreen))
            {
                int burst = Mathf.Max(1, bossSpawnBurst);
                int alive = CountAliveBossBombs();
                int room = Mathf.Max(0, bossMaxBombsOnScreen - alive);
                int toSpawn = Mathf.Min(burst, room);

                for (int i = 0; i < toSpawn; i++)
                    SpawnBossBomb();

                spawnCooldown = Mathf.Max(0.05f, bossSpawnInterval);
            }

            yield return null;
        }

        while (!IsGameOver && CountAliveBossBombs() > 0)
        {
            Prune(lethals);
            yield return null;
        }

        ClearLaserBossBuffs();
        ClearBossGun();
        RestoreFarmerBossMode();

        int refill = Mathf.Max(normalsAfterEachWave, 3);
        if (refill > 0)
            yield return SpawnProtectedNormals(refill);

        bossWaveNoFlock = false;
        IsWaveActive = false;
        SecondsUntilNextWave = 0f;
    }

    private IEnumerator PrepareChaosWave()
    {
        Prune(protectedNormals);
        Prune(lethals);
        Prune(minds);
        Prune(panics);

        yield return ExplodeAllExceptLaser();

        EnterFarmerBossLane();
        EnsureBossGunInHands();

        ChickenWander.SetBossLeftHuddleForFlock(false);
        yield return null;
    }

    private IEnumerator ExplodeAllExceptLaser()
    {
        var toExplode = new List<GameObject>();

        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander c = chickens[i];
            if (c == null)
                continue;

            LaserChicken laser = c.GetComponent<LaserChicken>();
            if (laser != null)
                continue;

            toExplode.Add(c.gameObject);
        }

        for (int i = 0; i < toExplode.Count; i++)
        {
            ExplodeChicken(toExplode[i]);
            if (openingSpawnGap > 0f)
                yield return new WaitForSeconds(Mathf.Min(0.08f, openingSpawnGap));
        }

        protectedNormals.Clear();
        panics.Clear();
        minds.Clear();

        // Drop non-laser lethals from tracking (destroyed above).
        for (int i = lethals.Count - 1; i >= 0; i--)
        {
            GameObject go = lethals[i];
            if (go == null || go.GetComponent<LaserChicken>() == null)
                lethals.RemoveAt(i);
        }
    }

    private void ExplodeChicken(GameObject go)
    {
        if (go == null)
            return;

        Bomb bomb = go.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.Detonate();
            return;
        }

        Vector3 pos = go.transform.position;
        GameObject fxPrefab = FindExplosionPrefab();
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, pos, Quaternion.identity);
            Destroy(fx, 0.7f);
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        Destroy(go);
    }

    private GameObject FindExplosionPrefab()
    {
        GameObject bombPrefab = Pick(bombChickenPrefabs);
        if (bombPrefab == null)
            return null;
        Bomb bomb = bombPrefab.GetComponent<Bomb>();
        return bomb != null ? bomb.explosion : null;
    }

    private GameObject SpawnBossBomb()
    {
        GameObject prefab = Pick(bombChickenPrefabs);
        if (prefab == null)
            return null;

        // Spawn on the far right edge, random height.
        Vector2 pos = new Vector2(
            spawnAreaMax.x,
            Random.Range(spawnAreaMin.y, spawnAreaMax.y));

        if (spawnEffect != null)
            StartCoroutine(PlaySpawnEffect(pos));

        GameObject bombGo = Instantiate(prefab, pos, Quaternion.identity);
        if (bombGo == null)
            return null;

        ChickenWander wander = bombGo.GetComponent<ChickenWander>();
        if (wander != null)
        {
            wander.SetWanderArea(spawnAreaMin, spawnAreaMax);
            wander.farmerTransform = farmerTransform;
            wander.SetBossMarchLeft(true);
        }

        Bomb bomb = bombGo.GetComponent<Bomb>();
        if (bomb != null)
            bomb.SetFuseRandom(bossBombFuseMin, bossBombFuseMax);

        lethals.Add(bombGo);
        return bombGo;
    }

    private int CountAliveBossBombs()
    {
        int n = 0;
        for (int i = 0; i < lethals.Count; i++)
        {
            GameObject go = lethals[i];
            if (go == null)
                continue;
            if (go.GetComponent<Bomb>() != null)
                n++;
        }
        return n;
    }

    private void EnterFarmerBossLane()
    {
        if (farmerTransform == null)
            return;

        var move = farmerTransform.GetComponent<PlayerMovement>();
        if (move == null)
            return;

        move.SetSpeedMultiplier(1.45f);
        move.SetBossLaneMode(true, spawnAreaMin.x, spawnAreaMin.y, spawnAreaMax.y);
    }

    private void RestoreFarmerBossMode()
    {
        if (farmerTransform == null)
            return;

        var move = farmerTransform.GetComponent<PlayerMovement>();
        if (move != null)
        {
            move.SetSpeedMultiplier(1f);
            move.SetBossLaneMode(false, 0f, 0f, 0f);
        }

        var grab = farmerTransform.GetComponent<GrabCluck>();
        if (grab != null)
            grab.ClearBossLaserLock();
    }

    private void BoostFarmerSpeed()
    {
        EnterFarmerBossLane();
    }

    private void RestoreFarmerSpeed()
    {
        RestoreFarmerBossMode();
    }

    private void EnsureBossGunInHands()
    {
        // Remove any wave lasers — boss uses the gun instead.
        LaserChicken[] lasers = FindObjectsByType<LaserChicken>();
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] == null)
                continue;
            Destroy(lasers[i].gameObject);
        }

        for (int i = lethals.Count - 1; i >= 0; i--)
        {
            GameObject go = lethals[i];
            if (go == null || go.GetComponent<LaserChicken>() != null)
                lethals.RemoveAt(i);
        }

        ClearBossGun();

        GameObject gunPrefab = bossGunPrefab;
        if (gunPrefab == null)
        {
            // Fallback: old laser-chicken-in-hands behaviour.
            EnsureBossLaserInHands();
            return;
        }

        GameObject gunGo = Instantiate(gunPrefab);
        gunGo.name = "BossGun";
        bossGunInstance = gunGo;

        LaserChicken weapon = gunGo.GetComponent<LaserChicken>();
        if (weapon == null)
            weapon = gunGo.AddComponent<LaserChicken>();

        GameObject beamPrefab = ResolveLaserBeamPrefab();
        weapon.ConfigureBossGun(beamPrefab, burstRate: 0.07f);

        lethals.Add(gunGo);

        if (farmerTransform != null)
        {
            var grab = farmerTransform.GetComponent<GrabCluck>();
            if (grab != null)
                grab.ForceGrab(gunGo.transform, lockAsManualLaser: true, holdLocalOverride: new Vector3(0.55f, 0.05f, 0f));
        }
    }

    private GameObject ResolveLaserBeamPrefab()
    {
        GameObject laserChicken = Pick(laserChickenPrefabs);
        if (laserChicken == null)
            return null;

        LaserChicken sample = laserChicken.GetComponent<LaserChicken>();
        return sample != null ? sample.laserPrefab : null;
    }

    private void ClearBossGun()
    {
        if (bossGunInstance != null)
        {
            Destroy(bossGunInstance);
            bossGunInstance = null;
        }
    }

    private void EnsureBossLaserInHands()
    {
        LaserChicken kept = null;

        LaserChicken[] lasers = FindObjectsByType<LaserChicken>();
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] == null)
                continue;

            if (kept == null)
            {
                kept = lasers[i];
                continue;
            }

            Destroy(lasers[i].gameObject);
        }

        if (kept == null)
        {
            GameObject laserGo = Spawn(Pick(laserChickenPrefabs));
            if (laserGo != null)
            {
                lethals.Add(laserGo);
                kept = laserGo.GetComponent<LaserChicken>();
            }
        }
        else if (!lethals.Contains(kept.gameObject))
        {
            lethals.Add(kept.gameObject);
        }

        if (kept == null)
            return;

        kept.SetImmune(true);
        kept.SetManualFire(true);

        if (farmerTransform != null)
        {
            var grab = farmerTransform.GetComponent<GrabCluck>();
            if (grab != null)
                grab.ForceGrab(kept.transform, lockAsManualLaser: true);
        }
    }

    private void ClearLaserBossBuffs()
    {
        LaserChicken[] lasers = FindObjectsByType<LaserChicken>();
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] == null)
                continue;
            lasers[i].SetImmune(false);
            lasers[i].SetManualFire(false);
            lasers[i].SetProtectFlock(false);
            lasers[i].SetCooldown(5f, 10f);
        }
    }

    private float GetWaveDuration(int wave)
    {
        if (wave == 4)
            return wave4Duration;
        return waveDuration;
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

    private ThreatKind PickThreatKind(int wave, bool canLethal, bool canMind, bool canPanic, bool canRogue)
    {
        float bombW = canLethal && Pick(bombChickenPrefabs) != null ? bombSpawnPercent : 0f;
        float mindW = canMind && Pick(mindChickenPrefabs) != null ? mindSpawnPercent : 0f;
        float electricW = canLethal && IsElectricWave(wave) && Pick(electricChickenPrefabs) != null
            ? electricSpawnPercent
            : 0f;
        float panicW = canPanic && Pick(panicChickenPrefabs) != null ? panicSpawnPercent : 0f;
        float rogueW = canRogue ? rogueSpawnPercent : 0f;

        float total = bombW + mindW + electricW + panicW + rogueW;
        if (total <= 0f)
        {
            if (canRogue) return ThreatKind.Rogue;
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
        roll -= electricW;
        if (roll < panicW) return ThreatKind.Panic;
        return ThreatKind.Rogue;
    }

    private bool IsElectricWave(int wave)
    {
        if (wave < electricUnlockWave)
            return false;
        if (electricMaxWave > 0 && wave > electricMaxWave)
            return false;
        return true;
    }

    private GameObject PrefabFor(ThreatKind kind)
    {
        switch (kind)
        {
            case ThreatKind.Mind: return Pick(mindChickenPrefabs);
            case ThreatKind.Electric: return Pick(electricChickenPrefabs);
            case ThreatKind.Panic: return Pick(panicChickenPrefabs);
            case ThreatKind.Rogue:
            {
                GameObject rogue = Pick(rogueChickenPrefabs);
                return rogue != null ? rogue : Pick(bombChickenPrefabs);
            }
            default: return Pick(bombChickenPrefabs);
        }
    }

    private static void EnsureRogue(GameObject chicken)
    {
        if (chicken == null)
            return;

        if (chicken.GetComponent<RogueChicken>() == null)
            chicken.AddComponent<RogueChicken>();

        ChickenWander wander = chicken.GetComponent<ChickenWander>();
        if (wander != null)
            wander.RefreshTypeFlags();
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
