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
    [Tooltip("Full length of the Electric VFX clip (~0.68s).")]
    [SerializeField] private float strikeVfxDuration = 0.68f;
    [Tooltip("When damage applies, measured from strike start (near end of the lightning anim).")]
    [SerializeField] private float damageDelay = 0.55f;

    private bool striking;

    /// <summary>True while this chicken is mid lightning strike.</summary>
    public bool IsStriking => striking;

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (striking)
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
        if (striking)
            return;

        striking = true;
        StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        Vector2 origin = transform.position;
        if (electricStrike != null)
        {
            GameObject vfx = Instantiate(electricStrike, origin, Quaternion.identity);
            Destroy(vfx, strikeVfxDuration);
        }

        if (source != null)
            source.GenerateImpulse();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        float delay = Mathf.Clamp(damageDelay, 0f, strikeVfxDuration);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ApplyStrike(origin);

        float remaining = Mathf.Max(0f, strikeVfxDuration - delay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        // Survive and arm the next strike.
        if (this == null)
            yield break;

        ResetTimer();
        striking = false;
    }

    private void ResetTimer()
    {
        timer = Random.Range(4, 9);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = Mathf.RoundToInt(timer).ToString();
        }
    }

    private void ApplyStrike(Vector2 origin)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>(FindObjectsSortMode.None);
        float radiusSq = strikeRadius * strikeRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            // Don't kill yourself.
            if (chicken.gameObject == gameObject)
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

            // Other electrics (and everything else in range) die.
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
