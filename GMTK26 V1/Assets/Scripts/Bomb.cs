using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

public class Bomb : MonoBehaviour
{
    public float timer;
    public TextMeshPro text;
    public GameObject explosion;
    public CinemachineImpulseSource source;

    [Header("Blast")]
    [Tooltip("Kill radius in world units. Explosion sprites are 64px @ 16 PPU (4 units across), so ~2 matches the visible blast.")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionVfxDuration = 0.7f;

    private bool dead;
    private AudioSource tickSource;

    private void Start()
    {
        if (GameAudio.Instance != null)
            tickSource = GameAudio.Instance.CreateTickSource(gameObject);
    }

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
            Detonate();
    }

    /// <summary>
    /// Forces an immediate explosion (timer expiry or external trigger like an electric strike).
    /// </summary>
    public void Detonate()
    {
        if (dead)
            return;

        dead = true;

        if (tickSource != null)
        {
            tickSource.Stop();
            tickSource = null;
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        StartCoroutine(BlastRoutine());

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        if (text != null)
            text.gameObject.SetActive(false);

        ChickenWander wander = GetComponent<ChickenWander>();
        if (wander != null)
            wander.enabled = false;
    }

    private IEnumerator BlastRoutine()
    {
        Vector2 origin = transform.position;
        GameObject spawnS = null;
        if (explosion != null)
        {
            spawnS = Instantiate(explosion, origin, Quaternion.identity);
            // Survives this bomb being destroyed early in a chain reaction.
            Destroy(spawnS, explosionVfxDuration);
        }

        if (source != null)
            source.GenerateImpulse();

        KillChickensInRadius(origin);

        yield return new WaitForSeconds(explosionVfxDuration);
        Destroy(gameObject);
    }

    private void KillChickensInRadius(Vector2 origin)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>(FindObjectsSortMode.None);
        float radiusSq = explosionRadius * explosionRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            if (chicken.gameObject == gameObject)
                continue;

            // Don't wipe a lightning chicken mid-strike (that used to orphan looping VFX).
            ElectricChicken electric = chicken.GetComponent<ElectricChicken>();
            if (electric != null && electric.IsStriking)
                continue;

            Vector2 toChicken = (Vector2)chicken.transform.position - origin;
            if (toChicken.sqrMagnitude <= radiusSq)
                Destroy(chicken.gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
