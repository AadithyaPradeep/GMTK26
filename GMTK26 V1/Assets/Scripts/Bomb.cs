using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

public class Bomb : MonoBehaviour
{
    public float timer;
    public TextMeshPro text;
    public GameObject explosion;
    public CinemachineImpulseSource source;

    [Header("Blast")]
    [Tooltip("Kill radius in world units. Explosion sprites are 64px @ 16 PPU (4 units across), so ~2 matches the visible blast.")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionVfxDuration = 0.7f;

    private bool dead;
    private AudioSource tickSource;
    private bool fuseConfigured;

    /// <summary>Override the default 5–10s fuse (e.g. boss wave short timers).</summary>
    public void SetFuse(float seconds)
    {
        timer = Mathf.Max(0.05f, seconds);
        fuseConfigured = true;
        if (text != null)
            text.text = Mathf.CeilToInt(timer).ToString();
    }

    public void SetFuseRandom(float minSeconds, float maxSeconds)
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minSeconds, maxSeconds));
        float max = Mathf.Max(min, maxSeconds);
        SetFuse(Random.Range(min, max));
    }

    private void Start()
    {
        if (!fuseConfigured)
            timer = Random.Range(5, 11);

        if (GameAudio.Instance != null)
            tickSource = GameAudio.Instance.CreateTickSource(gameObject);
    }

    private void Update()
    {
        if (dead)
            return;

        if (timer > 0f)
        {
            if (text != null)
                text.text = Mathf.RoundToInt(timer).ToString();
            timer -= Time.deltaTime;
        }

        if (timer <= 0f)
            Detonate();
    }

    private bool skipElectricsInBlast;

    /// <summary>
    /// Forces an immediate explosion (timer expiry or external trigger like an electric strike).
    /// </summary>
    public void Detonate(bool skipElectrics = false)
    {
        if (dead)
            return;

        dead = true;
        skipElectricsInBlast = skipElectrics;

        if (tickSource != null)
        {
            tickSource.Stop();
            tickSource = null;
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        StartCoroutine(BlastRoutine());

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        if (text != null)
            text.gameObject.SetActive(false);

        ChickenWander wander = GetComponent<ChickenWander>();
        if (wander != null)
            wander.enabled = false;
    }

    private IEnumerator BlastRoutine()
    {
        Vector2 origin = transform.position;
        GameObject spawnS = null;
        if (explosion != null)
        {
            spawnS = Instantiate(explosion, origin, Quaternion.identity);
            // Survives this bomb being destroyed early in a chain reaction.
            Destroy(spawnS, explosionVfxDuration);
        }

        if (source != null)
            source.GenerateImpulse();

        // Chaos: only the gun and each chicken's own timer kill — no blast chains.
        if (!GameMode.IsChaos)
            KillChickensInRadius(origin);

        yield return new WaitForSeconds(explosionVfxDuration);
        Destroy(gameObject);
    }

    private void KillChickensInRadius(Vector2 origin)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float radiusSq = explosionRadius * explosionRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            if (chicken.gameObject == gameObject)
                continue;

            // Exploding chickens don't kill other exploding chickens.
            if (chicken.GetComponent<Bomb>() != null)
                continue;

            ElectricChicken electric = chicken.GetComponent<ElectricChicken>();
            if (electric != null)
            {
                // Timer bombs kill electrics. Lightning-triggered bombs must not
                // (otherwise electrics look like they kill each other / themselves).
                if (skipElectricsInBlast || electric.IsStriking)
                    continue;
            }

            LaserChicken laser = chicken.GetComponent<LaserChicken>();
            if (laser != null && laser.IsImmune)
                continue;

            if (chicken.GetComponent<BossChicken>() != null)
                continue;

            if (GhostChicken.IsProtected(chicken))
                continue;

            Vector2 toChicken = (Vector2)chicken.transform.position - origin;
            if (toChicken.sqrMagnitude <= radiusSq)
                Destroy(chicken.gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
