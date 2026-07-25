using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Countdown chicken that fires a continuous eye laser for several seconds.
/// On the boss wave it switches to manual Space-bar fire (no timer).
/// </summary>
public class LaserChicken : MonoBehaviour
{
    public float timer = 5f;
    public TextMeshPro text;
    public GameObject laserPrefab;
    public CinemachineImpulseSource source;

    [Header("Beam")]
    [Tooltip("Native LaserBeam sprite width in world units (128px @ 16 PPU).")]
    [SerializeField] private float nativeBeamLength = 8f;
    [SerializeField] private float laserHalfThickness = 0.45f;
    [SerializeField] private float missileHitHalfThickness = 0.85f;
    [SerializeField] private float laserDuration = 5f;

    [Header("Manual / Boss burst")]
    [Tooltip("How long each machine-gun burst beam is visible.")]
    [SerializeField] private float burstDuration = 0.045f;
    [Tooltip("Delay between burst starts while Space is held.")]
    [SerializeField] private float burstInterval = 0.07f;

    [Header("Gun visual (boss wave)")]
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private string gunFireState = "GunFire";
    [Tooltip("If > 0, overrides animator speed so one fire cycle matches burstInterval.")]
    [SerializeField] private bool matchGunAnimToBurstRate = true;

    [Header("Cooldown")]
    [SerializeField] private float cooldownMin = 5f;
    [SerializeField] private float cooldownMax = 10f;

    private SpriteRenderer spriteRenderer;
    private ChickenWander wander;
    private GameObject activeBeam;
    private SpriteRenderer beamRenderer;
    private bool firing;
    private bool held;
    private bool immune;
    private bool manualFire;
    private readonly HashSet<BossChicken> damagedBossesThisShot = new HashSet<BossChicken>();
    private float nextBurstTime;
    private Coroutine burstRoutine;
    private int gunFireStateHash;

    public bool IsFiring => firing;
    public bool IsImmune => immune;
    public bool IsManualFire => manualFire;

    /// <summary>GrabCluck sets this so we don't re-enable wander while carried.</summary>
    public bool IsHeld
    {
        get => held;
        set => held = value;
    }

    public void SetImmune(bool value) => immune = value;

