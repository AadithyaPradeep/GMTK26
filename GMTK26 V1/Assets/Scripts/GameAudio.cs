using System.Collections;
using UnityEngine;

/// <summary>
/// Central audio hub for BGM and one-shot SFX.
/// </summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    /// <summary>
    /// Set by Home → Story before LoadHoldBlack so BGM doesn't start under How To Play
    /// (and so we don't start Farm music when another map was selected).
    /// </summary>
    public static bool HoldBgmForIntro { get; set; }

    [Header("Clips")]
    [SerializeField] private AudioClip bgm;
    [SerializeField] private AudioClip chickenIdle;
    [SerializeField] private AudioClip explosion;
    [SerializeField] private AudioClip bombTick;
    [SerializeField] private AudioClip grab;
    [SerializeField] private AudioClip drop;
    [SerializeField] private AudioClip step;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float idleCluckVolume = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float stepVolume = 0.55f;
    [SerializeField] [Range(0f, 1f)] private float tickVolume = 0.5f;

    private const string MusicPrefKey = "MusicVolume";
    private const string SfxPrefKey = "SfxVolume";

    [Header("Idle Clucks")]
    [SerializeField] private float idleCluckMinInterval = 2.5f;
    [SerializeField] private float idleCluckMaxInterval = 5.5f;

    public AudioClip BombTickClip => bombTick;
    public float TickVolume => tickVolume * sfxVolume;

    public float MusicVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            if (bgmSource != null)
                bgmSource.volume = bgmVolume;
            PlayerPrefs.SetFloat(MusicPrefKey, bgmVolume);
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxPrefKey, sfxVolume);
        }
    }

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (PlayerPrefs.HasKey(MusicPrefKey))
            bgmVolume = PlayerPrefs.GetFloat(MusicPrefKey, bgmVolume);
        if (PlayerPrefs.HasKey(SfxPrefKey))
            sfxVolume = PlayerPrefs.GetFloat(SfxPrefKey, sfxVolume);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private bool idleLoopStarted;

    private void Start()
    {
        if (HoldBgmForIntro)
            return;

        BeginGameplayAudio();
    }

    /// <summary>Call after How To Play / scene reveal so BGM starts on the real map only.</summary>
    public void ReleaseIntroHold()
    {
        HoldBgmForIntro = false;
        BeginGameplayAudio();
    }

    private void BeginGameplayAudio()
    {
        PlayBgm();
        if (!idleLoopStarted)
        {
            idleLoopStarted = true;
            StartCoroutine(IdleCluckLoop());
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayBgm()
    {
        if (bgm == null || bgmSource == null)
            return;

        bgmSource.clip = bgm;
        bgmSource.volume = bgmVolume;
        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void PlayGrab() => PlaySfx(grab, sfxVolume);
    public void PlayDrop() => PlaySfx(drop, sfxVolume);
    public void PlayExplosion() => PlaySfx(explosion, sfxVolume);
    public void PlayStep() => PlaySfx(step, stepVolume * sfxVolume);
    public void PlayChickenIdle() => PlaySfx(chickenIdle, idleCluckVolume * sfxVolume, Random.Range(0.92f, 1.08f));

    public AudioSource CreateTickSource(GameObject host)
    {
        if (bombTick == null || host == null)
            return null;

        AudioSource source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = bombTick;
        source.volume = tickVolume * sfxVolume;
        source.Play();
        return source;
    }

    private void PlaySfx(AudioClip clip, float volume, float pitch = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = 1f;
    }

    private IEnumerator IdleCluckLoop()
    {
        while (enabled)
        {
            float wait = Random.Range(idleCluckMinInterval, idleCluckMaxInterval);
            yield return new WaitForSecondsRealtime(wait);

            if (PauseMenu.IsPaused)
                continue;

            if (chickenIdle == null)
                continue;

            ChickenSpawner spawner = FindAnyObjectByType<ChickenSpawner>();
            if (spawner != null && !spawner.HasStarted)
                continue;

            // Soft ambient cluck so the farm feels alive.
            PlayChickenIdle();
        }
    }
}
