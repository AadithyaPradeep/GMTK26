using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Story/map play: countdown → lightning AoE → survive, reset timer, keep roaming/chasing.
/// Only bomb explosions (not lightning, not other electrics) kill this chicken.
/// Chaos: optional one-shot on timer via SetDestroyOnStrike(true).
/// </summary>
public class ElectricChicken : MonoBehaviour
{
    public float timer = 5f;
    public TextMeshPro text;
    public GameObject electricStrike;
    public CinemachineImpulseSource source;

    [Header("Strike")]
    [Tooltip("Kill / detonate radius. ElectricStrike frames are 128px @ 16 PPU (8 units across), so ~4 matches the visible blast.")]
    [SerializeField] private float strikeRadius = 4f;
    [Tooltip("Full length of the Electric VFX clip at 1x (~0.68s). Scaled by animSpeed.")]
    [SerializeField] private float strikeVfxDuration = 0.68f;
    [Tooltip("When damage applies, measured from strike start at 1x speed (story mode).")]
    [SerializeField] private float damageDelay = 0.55f;
    [Tooltip("Playback speed for death / strike lightning. Higher = faster.")]
    [SerializeField] private float animSpeed = 4f;

    private bool striking;
    private bool timerConfigured;
    private bool destroyOnStrike;
    private bool strikeFxPlayed;
    private bool dying;
    private float timerMin = 4f;
    private float timerMax = 9f;

    private float ScaledVfxDuration => strikeVfxDuration / Mathf.Max(0.01f, animSpeed);
    private float ScaledDamageDelay => damageDelay / Mathf.Max(0.01f, animSpeed);

    /// <summary>True while this chicken is mid lightning strike / death.</summary>
    public bool IsStriking => striking || dying;

    /// <summary>Override the default 4–9s strike timer (e.g. chaos mode 5–7s).</summary>
    public void SetTimer(float seconds)
    {
        timer = Mathf.Max(0.05f, seconds);
        timerConfigured = true;
        timerMin = timer;
        timerMax = timer;
        SyncTimerText();
    }

    public void SetTimerRandom(float minSeconds, float maxSeconds)
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minSeconds, maxSeconds));
        float max = Mathf.Max(min, maxSeconds);
        timerMin = min;
        timerMax = max;
        timerConfigured = true;
        timer = Random.Range(min, max);
        SyncTimerText();
    }

    /// <summary>If true, chicken is destroyed after the strike animation (chaos mode).</summary>
    public void SetDestroyOnStrike(bool destroy)
    {
        destroyOnStrike = destroy;
    }

    /// <summary>
    /// Explicit death (chaos gun / chaos timer). Story electrics die only via bomb Destroy().
    /// </summary>
    public void Die()
    {
        if (dying)
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
            return;
        }

        // Never self-kill mid story strike.
        if (striking && !destroyOnStrike)
            return;

        dying = true;
        striking = true;
        enabled = false;

        HideChickenVisuals();
        PlayStrikeFx(transform.position);
        Destroy(gameObject);
    }

    private void Start()
    {
        if (!timerConfigured)
            ResetTimer();
    }

    private void Update()
    {
        if (striking || dying)
            return;

        if (timer > 0f)
        {
            if (text != null)
                text.text = Mathf.RoundToInt(timer).ToString();
            timer -= Time.deltaTime;
        }

        if (timer <= 0f)
            Strike();
    }

    private void Strike()
    {
        if (striking || dying)
            return;

        if (destroyOnStrike)
        {
            Die();
            return;
        }

        striking = true;
        StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        Vector2 origin = transform.position;

        // Pause movement for the flash only — chicken STAYS VISIBLE (hiding looked like death).
        ChickenWander wander = GetComponent<ChickenWander>();
        if (wander != null)
            wander.enabled = false;

        PlayStrikeFx(origin);

        float delay = Mathf.Clamp(ScaledDamageDelay, 0f, ScaledVfxDuration);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ApplyStrike(origin);

        float remaining = Mathf.Max(0f, ScaledVfxDuration - delay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (this == null || dying)
            yield break;

        ResetTimer();
        striking = false;
        strikeFxPlayed = false;

        // Resume roam / chase unless farmer is still holding this chicken.
        if (wander != null && transform.parent == null)
            wander.enabled = true;
    }

    private void PlayStrikeFx(Vector2 origin)
    {
        if (strikeFxPlayed)
            return;

        strikeFxPlayed = true;

        if (electricStrike != null)
        {
            GameObject vfx = Instantiate(electricStrike, origin, Quaternion.identity);
            vfx.transform.SetParent(null);
            Animator anim = vfx.GetComponent<Animator>();
            if (anim != null)
                anim.speed = animSpeed;
            Destroy(vfx, Mathf.Max(0.05f, ScaledVfxDuration));
        }

        if (source != null)
            source.GenerateImpulse();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();
    }

    private void HideChickenVisuals()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].enabled = false;
        }

        if (text != null)
            text.gameObject.SetActive(false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        ChickenWander wander = GetComponent<ChickenWander>();
        if (wander != null)
            wander.enabled = false;
    }

    private void ResetTimer()
    {
        float min = timerConfigured ? timerMin : 4f;
        float max = timerConfigured ? timerMax : 9f;
        timer = Random.Range(min, max);
        SyncTimerText();
    }

    private void SyncTimerText()
    {
        if (text == null)
            return;
        text.gameObject.SetActive(true);
        text.text = Mathf.RoundToInt(timer).ToString();
    }

    private void ApplyStrike(Vector2 origin)
    {
        // Chaos: no AoE — gun + own timer only.
        if (GameMode.IsChaos)
            return;

        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float radiusSq = strikeRadius * strikeRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            if (chicken.gameObject == gameObject)
                continue;

            LaserChicken laser = chicken.GetComponent<LaserChicken>();
            if (laser != null && laser.IsImmune)
                continue;

            if (chicken.GetComponent<BossChicken>() != null)
                continue;

            if (GhostChicken.IsProtected(chicken))
                continue;

            Vector2 toChicken = (Vector2)chicken.transform.position - origin;
            if (toChicken.sqrMagnitude > radiusSq)
                continue;

            // Electrics never kill each other — lightning skips all ElectricChickens.
            if (chicken.GetComponent<ElectricChicken>() != null)
                continue;

            Bomb bomb = chicken.GetComponent<Bomb>();
            if (bomb != null)
            {
                // Lightning-triggered blasts must not wipe electrics (looked like mutual kill).
                bomb.Detonate(skipElectrics: true);
                continue;
            }

            Destroy(chicken.gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, strikeRadius);
    }
#endif
}
