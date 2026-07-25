using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Countdown chicken that fires an eye laser and kills anything in the beam path.
/// Survives the shot and re-arms (like Electric).
/// </summary>
public class LaserChicken : MonoBehaviour
{
    public float timer = 5f;
    public TextMeshPro text;
    public GameObject laserPrefab;
    public CinemachineImpulseSource source;

    [Header("Beam")]
    [Tooltip("Local eye offset when facing right (x is mirrored when flipX).")]
    [SerializeField] private Vector2 eyeOffset = new Vector2(0.35f, 0.25f);
    [Tooltip("Extra push in front of the eyes along facing direction.")]
    [SerializeField] private float beamSpawnForward = 0.12f;
    [Tooltip("World-unit length of the damaging beam.")]
    [SerializeField] private float laserLength = 12f;
    [Tooltip("Native LaserBeam sprite width in world units (128px @ 16 PPU).")]
    [SerializeField] private float nativeBeamLength = 8f;
    [SerializeField] private float laserHalfThickness = 0.45f;
    [SerializeField] private float laserDuration = 0.7f;
    [Tooltip("Extra time the beam stays visible after damage stops.")]
    [SerializeField] private float beamLinger = 0.35f;
    [SerializeField] private float damageStartDelay = 0.05f;

    private SpriteRenderer spriteRenderer;
    private ChickenWander wander;
    private GameObject activeBeam;
    private bool firing;
    private bool held;

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

    private void OnDestroy()
    {
        DestroyActiveBeam();
    }

    private void OnDisable()
    {
        // If this chicken dies / is disabled mid-shot, clear the beam.
        DestroyActiveBeam();
        firing = false;
    }

    private void Update()
    {
        if (firing)
            return;

        // Timer keeps counting while held (same idea as bombs).
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
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        if (wander != null)
            wander.enabled = false;

        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        Vector2 direction = facingLeft ? Vector2.left : Vector2.right;
        Vector2 origin = (Vector2)transform.position
            + new Vector2(facingLeft ? -eyeOffset.x : eyeOffset.x, eyeOffset.y)
            + direction * beamSpawnForward;

        DestroyActiveBeam();

        float totalBeamLife = laserDuration + Mathf.Max(0f, beamLinger);

        if (laserPrefab != null)
        {
            activeBeam = Instantiate(laserPrefab, origin, Quaternion.identity);
            float lengthScale = laserLength / Mathf.Max(0.01f, nativeBeamLength);
            activeBeam.transform.localScale = new Vector3(
                facingLeft ? -lengthScale : lengthScale,
                1f,
                1f);

            // Hard lifetime so looping LaserBeam anim can never stick around.
            Destroy(activeBeam, totalBeamLife);
        }

        if (source != null)
            source.GenerateImpulse();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        float elapsed = 0f;
        bool damageStarted = damageStartDelay <= 0f;

        while (elapsed < laserDuration)
        {
            if (this == null)
                yield break;

            elapsed += Time.deltaTime;

            if (!damageStarted && elapsed >= damageStartDelay)
                damageStarted = true;

            if (damageStarted)
                KillAlongBeam(origin, direction);

            yield return null;
        }

        // Keep the beam visible a moment after damage ends.
        if (beamLinger > 0f)
            yield return new WaitForSeconds(beamLinger);

        DestroyActiveBeam();

        if (this == null)
            yield break;

        if (wander != null && !held)
            wander.enabled = true;

        ResetTimer();
        firing = false;
    }

    private void DestroyActiveBeam()
    {
        if (activeBeam == null)
            return;

        Destroy(activeBeam);
        activeBeam = null;
    }

    private void KillAlongBeam(Vector2 origin, Vector2 direction)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            if (chicken.gameObject == gameObject)
                continue;

            if (!IsInBeam(origin, direction, chicken.transform.position))
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

    private bool IsInBeam(Vector2 origin, Vector2 direction, Vector2 point)
    {
        Vector2 toPoint = point - origin;
        float along = Vector2.Dot(toPoint, direction);
        if (along < 0f || along > laserLength)
            return false;

        Vector2 perp = new Vector2(-direction.y, direction.x);
        float across = Mathf.Abs(Vector2.Dot(toPoint, perp));
        return across <= laserHalfThickness;
    }

    private void ResetTimer()
    {
        timer = Random.Range(5, 10);
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
        Vector2 origin = (Vector2)transform.position
            + new Vector2(facingLeft ? -eyeOffset.x : eyeOffset.x, eyeOffset.y)
            + direction * beamSpawnForward;
        Vector2 end = origin + direction * laserLength;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
        Gizmos.DrawLine(origin, end);
    }
#endif
}
