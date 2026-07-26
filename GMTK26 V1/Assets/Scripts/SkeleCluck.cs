using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// World3 skeleton chicken — fire-style countdown, then a burst of arrows in random directions.
/// </summary>
public class SkeleCluck : MonoBehaviour
{
    public float timer = 5f;
    public TextMeshPro text;
    public GameObject arrowPrefab;
    public GameObject hitExplosionPrefab;
    public CinemachineImpulseSource source;

    [Header("Arrow")]
    [SerializeField] private float arrowSpeed = 18f;
    [SerializeField] [Range(0.1f, 1f)] private float mapTravelFraction = 0.6f;
    [SerializeField] private float hitVfxDuration = 0.7f;
    [SerializeField] private float bodyHitRadius = 0.35f;
    [SerializeField] private int shotsPerBurst = 3;
    [SerializeField] private float shotInterval = 0.12f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 5f;

    private SpriteRenderer spriteRenderer;
    private ChickenWander wander;
    private bool firing;
    private bool held;
    private Coroutine burstRoutine;

    public bool IsFiring => firing;

    /// <summary>GrabCluck sets this so we don't re-enable wander while carried.</summary>
    public bool IsHeld
    {
        get => held;
        set => held = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        wander = GetComponent<ChickenWander>();
    }

    private void Start()
    {
        ResetTimer();
    }

    private void OnDisable()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        firing = false;
    }

    private void Update()
    {
        if (held)
            SyncHeldFacingFromFarmer();

        if (firing)
            return;

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
        if (firing || arrowPrefab == null)
            return;

        firing = true;
        if (text != null)
            text.gameObject.SetActive(false);

        if (wander != null && !held)
            wander.enabled = false;

        if (burstRoutine != null)
            StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        int shots = Mathf.Max(1, shotsPerBurst);
        float travel = ResolveTravelDistance();
        float gap = Mathf.Max(0.02f, shotInterval);

        for (int i = 0; i < shots; i++)
        {
            if (this == null)
                yield break;

            if (held)
                SyncHeldFacingFromFarmer();

            LaunchOne(travel);

            if (i < shots - 1)
                yield return new WaitForSeconds(gap);
        }

        burstRoutine = null;
        firing = false;

        if (wander != null && !held)
            wander.enabled = true;

        ResetTimer();
    }

    private void SyncHeldFacingFromFarmer()
    {
        if (!held || spriteRenderer == null || transform.parent == null)
            return;

        SpriteRenderer farmer = transform.parent.GetComponent<SpriteRenderer>();
        if (farmer != null)
            spriteRenderer.flipX = farmer.flipX;
    }

    private void LaunchOne(float travel)
    {
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;
        direction.Normalize();

        Vector2 origin = transform.position;

        GameObject arrow = Instantiate(arrowPrefab, origin, Quaternion.identity);
        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile == null)
            projectile = arrow.AddComponent<ArrowProjectile>();

        projectile.Launch(
            origin,
            direction,
            travel,
            arrowSpeed,
            gameObject,
            hitExplosionPrefab,
            hitVfxDuration,
            bodyHitRadius,
            source);
    }

    private float ResolveTravelDistance()
    {
        ChickenSpawner spawner = FindAnyObjectByType<ChickenSpawner>();
        if (spawner != null)
            return Mathf.Max(1f, spawner.MapWidth * mapTravelFraction);

        return 10f * mapTravelFraction / 0.6f;
    }

    private void ResetTimer()
    {
        timer = Mathf.Max(0.1f, cooldown);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(timer).ToString();
        }
    }
}