    public void SetManualFire(bool enabled)
    {
        manualFire = enabled;
        if (manualFire)
        {
            timer = 0f;
            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.text = "SPC";
            }
        }
        else if (!firing)
        {
            ResetTimer();
        }
    }

    public void SetCooldown(float min, float max)
    {
        cooldownMin = Mathf.Max(0.1f, min);
        cooldownMax = Mathf.Max(cooldownMin, max);
        if (!firing && !manualFire)
            ResetTimer();
    }

    /// <summary>Called from GrabCluck on Space during boss wave.</summary>
    public static bool TryFireAnyManual()
    {
        LaserChicken[] lasers = FindObjectsByType<LaserChicken>();
        for (int i = 0; i < lasers.Length; i++)
        {
            LaserChicken laser = lasers[i];
            if (laser == null || !laser.manualFire)
                continue;

            if (laser.TryFireManual())
                return true;
        }

        return false;
    }

    /// <summary>Instant burst; call every frame while Space is held for machine-gun fire.</summary>
    public bool TryFireManual()
    {
        if (!manualFire)
            return false;
        if (Time.time < nextBurstTime)
            return false;

        nextBurstTime = Time.time + burstInterval;
        FireBurst();
        return true;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        wander = GetComponent<ChickenWander>();
        if (gunAnimator == null)
            gunAnimator = GetComponent<Animator>();
        gunFireStateHash = Animator.StringToHash(gunFireState);
        SyncGunAnimatorSpeed();
    }

    /// <summary>Boss-wave gun setup: keep laser beam VFX, drive the gun sprite animator.</summary>
    public void ConfigureBossGun(GameObject beamPrefab, float? burstRate = null)
    {
        if (beamPrefab != null)
            laserPrefab = beamPrefab;

        if (burstRate.HasValue && burstRate.Value > 0.01f)
            burstInterval = burstRate.Value;

        if (gunAnimator == null)
            gunAnimator = GetComponent<Animator>();

        gunFireStateHash = Animator.StringToHash(gunFireState);
        SyncGunAnimatorSpeed();
        SetImmune(true);
        SetManualFire(true);
    }

    private void SyncGunAnimatorSpeed()
    {
        if (gunAnimator == null || !matchGunAnimToBurstRate)
            return;

        // GunFire clip length is typically ~1s; scale so one loop ≈ one burst.
        gunAnimator.speed = 1f / Mathf.Max(0.05f, burstInterval);
    }

    private void Start()
    {
        if (!manualFire)
            ResetTimer();
        else if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = "SPC";
        }
    }

    private void OnDestroy()
    {
        DestroyActiveBeam();
    }

    private void OnDisable()
    {
        DestroyActiveBeam();
        firing = false;
    }

    private void Update()
    {
        if (firing)
        {
            UpdateBeamFacing();
            return;
        }

        // Boss-wave laser: no auto timer — Space triggers fire via GrabCluck.
        if (manualFire)
        {
            SyncHeldFacingFromFarmer();
            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.text = "SPC";
            }
            return;
        }

        if (timer > 0f)
        {
            if (text != null)
                text.text = Mathf.RoundToInt(timer).ToString();
            timer -= Time.deltaTime;
        }

        if (timer <= 0f)
            Fire();
    }

    private void Fire()
    {
        if (firing)
            return;

        firing = true;
        damagedBossesThisShot.Clear();
        StartCoroutine(FireRoutine());
    }

    private void FireBurst()
    {
        // Don't stack burst coroutines; drop the previous flash and fire again.
        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        DestroyActiveBeam();
        damagedBossesThisShot.Clear();
        burstRoutine = StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        firing = true;
        PlayGunFireAnim();

        if (laserPrefab != null)
        {
            activeBeam = Instantiate(laserPrefab, transform);
            activeBeam.transform.localPosition = Vector3.zero;
            activeBeam.transform.localRotation = Quaternion.identity;
            beamRenderer = activeBeam.GetComponent<SpriteRenderer>();
            UpdateBeamFacing();
        }

        if (source != null)
            source.GenerateImpulse();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        // Instant hit-scan for this burst.
        ApplyBeamDamageOnce();

        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            if (this == null)
                yield break;

            elapsed += Time.deltaTime;
            UpdateBeamFacing();
            yield return null;
        }

        DestroyActiveBeam();
        firing = false;
        burstRoutine = null;

        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = "SPC";
        }
    }

    private void PlayGunFireAnim()
    {
        if (gunAnimator == null)
            return;

        SyncGunAnimatorSpeed();
        gunAnimator.Play(gunFireStateHash, 0, 0f);
    }

    private void SyncHeldFacingFromFarmer()
    {
        if (!held || spriteRenderer == null || transform.parent == null)
            return;

        SpriteRenderer farmer = transform.parent.GetComponent<SpriteRenderer>();
        if (farmer != null)
            spriteRenderer.flipX = farmer.flipX;
    }

    private void ApplyBeamDamageOnce()
    {
        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        Vector2 origin = transform.position;
        Vector2 direction = facingLeft ? Vector2.left : Vector2.right;
        KillAlongBeam(origin, direction);
    }

    private IEnumerator FireRoutine()
    {
        DestroyActiveBeam();

        if (laserPrefab != null)
        {
            activeBeam = Instantiate(laserPrefab, transform);
            activeBeam.transform.localPosition = Vector3.zero;
            activeBeam.transform.localRotation = Quaternion.identity;
            beamRenderer = activeBeam.GetComponent<SpriteRenderer>();
            UpdateBeamFacing();

            Destroy(activeBeam, laserDuration + 0.05f);
        }

        if (source != null)
            source.GenerateImpulse();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        float elapsed = 0f;
        while (elapsed < laserDuration)
        {
            if (this == null)
                yield break;

            elapsed += Time.deltaTime;
            UpdateBeamFacing();

            bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
            Vector2 origin = transform.position;
            Vector2 direction = facingLeft ? Vector2.left : Vector2.right;
            KillAlongBeam(origin, direction);

            yield return null;
        }

        DestroyActiveBeam();

        if (this == null)
            yield break;

        if (wander != null && !held)
            wander.enabled = true;

        if (manualFire)
        {
            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.text = "SPC";
            }
        }
        else
        {
            ResetTimer();
        }

        firing = false;
    }

    private void UpdateBeamFacing()
    {
        if (activeBeam == null)
            return;

        activeBeam.transform.localPosition = Vector3.zero;
        activeBeam.transform.localRotation = Quaternion.identity;

        if (beamRenderer != null && spriteRenderer != null)
            beamRenderer.flipX = spriteRenderer.flipX;
    }

    private void DestroyActiveBeam()
    {
        if (activeBeam == null)
            return;

        Destroy(activeBeam);
        activeBeam = null;
        beamRenderer = null;
    }

    private void KillAlongBeam(Vector2 origin, Vector2 direction)
    {
        IReadOnlyList<BossChicken> bosses = BossChicken.ActiveBosses;
        for (int i = 0; i < bosses.Count; i++)
        {
            BossChicken boss = bosses[i];
            if (boss == null || boss.IsDead)
                continue;
            if (damagedBossesThisShot.Contains(boss))
                continue;
            if (!IsInBeam(origin, direction, boss.transform.position, laserHalfThickness))
                continue;

            if (boss.TakeDamage(1))
                damagedBossesThisShot.Add(boss);
        }

        // Laser detonates boss missiles in the beam (slightly wider hit for rockets).
        IReadOnlyList<BossMissile> missiles = BossMissile.ActiveMissiles;
        for (int i = missiles.Count - 1; i >= 0; i--)
        {
            BossMissile missile = missiles[i];
            if (missile == null)
                continue;
            if (IsInBeam(origin, direction, missile.transform.position, missileHitHalfThickness))
                missile.Explode();
        }

        // Boss-wave laser: only hurts bosses (and missiles), never the flock.
        if (manualFire)
            return;

        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            if (chicken.gameObject == gameObject)
                continue;

            if (chicken.GetComponent<BossChicken>() != null)
                continue;

            LaserChicken otherLaser = chicken.GetComponent<LaserChicken>();
            if (otherLaser != null && otherLaser.IsImmune)
                continue;

            if (!IsInBeam(origin, direction, chicken.transform.position, laserHalfThickness))
                continue;

            Bomb bomb = chicken.GetComponent<Bomb>();
            if (bomb != null)
            {
                bomb.Detonate();
                continue;
            }

            Destroy(chicken.gameObject);
        }
    }

    private bool IsInBeam(Vector2 origin, Vector2 direction, Vector2 point, float halfThickness)
    {
        Vector2 toPoint = point - origin;
        float along = Vector2.Dot(toPoint, direction);
        if (along < 0f || along > nativeBeamLength)
            return false;

        Vector2 perp = new Vector2(-direction.y, direction.x);
        float across = Mathf.Abs(Vector2.Dot(toPoint, perp));
        return across <= halfThickness;
    }

    private void ResetTimer()
    {
        timer = Random.Range(cooldownMin, cooldownMax);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(timer).ToString();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        bool facingLeft = sr != null && sr.flipX;
        Vector2 direction = facingLeft ? Vector2.left : Vector2.right;
        Vector2 origin = transform.position;
        Vector2 end = origin + direction * nativeBeamLength;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
        Gizmos.DrawLine(origin, end);
    }
#endif
}
