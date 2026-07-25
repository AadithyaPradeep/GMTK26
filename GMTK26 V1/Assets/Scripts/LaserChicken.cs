using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Countdown chicken that fires a continuous eye laser for several seconds.
/// The beam is parented to the chicken so it follows movement / facing.
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
    [SerializeField] private float laserDuration = 5f;

    private SpriteRenderer spriteRenderer;
    private ChickenWander wander;
    private GameObject activeBeam;
    private SpriteRenderer beamRenderer;
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
        DestroyActiveBeam();

        if (laserPrefab != null)
        {
            activeBeam = Instantiate(laserPrefab, transform);
            activeBeam.transform.localPosition = Vector3.zero;
            activeBeam.transform.localRotation = Quaternion.identity;
            // Leave prefab scale untouched.
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

        ResetTimer();
        firing = false;
    }

    private void UpdateBeamFacing()
    {
        if (activeBeam == null)
            return;

        activeBeam.transform.localPosition = Vector3.zero;
        activeBeam.transform.localRotation = Quaternion.identity;

        // Flip the sprite only — never touch localScale.
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
        if (along < 0f || along > nativeBeamLength)
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
        Vector2 origin = transform.position;
        Vector2 end = origin + direction * nativeBeamLength;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
        Gizmos.DrawLine(origin, end);
    }
#endif
}
