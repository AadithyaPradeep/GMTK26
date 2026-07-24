using System.Collections;
using UnityEngine;

/// <summary>
/// Central audio hub for BGM and one-shot SFX.
/// </summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

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

    [Header("Idle Clucks")]
    [SerializeField] private float idleCluckMinInterval = 2.5f;
    [SerializeField] private float idleCluckMaxInterval = 5.5f;

    public AudioClip BombTickClip => bombTick;
    public float TickVolume => tickVolume;

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

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void Start()
    {
        PlayBgm();
        StartCoroutine(IdleCluckLoop());
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
    public void PlayStep() => PlaySfx(step, stepVolume);
    public void PlayChickenIdle() => PlaySfx(chickenIdle, idleCluckVolume, Random.Range(0.92f, 1.08f));

    public AudioSource CreateTickSource(GameObject host)
    {
        if (bombTick == null || host == null)
            return null;

        AudioSource source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = bombTick;
        source.volume = tickVolume;
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
            yield return new WaitForSeconds(wait);

            if (chickenIdle == null)
                continue;

            // Soft ambient cluck so the farm feels alive.
            PlayChickenIdle();
        }
    }
}
