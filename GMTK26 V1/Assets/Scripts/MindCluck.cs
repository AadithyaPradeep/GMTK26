using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mind Cluck: periodically pulses a short attract aura in a small radius.
/// Other chickens are pulled in only while a pulse is active.
/// </summary>
public class MindCluck : MonoBehaviour
{
    [Header("Attract Pulse")]
    [SerializeField] private float attractRadius = 3f;
    [SerializeField] private float pulseDuration = 2f;
    [SerializeField] private float minCooldown = 3.5f;
    [SerializeField] private float maxCooldown = 6f;

    [Header("Optional VFX")]
    [Tooltip("If set, enabled only while pulsing (e.g. MC Eff child).")]
    [SerializeField] private GameObject pulseEffect;
    public GameObject radEff;

    private static readonly List<MindCluck> Active = new List<MindCluck>();

    public bool IsPulsing { get; private set; }
    public float AttractRadius => attractRadius;

    private void Awake()
    {
        if (pulseEffect == null)
        {
            Transform eff = transform.Find("MC Eff");
            Transform reff = transform.Find("Radius");
            if (eff != null)
            {
                pulseEffect = eff.gameObject;
                radEff = reff.gameObject;
            }
        }

        if (pulseEffect != null)
        {
            pulseEffect.SetActive(false);
            radEff.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);

        StartCoroutine(PulseLoop());
    }

    private void OnDisable()
    {
        Active.Remove(this);
        IsPulsing = false;
        if (pulseEffect != null)
        {
            pulseEffect.SetActive(false);
            radEff.SetActive(false);
        }
        StopAllCoroutines();
    }

    private IEnumerator PulseLoop()
    {
        while (enabled)
        {
            float cooldown = Random.Range(minCooldown, maxCooldown);
            yield return new WaitForSeconds(cooldown);

            IsPulsing = true;
            if (pulseEffect != null)
            {
                pulseEffect.SetActive(true);
                radEff.SetActive(true);
            }

            yield return new WaitForSeconds(pulseDuration);

            IsPulsing = false;
            if (pulseEffect != null)
            {
                pulseEffect.SetActive(false);
                radEff.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Nearest Mind Cluck that is currently pulsing and within its attract radius.
    /// </summary>
    public static bool TryGetAttracting(Vector2 from, Transform self, out MindCluck nearest)
    {
        nearest = null;
        float bestDistSq = float.MaxValue;

        for (int i = Active.Count - 1; i >= 0; i--)
        {
            MindCluck mind = Active[i];
            if (mind == null)
            {
                Active.RemoveAt(i);
                continue;
            }

            if (!mind.isActiveAndEnabled || !mind.IsPulsing)
                continue;

            if (self != null && mind.transform == self)
                continue;

            float radius = mind.attractRadius;
            float distSq = ((Vector2)mind.transform.position - from).sqrMagnitude;
            if (distSq > radius * radius)
                continue;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                nearest = mind;
            }
        }

        return nearest != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsPulsing
            ? new Color(0.6f, 0.2f, 1f, 0.85f)
            : new Color(0.6f, 0.2f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
#endif
}
