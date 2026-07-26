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
    [Tooltip("Scene loaded when the finish portal is entered.")]
    [SerializeField] private string portalTargetScene = "World2";
    [Tooltip("If true, spawn the finish portal at the center of spawnArea (World2 loop).")]
    [SerializeField] private bool portalAtCenter;

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
    [Tooltip("0 = spawn from wave start. 0.5 = halfway through the unlock wave.")]
    [SerializeField] [Range(0f, 1f)] private float mindUnlockWaveProgress = 0f;
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
    [SerializeField] private float bossBombFuseMin = 8f;
    [SerializeField] private float bossBombFuseMax = 8f;
    [SerializeField] private float bossWaveLaserCooldown = 3f;
    [SerializeField] private int normalsAfterEachWave = 2;

    [Header("Chaos Mode")]
    [Tooltip("If set, Chaos uses these bombs instead of bombChickenPrefabs (e.g. Farm bombs on Dusk).")]
    [SerializeField] private GameObject[] chaosBombChickenPrefabs;
    [Tooltip("If set, Chaos uses these electrics instead of electricChickenPrefabs.")]
    [SerializeField] private GameObject[] chaosElectricChickenPrefabs;
    [SerializeField] private float chaosElectricStartDelay = 15f;
    [SerializeField] private float chaosElectricTimerMin = 5f;
    [SerializeField] private float chaosElectricTimerMax = 7f;
    [SerializeField] private float chaosElectricSpawnInterval = 0.25f;
    [SerializeField] private int chaosElectricSpawnBurst = 3;
    [SerializeField] private int chaosMaxElectricsOnScreen = 18;

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

        PauseMenu.EnsureExists();
        CluckLivesUI.EnsureExists();
        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        // Only after Home → Play. Chaos / World2 / portal loop skip.
        bool showHtp = GameMode.PendingHowToPlay;
        GameMode.PendingHowToPlay = false;

        if (showHtp && !GameMode.IsChaos)
        {
            // Wait until the scene is loaded and held under a black fader.
            float timeout = 5f;
            while (!SceneFader.IsHoldingBlack && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            GameObject htp = HowToPlayIntro.FindInScene();
            if (htp != null)
                yield return HowToPlayIntro.Play(htp);
            else
                HowToPlayIntro.HideIfPresent();

            // Now reveal World 1 under the dismissed HTP.
            yield return SceneFader.RevealRoutine();
        }
        else
        {
            HowToPlayIntro.HideIfPresent();
            while (SceneFader.IsBusy)
                yield return null;
        }

        if (introBanner != null)
            introBanner.SetActive(true);

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
                var lostUi = FindAnyObjectByType<WaveTimerUI>();
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

        var ui = FindAnyObjectByType<WaveTimerUI>();
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
        float midX = (spawnAreaMin.x + spawnAreaMax.x) * 0.5f;
        float midY = (spawnAreaMin.y + spawnAreaMax.y) * 0.5f;
        Vector3 pos = portalAtCenter
            ? new Vector3(midX, midY, 0f)
            : new Vector3(spawnAreaMax.x, midY, 0f);

        // Respect Farm / Dusk / Combo selection from Home map pick.
        string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string target = GameMode.PortalTargetScene(current);
        if (string.IsNullOrEmpty(target))
            target = string.IsNullOrEmpty(portalTargetScene) ? "SampleScene" : portalTargetScene;

        GameObject portal = null;
        GameObject sceneGate = FindSceneGate();

        // Prefer a fresh prefab instance — scene Gate often has no collider / bad wiring.
        if (levelPortalPrefab != null)
        {
            if (sceneGate != null)
                sceneGate.SetActive(false);

            portal = Instantiate(levelPortalPrefab, pos, Quaternion.identity);
            portal.name = "Gate";
        }
        else
        {
            portal = sceneGate;
        }

        if (portal == null)
            return;

        portal.SetActive(true);
        portal.transform.position = pos;
        portal.name = "Gate";

        // Animator can fight portal setup — keep the sprite, drop the controller drive.
        Animator anim = portal.GetComponent<Animator>();
        if (anim != null)
            anim.enabled = false;

        SpriteRenderer sr = portal.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            sr.sortingOrder = Mathf.Max(sr.sortingOrder, 20);
        }

        LevelPortal levelPortal = portal.GetComponent<LevelPortal>();
        if (levelPortal == null)
            levelPortal = portal.AddComponent<LevelPortal>();
        levelPortal.Configure(target, 4f);
    }

    private static GameObject FindSceneGate()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t.name != "Gate")
                continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                continue;
            return t.gameObject;
        }

        return null;
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
        int mindsSpawnedThisWave = 0;
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
            bool canMind = CanSpawnMind(wave, mindsSpawnedThisWave);
            bool canPanic = wave >= panicUnlockWave && panicsSpawnedThisWave < maxPanicsPerWave;
            bool canRogue = wave >= rogueUnlockWave && roguesSpawnedThisWave < maxRoguesPerWave
                && (Pick(rogueChickenPrefabs) != null || Pick(bombChickenPrefabs) != null);

            if (spawnCooldown <= 0f && (canLethal || canMind || canPanic || canRogue))
            {
                for (int i = 0; i < burst; i++)
                {
                    canLethal = lethals.Count < maxThreats;
                    canMind = CanSpawnMind(wave, mindsSpawnedThisWave);
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
                    {
                        minds.Add(chicken);
                        mindsSpawnedThisWave++;
                    }
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
                var lostUi = FindAnyObjectByType<WaveTimerUI>();
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

        var ui = FindAnyObjectByType<WaveTimerUI>();
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

        var ui = FindAnyObjectByType<WaveTimerUI>();
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

    /// <summary>CHAOS mode: endless bomb rush from the right + gun; electrics after a delay.</summary>
    private IEnumerator RunChaosWave()
    {
        IsWaveActive = true;
        SecondsUntilNextWave = 999f;
        bossWaveNoFlock = true;

        yield return PrepareChaosWave();

        float spawnCooldown = 0f;
        float electricSpawnCooldown = 0f;
        float elapsed = 0f;

        while (!IsGameOver)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            spawnCooldown -= dt;
            electricSpawnCooldown -= dt;
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

            if (elapsed >= chaosElectricStartDelay
                && electricSpawnCooldown <= 0f
                && CountAliveChaosElectrics() < Mathf.Max(1, chaosMaxElectricsOnScreen))
            {
                int burst = Mathf.Max(1, chaosElectricSpawnBurst);
                int alive = CountAliveChaosElectrics();
                int room = Mathf.Max(0, chaosMaxElectricsOnScreen - alive);
                int toSpawn = Mathf.Min(burst, room);

                for (int i = 0; i < toSpawn; i++)
                    SpawnChaosElectric();

                electricSpawnCooldown = Mathf.Max(0.05f, chaosElectricSpawnInterval);
            }

            yield return null;
        }

        ClearLaserBossBuffs();
        ClearBossGun();
        RestoreFarmerBossMode();

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
        GameObject bombPrefab = PickChaosBombPrefab();
        if (bombPrefab == null)
            bombPrefab = Pick(bombChickenPrefabs);
        if (bombPrefab == null)
            return null;
        Bomb bomb = bombPrefab.GetComponent<Bomb>();
        return bomb != null ? bomb.explosion : null;
    }

    private GameObject PickChaosBombPrefab()
    {
        if (GameMode.IsChaos && chaosBombChickenPrefabs != null && chaosBombChickenPrefabs.Length > 0)
        {
            GameObject chaos = Pick(chaosBombChickenPrefabs);
            if (chaos != null)
                return chaos;
        }

        return Pick(bombChickenPrefabs);
    }

    private GameObject PickChaosElectricPrefab()
    {
        if (GameMode.IsChaos && chaosElectricChickenPrefabs != null && chaosElectricChickenPrefabs.Length > 0)
        {
            GameObject chaos = Pick(chaosElectricChickenPrefabs);
            if (chaos != null)
                return chaos;
        }

        return Pick(electricChickenPrefabs);
    }

    private GameObject SpawnBossBomb()
    {
        GameObject prefab = PickChaosBombPrefab();
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

    private GameObject SpawnChaosElectric()
    {
        GameObject prefab = PickChaosElectricPrefab();
        if (prefab == null)
            return null;

        Vector2 pos = new Vector2(
            spawnAreaMax.x,
            Random.Range(spawnAreaMin.y, spawnAreaMax.y));

        if (spawnEffect != null)
            StartCoroutine(PlaySpawnEffect(pos));

        GameObject electricGo = Instantiate(prefab, pos, Quaternion.identity);
        if (electricGo == null)
            return null;

        ChickenWander wander = electricGo.GetComponent<ChickenWander>();
        if (wander != null)
        {
            wander.SetWanderArea(spawnAreaMin, spawnAreaMax);
            wander.farmerTransform = farmerTransform;
            wander.SetBossMarchLeft(true);
        }

        ElectricChicken electric = electricGo.GetComponent<ElectricChicken>();
        if (electric != null)
        {
            electric.SetTimerRandom(chaosElectricTimerMin, chaosElectricTimerMax);
            electric.SetDestroyOnStrike(true);
        }

        lethals.Add(electricGo);
        return electricGo;
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

    private int CountAliveChaosElectrics()
    {
        int n = 0;
        for (int i = 0; i < lethals.Count; i++)
        {
            GameObject go = lethals[i];
            if (go == null)
                continue;
            if (go.GetComponent<ElectricChicken>() != null)
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

    private bool CanSpawnMind(int wave, int mindsSpawnedThisWave)
    {
        if (Pick(mindChickenPrefabs) == null)
            return false;
        // Cap is per wave (not concurrent) so a dead alien does not immediately respawn.
        if (mindsSpawnedThisWave >= Mathf.Max(1, maxMindsOnScreen))
            return false;
        if (wave < mindUnlockWave)
            return false;
        if (wave > mindUnlockWave)
            return true;

        float duration = Mathf.Max(0.01f, GetWaveDuration(wave));
        float elapsed = duration - SecondsUntilNextWave;
        return elapsed >= duration * Mathf.Clamp01(mindUnlockWaveProgress);
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
