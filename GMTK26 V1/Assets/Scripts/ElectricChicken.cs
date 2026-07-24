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

    private bool dead;

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
            Strike();
    }

    private void Strike()
    {
        if (dead)
            return;

        dead = true;
        StartCoroutine(StrikeRoutine());

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        if (text != null)
            text.gameObject.SetActive(false);

        ChickenWander wander = GetComponent<ChickenWander>();
        if (wander != null)
            wander.enabled = false;
    }

    private IEnumerator StrikeRoutine()
    {
        Vector2 origin = transform.position;
        GameObject vfx = null;
        if (electricStrike != null)
            vfx = Instantiate(electricStrike, origin, Quaternion.identity);

        if (source != null)
            source.GenerateImpulse();

        // Wait until the lightning anim is almost finished, then apply damage.
        float delay = Mathf.Clamp(damageDelay, 0f, strikeVfxDuration);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ApplyStrike(origin);

        float remaining = Mathf.Max(0f, strikeVfxDuration - delay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (vfx != null)
            Destroy(vfx);
        Destroy(gameObject);
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

            if (chicken.gameObject == gameObject)
                continue;

            // Electric chickens are immune to lightning.
            if (chicken.GetComponent<ElectricChicken>() != null)
                continue;

            Vector2 toChicken = (Vector2)chicken.transform.position - origin;
            if (toChicken.sqrMagnitude > radiusSq)
                continue;

            Bomb bomb = chicken.GetComponent<Bomb>();
            if (bomb != null)
            {
                // Bomb chickens explode instead of silently dying.
                bomb.Detonate();
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
