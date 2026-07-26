using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central audio hub for BGM and one-shot SFX.
/// Voice / map clips also load from Resources/Music so they work even if
/// scene Inspector refs are stale (common when Unity holds an old scene in memory).
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

    [Header("Voice / Ability Clips")]
    [SerializeField] private AudioClip alienPower;
    [SerializeField] private AudioClip laserFire;
    [SerializeField] private AudioClip zombieSpawn;
    [SerializeField] private AudioClip mindHypnosis;
    [SerializeField] private AudioClip fireAmbient;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float idleCluckVolume = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float stepVolume = 0.55f;
    [SerializeField] [Range(0f, 1f)] private float tickVolume = 0.5f;
    [Tooltip("Shared volume for character / ability voice clips (not map BGM).")]
    [SerializeField] [Range(0f, 1f)] private float voiceVolume = 0.45f;
    [Tooltip("Extra scale for fire ambient under voiceVolume.")]
    [SerializeField] [Range(0f, 1f)] private float fireAmbientScale = 0.7f;

    private const string MusicPrefKey = "MusicVolume";
    private const string SfxPrefKey = "SfxVolume";

    [Header("Idle Clucks")]
    [SerializeField] private float idleCluckMinInterval = 2.5f;
    [SerializeField] private float idleCluckMaxInterval = 5.5f;
    [SerializeField] private bool playIdleClucks = true;

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
            RefreshVoiceLoopVolumes();
        }
    }

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource mindLoopSource;
    private AudioSource fireLoopSource;

    private int mindPulseCount;
    private int fireChickenCount;
    private bool idleLoopStarted;
    private bool clipsResolved;

    private float VoicePlayVolume => voiceVolume * sfxVolume;
    private float FireLoopVolume => VoicePlayVolume * fireAmbientScale;

    /// <summary>Creates a GameAudio if the scene has none (e.g. Home without a wired object).</summary>
    public static GameAudio EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("GameAudio");
        return go.AddComponent<GameAudio>();
    }

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

        mindLoopSource = gameObject.AddComponent<AudioSource>();
        mindLoopSource.playOnAwake = false;
        mindLoopSource.loop = true;
        mindLoopSource.spatialBlend = 0f;

        fireLoopSource = gameObject.AddComponent<AudioSource>();
        fireLoopSource.playOnAwake = false;
        fireLoopSource.loop = true;
        fireLoopSource.spatialBlend = 0f;

        ResolveClipsFromResources();
    }

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
        ResolveClipsFromResources();
        PlayBgm();
        if (playIdleClucks && !idleLoopStarted)
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

    /// <summary>
    /// Loads voice clips + map BGM from Resources/Music. Overrides map BGM by scene
    /// so Dusk/Graveyard/Home never keep a stale Farm loop from an unsynced scene.
    /// </summary>
    private void ResolveClipsFromResources()
    {
        alienPower = LoadMusic("Alien", alienPower);
        laserFire = LoadMusic("Laser", laserFire);
        zombieSpawn = LoadMusic("Zombie", zombieSpawn);
        mindHypnosis = LoadMusic("Hypnotic_BG", mindHypnosis);
        fireAmbient = LoadMusic("Fire", fireAmbient);

        string scene = SceneManager.GetActiveScene().name;
        if (scene == "HomeScene")
        {
            AudioClip home = LoadMusic("HomeJazz", null);
            if (home != null)
                bgm = home;
            playIdleClucks = false;
        }
        else if (scene == "World2" || GameMode.CurrentMapId == GameMode.DuskId)
        {
            AudioClip night = LoadMusic("OST_NIGHT", null);
            if (night != null)
                bgm = night;
        }
        else if (scene == "World3" || GameMode.CurrentMapId == GameMode.GraveyardId)
        {
            AudioClip grave = LoadMusic("OST_GRAVEYARD", null);
            if (grave != null)
                bgm = grave;
        }

        clipsResolved = true;
    }

    private static AudioClip LoadMusic(string resourceName, AudioClip fallback)
    {
        AudioClip loaded = Resources.Load<AudioClip>("Music/" + resourceName);
        return loaded != null ? loaded : fallback;
    }

    public void PlayBgm()
    {
        if (!clipsResolved)
            ResolveClipsFromResources();

        if (bgm == null || bgmSource == null)
            return;

        if (bgmSource.clip != bgm)
        {
            bgmSource.Stop();
            bgmSource.clip = bgm;
        }

        bgmSource.volume = bgmVolume;
        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void PlayGrab() => PlaySfx(grab, sfxVolume);
    public void PlayDrop() => PlaySfx(drop, sfxVolume);
    public void PlayExplosion() => PlaySfx(explosion, sfxVolume);
    public void PlayStep() => PlaySfx(step, stepVolume * sfxVolume);
    public void PlayChickenIdle() => PlaySfx(chickenIdle, idleCluckVolume * sfxVolume, Random.Range(0.92f, 1.08f));

    public void PlayAlienPower()
    {
        EnsureVoiceClip(ref alienPower, "Alien");
        PlaySfx(alienPower, VoicePlayVolume);
    }

    public void PlayLaserFire()
    {
        EnsureVoiceClip(ref laserFire, "Laser");
        PlaySfx(laserFire, VoicePlayVolume);
    }

    public void PlayZombieSpawn()
    {
        EnsureVoiceClip(ref zombieSpawn, "Zombie");
        PlaySfx(zombieSpawn, VoicePlayVolume);
    }

    public void NotifyMindPulseStarted()
    {
        mindPulseCount++;
        if (mindPulseCount == 1)
        {
            EnsureVoiceClip(ref mindHypnosis, "Hypnotic_BG");
            StartVoiceLoop(mindLoopSource, mindHypnosis, VoicePlayVolume);
        }
    }

    public void NotifyMindPulseEnded()
    {
        mindPulseCount = Mathf.Max(0, mindPulseCount - 1);
        if (mindPulseCount == 0)
            StopVoiceLoop(mindLoopSource);
    }

    public void NotifyFireChickenSpawned()
    {
        fireChickenCount++;
        if (fireChickenCount == 1)
        {
            EnsureVoiceClip(ref fireAmbient, "Fire");
            StartVoiceLoop(fireLoopSource, fireAmbient, FireLoopVolume);
        }
    }

    public void NotifyFireChickenDespawned()
    {
        fireChickenCount = Mathf.Max(0, fireChickenCount - 1);
        if (fireChickenCount == 0)
            StopVoiceLoop(fireLoopSource);
    }

    private void EnsureVoiceClip(ref AudioClip clip, string resourceName)
    {
        if (clip != null)
            return;
        clip = Resources.Load<AudioClip>("Music/" + resourceName);
    }

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

    private void StartVoiceLoop(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        source.clip = clip;
        source.volume = volume;
        if (!source.isPlaying)
            source.Play();
    }

    private void StopVoiceLoop(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
    }

    private void RefreshVoiceLoopVolumes()
    {
        if (mindLoopSource != null && mindLoopSource.isPlaying)
            mindLoopSource.volume = VoicePlayVolume;
        if (fireLoopSource != null && fireLoopSource.isPlaying)
            fireLoopSource.volume = FireLoopVolume;
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
