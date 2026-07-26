using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

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
    private static bool appQuitting;

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
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(timer).ToString();
        }
    }

    public void SetTimerRandom(float minSeconds, float maxSeconds)
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minSeconds, maxSeconds));
        float max = Mathf.Max(min, maxSeconds);
        timerMin = min;
        timerMax = max;
        timerConfigured = true;
        timer = Random.Range(min, max);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(timer).ToString();
        }
    }

    /// <summary>If true, chicken is destroyed after the strike animation (chaos mode).</summary>
    public void SetDestroyOnStrike(bool destroy)
    {
        destroyOnStrike = destroy;
    }

    /// <summary>
    /// Kill this electric: play fast lightning VFX + sound, then destroy.
    /// Safe to call from gun / splash / timer.
    /// </summary>
    public void Die()
    {
        if (dying || strikeFxPlayed)
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
            return;
        }

        dying = true;
        striking = true;
        enabled = false;

        Vector2 origin = transform.position;
        HideChickenVisuals();
        PlayStrikeFx(origin);

        // VFX is unparented and lives on its own; chicken can go immediately.
        Destroy(gameObject);
    }

    private void Start()
    {
        if (!timerConfigured)
            ResetTimer();
    }

    private void OnApplicationQuit()
    {
        appQuitting = true;
    }

    private void OnDestroy()
    {
        // Catch any raw Destroy() that skipped Die() — still show lightning.
        if (appQuitting || !Application.isPlaying)
            return;
        if (strikeFxPlayed)
            return;

        PlayStrikeFx(transform.position);
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

        // Chaos one-shots: same death FX as a gun kill.
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

        HideChickenVisuals();
        PlayStrikeFx(origin);

        float delay = Mathf.Clamp(ScaledDamageDelay, 0f, ScaledVfxDuration);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ApplyStrike(origin);

        float remaining = Mathf.Max(0f, ScaledVfxDuration - delay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (this == null)
            yield break;

        // Story electrics survive and arm the next strike.
        ShowChickenVisuals();
        ResetTimer();
        striking = false;
        strikeFxPlayed = false;
    }

    private void PlayStrikeFx(Vector2 origin)
    {
        if (strikeFxPlayed)
            return;

        strikeFxPlayed = true;

        if (electricStrike != null)
        {
            GameObject vfx = Instantiate(electricStrike, origin, Quaternion.identity);
            // Detach so it survives this chicken being destroyed.
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

    private void ShowChickenVisuals()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].enabled = true;
        }

        if (text != null)
            text.gameObject.SetActive(true);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
    }

    private void ResetTimer()
    {
        float min = timerConfigured ? timerMin : 4f;
        float max = timerConfigured ? timerMax : 9f;
        timer = Random.Range(min, max);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(timer).ToString();
        }
    }

    private void ApplyStrike(Vector2 origin)
    {
        // Chaos: only the gun and each chicken's own timer kill — no strike chains.
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

            Bomb bomb = chicken.GetComponent<Bomb>();
            if (bomb != null)
            {
                bomb.Detonate();
                continue;
            }

            ElectricChicken otherElectric = chicken.GetComponent<ElectricChicken>();
            if (otherElectric != null)
            {
                otherElectric.Die();
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
