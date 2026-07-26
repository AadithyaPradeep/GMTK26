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
    [SerializeField] private float laserHalfThickness = 0.7f;
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
    private bool protectFlock;
    private bool manualFullBeam = true;
    private float manualCooldown = 2f;
    private float manualBeamDuration = 0.9f;
    private readonly HashSet<BossChicken> damagedBossesThisShot = new HashSet<BossChicken>();
    private float nextBurstTime;
    private Coroutine burstRoutine;
    private Coroutine fireRoutine;
    private int gunFireStateHash;
    private bool timerForced;

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

    /// <summary>When true, the beam still hurts bosses/missiles but never flock chickens.</summary>
    public void SetProtectFlock(bool value) => protectFlock = value;

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

    /// <summary>Force the next countdown (e.g. story boss laser = 5s).</summary>
    public void SetNextTimer(float seconds)
    {
        timer = Mathf.Max(0.1f, seconds);
        timerForced = true;
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.CeilToInt(timer).ToString();
        }
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

    /// <summary>Held story-boss weapon: Space fires a full beam with cooldown.</summary>
    public void ConfigureHeldBossLaser(float cooldownSeconds, float beamDurationSeconds)
    {
        SetImmune(true);
        SetProtectFlock(true);
        SetManualFire(true);
        manualFullBeam = true;
        manualCooldown = Mathf.Max(0.1f, cooldownSeconds);
        manualBeamDuration = Mathf.Max(0.1f, beamDurationSeconds);
        nextBurstTime = 0f;
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = "SPC";
        }
    }

    /// <summary>Boss special: fire a continuous beam for the given duration then optionally destroy.</summary>
    public void ForceContinuousFire(float duration, bool destroyWhenDone = false)
    {
        SetImmune(true);
        SetManualFire(false);
        timer = 999f;
        if (text != null)
            text.gameObject.SetActive(false);

        if (wander != null)
            wander.enabled = false;

        if (fireRoutine != null)
            StopCoroutine(fireRoutine);
        fireRoutine = StartCoroutine(ForcedFireRoutine(duration, destroyWhenDone));
    }

    /// <summary>Instant burst; call every frame while Space is held for machine-gun fire.
    /// For held story laser (full beam), fires once per press with cooldown.</summary>
    public bool TryFireManual()
    {
        if (!manualFire)
            return false;
        if (firing)
            return false;
        if (Time.time < nextBurstTime)
            return false;

        nextBurstTime = Time.time + (manualFullBeam ? manualCooldown : burstInterval);

        if (manualFullBeam)
        {
            FireForDuration(manualBeamDuration);
            return true;
        }

        FireBurst();
        return true;
    }

    private void FireForDuration(float duration)
    {
        if (firing)
            return;

        firing = true;
        damagedBossesThisShot.Clear();
        if (fireRoutine != null)
            StopCoroutine(fireRoutine);
        fireRoutine = StartCoroutine(FireRoutine(duration));
    }

    private IEnumerator ForcedFireRoutine(float duration, bool destroyWhenDone)
    {
        yield return FireRoutine(duration);
        fireRoutine = null;
        if (destroyWhenDone && this != null)
            Destroy(gameObject);
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
        manualFullBeam = false;
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
        if (!manualFire && !timerForced)
            ResetTimer();
        else if (manualFire && text != null)
        {
            text.gameObject.SetActive(true);
            text.text = "SPC";
        }
        else if (timerForced && text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.CeilToInt(timer).ToString();
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
            if (held)
                SyncHeldFacingFromFarmer();
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
                float remaining = nextBurstTime - Time.time;
                text.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "SPC";
            }
            return;
        }

        if (held)
            SyncHeldFacingFromFarmer();

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
        FireForDuration(laserDuration);
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

    private IEnumerator FireRoutine(float duration)
    {
        DestroyActiveBeam();

        if (laserPrefab != null)
        {
            activeBeam = Instantiate(laserPrefab, transform);
            activeBeam.transform.localPosition = Vector3.zero;
            activeBeam.transform.localRotation = Quaternion.identity;
            beamRenderer = activeBeam.GetComponent<SpriteRenderer>();
            UpdateBeamFacing();

            Destroy(activeBeam, duration + 0.05f);
        }

        if (source != null)
            source.GenerateImpulse();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (this == null)
                yield break;

            elapsed += Time.deltaTime;
            if (held)
                SyncHeldFacingFromFarmer();
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
        else if (!timerForced)
        {
            ResetTimer();
        }

        firing = false;
        fireRoutine = null;
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
            if (!IsBodyInBeam(origin, direction, boss.gameObject, laserHalfThickness))
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
            if (IsBodyInBeam(origin, direction, missile.gameObject, missileHitHalfThickness))
                missile.Explode();
        }

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

            if (GhostChicken.IsProtected(chicken))
                continue;

            LaserChicken otherLaser = chicken.GetComponent<LaserChicken>();
            if (otherLaser != null && otherLaser.IsImmune)
                continue;

            if (!IsBodyInBeam(origin, direction, chicken.gameObject, laserHalfThickness))
                continue;

            Bomb bomb = chicken.GetComponent<Bomb>();
            if (bomb != null)
            {
                bomb.Detonate();
                continue;
            }

            // Chaos gun only — story electrics die solely from bomb explosions.
            ElectricChicken electric = chicken.GetComponent<ElectricChicken>();
            if (electric != null)
            {
                if (GameMode.IsChaos)
                    electric.Die();
                continue;
            }

            // Held gun / protected story laser: never wipe normals / panics / minds.
            if (manualFire || protectFlock)
                continue;

            Destroy(chicken.gameObject);
        }
    }

    /// <summary>
    /// True if the beam strip overlaps the target's collider (or sprite bounds).
    /// Uses closest point on the body so any part of the sprite can be hit.
    /// </summary>
    private bool IsBodyInBeam(Vector2 origin, Vector2 direction, GameObject target, float halfThickness)
    {
        if (target == null)
            return false;

        Vector2 sample = GetBodySamplePoint(origin, direction, target);
        return IsInBeam(origin, direction, sample, halfThickness);
    }

    private Vector2 GetBodySamplePoint(Vector2 origin, Vector2 direction, GameObject target)
    {
        // Point on the beam nearest the body's center, then snap to the body surface.
        Vector2 center = target.transform.position;
        Collider2D col = target.GetComponent<Collider2D>();
        if (col != null && col.enabled)
            center = col.bounds.center;
        else
        {
            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr != null)
                center = sr.bounds.center;
        }

        float along = Mathf.Clamp(Vector2.Dot(center - origin, direction), 0f, nativeBeamLength);
        Vector2 onBeam = origin + direction * along;

        if (col != null && col.enabled)
            return col.ClosestPoint(onBeam);

        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite != null)
            return ClosestPointOnBounds(sprite.bounds, onBeam);

        return center;
    }

    private static Vector2 ClosestPointOnBounds(Bounds bounds, Vector2 point)
    {
        return new Vector2(
            Mathf.Clamp(point.x, bounds.min.x, bounds.max.x),
            Mathf.Clamp(point.y, bounds.min.y, bounds.max.y));
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
