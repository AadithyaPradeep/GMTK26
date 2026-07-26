using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Story wave-6 boss: missile salvos + occasional dual side-laser special.
/// </summary>
public class BossChicken : MonoBehaviour
{
    private static readonly List<BossChicken> Active = new List<BossChicken>();

    [Header("Prefabs")]
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private GameObject livesPrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject sideLaserPrefab; // LaserCluck — we only steal its beam prefab
    [SerializeField] private GameObject sideBeamPrefab;  // optional direct Laser beam prefab

    [Header("Combat")]
    [SerializeField] private int maxLives = 10;
    [SerializeField] private float salvoInterval = 3f;
    [SerializeField] private int missilesPerSalvo = 7;
    [SerializeField] private float salvoStagger = 0.05f;
    [SerializeField] private float missileSpeed = 2.6f;
    [SerializeField] private float missileLifetime = 5.5f;
    [SerializeField] private float missileTurnRate = 150f;
    [SerializeField] private float aimSpreadDegrees = 100f;
    [SerializeField] private float specialChance = 0.35f;
    [SerializeField] private float specialLaserDuration = 5f;
    [SerializeField] private Vector3 livesLocalOffset = new Vector3(0f, 1.35f, 0f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float arrivalThreshold = 0.4f;
    [SerializeField] private float minTargetDistance = 4f;
    [SerializeField] private Vector2 wanderAreaMin = new Vector2(-12f, -4.8f);
    [SerializeField] private Vector2 wanderAreaMax = new Vector2(6f, 4.1f);

    private int lives;
    private bool dead;
    private bool combatActive;
    private BossLivesDisplay livesDisplay;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 areaMin;
    private Vector2 areaMax;
    private Vector2 moveTarget;
    private Transform farmerTarget;
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    public static IReadOnlyList<BossChicken> ActiveBosses => Active;
    public static int AliveCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Active.Count; i++)
            {
                if (Active[i] != null && !Active[i].dead)
                    n++;
            }
            return n;
        }
    }

    public static BossChicken Instance
    {
        get
        {
            for (int i = 0; i < Active.Count; i++)
            {
                if (Active[i] != null && !Active[i].dead)
                    return Active[i];
            }
            return null;
        }
    }

    public bool IsDead => dead;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        lives = maxLives;
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }

    public void Begin(Vector2 spawnAreaMin, Vector2 spawnAreaMax, bool runOpening = false)
    {
        areaMin = wanderAreaMin;
        areaMax = wanderAreaMax;
        if (areaMax.x <= areaMin.x || areaMax.y <= areaMin.y)
        {
            areaMin = spawnAreaMin;
            areaMax = spawnAreaMax;
        }

        GameObject farmer = GameObject.Find("Farmer");
        if (farmer != null)
            farmerTarget = farmer.transform;

        lives = maxLives;
        SpawnLivesUi();
        PickNewTarget(force: true);
        combatActive = true;
        StartCoroutine(SalvoRoutine());
    }

    public bool TakeDamage(int amount = 1)
    {
        if (dead || amount <= 0)
            return false;

        lives = Mathf.Max(0, lives - amount);
        if (livesDisplay != null)
            livesDisplay.SetRemaining(lives);

        if (lives <= 0)
        {
            Die();
            return true;
        }

        PickNewTarget(force: true);
        return true;
    }

    public void ConfigurePrefabs(GameObject missile, GameObject lives)
    {
        if (missile != null)
            missilePrefab = missile;
        if (lives != null)
            livesPrefab = lives;
    }

    public void ConfigureSideLaserPrefab(GameObject laserChickenPrefab)
    {
        if (laserChickenPrefab != null)
            sideLaserPrefab = laserChickenPrefab;

        // Prefer the beam child prefab so specials spawn lasers only (no chicken).
        if (sideLaserPrefab != null)
        {
            LaserChicken sample = sideLaserPrefab.GetComponent<LaserChicken>();
            if (sample != null && sample.laserPrefab != null)
                sideBeamPrefab = sample.laserPrefab;
        }
    }

    public void ConfigureExplosion(GameObject explosion)
    {
        if (explosion != null)
            explosionPrefab = explosion;
    }

    private void SpawnLivesUi()
    {
        if (livesPrefab == null)
            return;

        GameObject livesGo = Instantiate(livesPrefab, transform);
        livesGo.transform.localPosition = livesLocalOffset;
        livesGo.transform.localRotation = Quaternion.identity;
        livesGo.transform.localScale = Vector3.one;

        livesDisplay = livesGo.GetComponent<BossLivesDisplay>();
        if (livesDisplay == null)
            livesDisplay = livesGo.AddComponent<BossLivesDisplay>();

        livesDisplay.Initialize(maxLives);
        livesDisplay.SetRemaining(lives);
    }

    private IEnumerator SalvoRoutine()
    {
        yield return new WaitForSeconds(1f);

        int cycle = 0;
        while (!dead)
        {
            cycle++;
            bool doSpecial = cycle > 1 && (cycle % 3 == 0 || Random.value < specialChance);

            if (doSpecial)
                yield return SpecialSideLasers();
            else
                yield return FireSalvo();

            if (dead)
                yield break;

            yield return new WaitForSeconds(salvoInterval);
        }
    }

    private IEnumerator SpecialSideLasers()
    {
        GameObject beamPrefab = sideBeamPrefab;
        if (beamPrefab == null && sideLaserPrefab != null)
        {
            LaserChicken sample = sideLaserPrefab.GetComponent<LaserChicken>();
            if (sample != null)
                beamPrefab = sample.laserPrefab;
        }

        if (beamPrefab == null)
        {
            yield return FireSalvo();
            yield break;
        }

        float yL1 = Random.Range(areaMin.y, areaMax.y);
        float yL2 = Random.Range(areaMin.y, areaMax.y);
        float yR1 = Random.Range(areaMin.y, areaMax.y);
        float yR2 = Random.Range(areaMin.y, areaMax.y);

        // Beam-only hazards on each side (no chicken sprites).
        SpawnSideBeam(new Vector2(areaMin.x, yL1), faceLeft: false, beamPrefab);
        SpawnSideBeam(new Vector2(areaMin.x, yL2), faceLeft: false, beamPrefab);
        SpawnSideBeam(new Vector2(areaMax.x, yR1), faceLeft: true, beamPrefab);
        SpawnSideBeam(new Vector2(areaMax.x, yR2), faceLeft: true, beamPrefab);

        yield return new WaitForSeconds(specialLaserDuration + 0.15f);
    }

    private void SpawnSideBeam(Vector2 pos, bool faceLeft, GameObject beamPrefab)
    {
        GameObject host = new GameObject(faceLeft ? "BossSideBeam_R" : "BossSideBeam_L");
        host.transform.position = pos;
        BossSideBeam beam = host.AddComponent<BossSideBeam>();
        beam.Begin(beamPrefab, faceLeft, specialLaserDuration);
    }

    private void FireOneMissile()
    {
        if (missilePrefab == null)
            return;

        Transform home = farmerTarget;
        LaserChicken[] lasers = FindObjectsByType<LaserChicken>();
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] != null && lasers[i].IsHeld)
            {
                home = lasers[i].transform;
                break;
            }
        }

        if (home == null)
        {
            GameObject farmer = GameObject.Find("Farmer");
            if (farmer != null)
                home = farmer.transform;
        }

        GameObject missileGo = Instantiate(missilePrefab, transform.position, Quaternion.identity);
        BossMissile rocket = missileGo.GetComponent<BossMissile>();
        if (rocket == null)
            rocket = missileGo.AddComponent<BossMissile>();

        rocket.ConfigureExplosion(explosionPrefab);
        rocket.ConfigureFlight(missileSpeed, missileLifetime, missileTurnRate);

        // Fan missiles out so each starts in a different direction, then curves in.
        float t = missilesPerSalvo <= 1 ? 0.5f : (float)salvoIndex / (missilesPerSalvo - 1);
        float spread = Mathf.Lerp(-aimSpreadDegrees * 0.5f, aimSpreadDegrees * 0.5f, t);
        rocket.LaunchCurving(home, spread);
    }

    private int salvoIndex;

    private IEnumerator FireSalvo()
    {
        int count = Mathf.Max(1, missilesPerSalvo);
        for (int i = 0; i < count; i++)
        {
            if (dead)
                yield break;

            salvoIndex = i;
            FireOneMissile();

            if (salvoStagger > 0f && i < count - 1)
                yield return new WaitForSeconds(salvoStagger);
        }
    }

    private Transform PickRandomFlockChicken()
    {
        var flock = new List<Transform>();
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander c = chickens[i];
            if (c == null)
                continue;
            if (!IsFlockTarget(c.gameObject))
                continue;
            flock.Add(c.transform);
        }

        if (flock.Count == 0)
            return null;
        return flock[Random.Range(0, flock.Count)];
    }

    public static bool IsEnemyThreat(GameObject go)
    {
        if (go == null)
            return false;
        if (go.GetComponent<BossChicken>() != null)
            return false;
        if (go.GetComponent<LaserChicken>() != null)
            return false;

        return go.GetComponent<Bomb>() != null
            || go.GetComponent<MindCluck>() != null
            || go.GetComponent<ElectricChicken>() != null
            || go.GetComponent<GhostChicken>() != null;
    }

    public static bool IsPlainNormal(GameObject go)
    {
        if (go == null)
            return false;
        if (go.GetComponent<ChickenWander>() == null)
            return false;
        if (go.GetComponent<BossChicken>() != null)
            return false;
        if (go.GetComponent<LaserChicken>() != null)
            return false;
        if (go.GetComponent<Bomb>() != null)
            return false;
        if (go.GetComponent<MindCluck>() != null)
            return false;
        if (go.GetComponent<ElectricChicken>() != null)
            return false;
        if (go.GetComponent<PanicChicken>() != null)
            return false;
        if (go.GetComponent<GhostChicken>() != null)
            return false;
        return true;
    }

    public static bool IsFlockTarget(GameObject go)
    {
        if (go == null)
            return false;
        return IsPlainNormal(go) || go.GetComponent<PanicChicken>() != null;
    }

    private void Update()
    {
        if (dead || !combatActive)
            return;

        WanderStep();
    }

    private void WanderStep()
    {
        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, moveTarget) <= arrivalThreshold)
            PickNewTarget(force: false);

        Vector2 next = Vector2.MoveTowards(pos, moveTarget, moveSpeed * Time.deltaTime);
        transform.position = next;

        float dx = next.x - pos.x;
        if (spriteRenderer != null && Mathf.Abs(dx) > 0.01f)
            spriteRenderer.flipX = dx < 0f;

        if (animator != null)
            animator.SetBool(IsMovingHash, true);
    }

    private void PickNewTarget(bool force)
    {
        Vector2 pos = transform.position;
        Vector2 best = moveTarget;
        float bestDist = -1f;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(areaMin.x, areaMax.x),
                Random.Range(areaMin.y, areaMax.y));

            float dist = Vector2.Distance(pos, candidate);
            if (dist > bestDist)
            {
                bestDist = dist;
                best = candidate;
            }

            if (!force && dist >= minTargetDistance)
            {
                moveTarget = candidate;
                return;
            }
        }

        moveTarget = best;
    }

    private void Die()
    {
        if (dead)
            return;

        dead = true;
        combatActive = false;

        for (int i = BossMissile.ActiveMissiles.Count - 1; i >= 0; i--)
        {
            BossMissile m = BossMissile.ActiveMissiles[i];
            if (m != null)
                m.Explode();
        }

        Destroy(gameObject);
    }
}
