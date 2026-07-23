using System.Collections;
using TMPro;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float timer;
    public TextMeshPro text;
    public GameObject explosion;

    [Header("Blast")]
    [Tooltip("Kill radius in world units. Explosion sprites are 64px @ 16 PPU (4 units across), so ~2 matches the visible blast.")]
    [SerializeField] private float explosionRadius = 2f;

    private bool dead = false;

    private void Update()
    {
        if (timer > 0)
        {
            text.text = Mathf.RoundToInt(timer).ToString();
            timer -= Time.deltaTime;
        }

        if (timer <= 0 && !dead)
        {
            dead = true;
            StartCoroutine(Spawn());

            // Hide the bomb chicken visuals while the blast plays.
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.enabled = false;

            if (text != null)
                text.gameObject.SetActive(false);

            ChickenWander wander = GetComponent<ChickenWander>();
            if (wander != null)
                wander.enabled = false;
        }
    }

    private IEnumerator Spawn()
    {
        Vector2 origin = transform.position;
        GameObject spawnS = Instantiate(explosion, origin, Quaternion.identity);

        KillChickensInRadius(origin);

        yield return new WaitForSeconds(0.7f);
        Destroy(spawnS);
        Destroy(gameObject);
    }

    private void KillChickensInRadius(Vector2 origin)
    {
        // Distance check (not collider overlap) so grabbed chickens with disabled colliders still die.
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>(FindObjectsSortMode.None);
        float radiusSq = explosionRadius * explosionRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            // Keep this bomb alive until the VFX finishes, then destroy it in Spawn().
            if (chicken.gameObject == gameObject)
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
