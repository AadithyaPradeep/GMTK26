using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave-6 boss: marches from the right toward the left huddle. No missiles.
/// Multiple bosses can be alive at once.
/// </summary>
public class BossChicken : MonoBehaviour
{
    private static readonly List<BossChicken> Active = new List<BossChicken>();

    [Header("Prefabs")]
    [SerializeField] private GameObject livesPrefab;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Combat")]
    [SerializeField] private int maxLives = 2;
    [SerializeField] private float openingExplosionStagger = 0.06f;
    [SerializeField] private Vector3 livesLocalOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private float contactKillRadius = 0.7f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.6f;
    [SerializeField] private float yDriftSpeed = 1.4f;
    [SerializeField] private float yRetargetTime = 1.1f;
    [SerializeField] private Vector2 wanderAreaMin = new Vector2(-12f, -4.8f);
    [SerializeField] private Vector2 wanderAreaMax = new Vector2(6f, 4.1f);

    private int lives;
    private bool dead;
    private bool marchActive;
    private BossLivesDisplay livesDisplay;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 areaMin;
    private Vector2 areaMax;
    private float yTarget;
    private float yRetargetTimer;
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

    /// <summary>Kept for older call sites — returns first living boss if any.</summary>
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

    /// <param name="runOpening">Only the first boss should clear enemy mobs.</param>
    public void Begin(Vector2 spawnAreaMin, Vector2 spawnAreaMax, bool runOpening)
    {
        areaMin = wanderAreaMin;
        areaMax = wanderAreaMax;
        if (areaMax.x <= areaMin.x || areaMax.y <= areaMin.y)
        {
            areaMin = spawnAreaMin;
            areaMax = spawnAreaMax;
        }

        yTarget = transform.position.y;
        yRetargetTimer = 0f;
        SpawnLivesUi();
        StartCoroutine(BossRoutine(runOpening));
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

        return true;
    }

    public void ConfigurePrefabs(GameObject missile, GameObject lives)
    {
        // Missile unused in march mode; keep signature for spawner inject.
        if (lives != null)
            livesPrefab = lives;
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

    private IEnumerator BossRoutine(bool runOpening)
    {
        if (runOpening)
            yield return OpeningExplosions();

        marchActive = true;
        while (!dead)
            yield return null;
    }

    private IEnumerator OpeningExplosions()
    {
        List<Transform> enemies = CollectEnemyTargets();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (dead)
                yield break;

            Transform target = enemies[i];
            if (target == null)
                continue;

            ExplodeEnemyMob(target.gameObject);

            if (openingExplosionStagger > 0f)
                yield return new WaitForSeconds(openingExplosionStagger);
        }

        yield return new WaitForSeconds(0.2f);
    }

    private void ExplodeEnemyMob(GameObject enemy)
    {
        if (enemy == null)
            return;

        Vector3 pos = enemy.transform.position;

        Bomb bomb = enemy.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.Detonate();
            return;
        }

        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
            Destroy(fx, 0.7f);
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        Destroy(enemy);
    }

    private static List<Transform> CollectEnemyTargets()
    {
        var list = new List<Transform>();
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander c = chickens[i];
            if (c == null)
                continue;

            if (IsEnemyThreat(c.gameObject))
                list.Add(c.transform);
        }

        return list;
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
            || go.GetComponent<ElectricChicken>() != null;
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
        if (dead || !marchActive)
            return;

        MarchLeft();
        TryContactKillFlock();
    }

    private void MarchLeft()
    {
        Vector2 pos = transform.position;

        yRetargetTimer -= Time.deltaTime;
        if (yRetargetTimer <= 0f)
        {
            yTarget = Random.Range(areaMin.y, areaMax.y);
            yRetargetTimer = Random.Range(yRetargetTime * 0.7f, yRetargetTime * 1.4f);
        }

        float nextX = pos.x - moveSpeed * Time.deltaTime;
        float nextY = Mathf.MoveTowards(pos.y, yTarget, yDriftSpeed * Time.deltaTime);

        // Stop at left boundary but keep milling vertically.
        nextX = Mathf.Max(nextX, areaMin.x);
        nextY = Mathf.Clamp(nextY, areaMin.y, areaMax.y);

        transform.position = new Vector3(nextX, nextY, transform.position.z);

        if (spriteRenderer != null)
            spriteRenderer.flipX = true; // facing left while marching

        if (animator != null)
            animator.SetBool(IsMovingHash, true);
    }

    private void TryContactKillFlock()
    {
        float radiusSq = contactKillRadius * contactKillRadius;
        Vector2 origin = transform.position;
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;
            if (!IsFlockTarget(chicken.gameObject))
                continue;

            if (((Vector2)chicken.transform.position - origin).sqrMagnitude > radiusSq)
                continue;

            Destroy(chicken.gameObject);
        }
    }

    private void Die()
    {
        if (dead)
            return;

        dead = true;
        marchActive = false;
        Destroy(gameObject);
    }
}
