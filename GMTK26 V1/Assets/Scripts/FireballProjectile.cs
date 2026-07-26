using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Moves a fireball along a facing direction; kills on full-body contact or end blast.
/// </summary>
public class FireballProjectile : MonoBehaviour
{
    private Vector2 origin;
    private Vector2 direction;
    private float maxDistance;
    private float speed;
    private GameObject owner;
    private GameObject explosionPrefab;
    private float explosionRadius;
    private float explosionVfxDuration;
    private float bodyHitRadius;
    private CinemachineImpulseSource impulseSource;
    private System.Action onFinished;

    private float traveled;
    private bool dead;
    private SpriteRenderer spriteRenderer;

    public void Launch(
        Vector2 launchOrigin,
        Vector2 launchDirection,
        float travelDistance,
        float moveSpeed,
        GameObject ownerChicken,
        GameObject explosion,
        float blastRadius,
        float blastVfxDuration,
        float hitRadius,
        CinemachineImpulseSource impulse,
        System.Action finishedCallback)
    {
        origin = launchOrigin;
        direction = launchDirection.normalized;
        maxDistance = Mathf.Max(0.5f, travelDistance);
        speed = Mathf.Max(0.5f, moveSpeed);
        owner = ownerChicken;
        explosionPrefab = explosion;
        explosionRadius = Mathf.Max(0.1f, blastRadius);
        explosionVfxDuration = Mathf.Max(0.05f, blastVfxDuration);
        bodyHitRadius = Mathf.Max(0.05f, hitRadius);
        impulseSource = impulse;
        onFinished = finishedCallback;

        transform.position = origin;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0f;
    }

    private void Update()
    {
        if (dead)
            return;

        float step = speed * Time.deltaTime;
        Vector2 pos = transform.position;
        Vector2 next = pos + direction * step;
        transform.position = next;
        traveled += step;

        if (TryHitAnyChicken(next, out ChickenWander hit))
        {
            ExplodeAt(hit != null ? (Vector2)hit.transform.position : next, hit);
            return;
        }

        if (traveled >= maxDistance)
            ExplodeAt(next, null);
    }

    private bool TryHitAnyChicken(Vector2 point, out ChickenWander hit)
    {
        hit = null;
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float best = float.MaxValue;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;
            if (owner != null && chicken.gameObject == owner)
                continue;
            if (chicken.GetComponent<FireChicken>() != null)
                continue;
            if (chicken.GetComponent<BossChicken>() != null)
                continue;

            if (GhostChicken.IsProtected(chicken))
                continue;

            LaserChicken laser = chicken.GetComponent<LaserChicken>();
            if (laser != null && laser.IsImmune)
                continue;

            if (!OverlapsBody(chicken.gameObject, point))
                continue;

            float dist = Vector2.Distance(point, chicken.transform.position);
            if (dist < best)
            {
                best = dist;
                hit = chicken;
            }
        }

        return hit != null;
    }

    private bool OverlapsBody(GameObject target, Vector2 point)
    {
        Collider2D col = target.GetComponent<Collider2D>();
        if (col != null && col.enabled)
        {
            Vector2 closest = col.ClosestPoint(point);
            return Vector2.Distance(closest, point) <= bodyHitRadius;
        }

        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Bounds b = sr.bounds;
            Vector2 closest = new Vector2(
                Mathf.Clamp(point.x, b.min.x, b.max.x),
                Mathf.Clamp(point.y, b.min.y, b.max.y));
            return Vector2.Distance(closest, point) <= bodyHitRadius;
        }

        return Vector2.Distance(point, target.transform.position) <= bodyHitRadius;
    }

    private void ExplodeAt(Vector2 blastOrigin, ChickenWander directHit)
    {
        if (dead)
            return;

        dead = true;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        if (impulseSource != null)
            impulseSource.GenerateImpulse();

        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, blastOrigin, Quaternion.identity);
            Destroy(fx, explosionVfxDuration);
        }

        // Direct body hit always kills / detonates that target first.
        if (directHit != null)
            ApplyHit(directHit);

        KillInRadius(blastOrigin);

        onFinished?.Invoke();
        onFinished = null;

        Destroy(gameObject);
    }

    private void ApplyHit(ChickenWander chicken)
    {
        if (chicken == null)
            return;

        if (GhostChicken.IsProtected(chicken))
            return;

        // Fireballs never harm fire chickens (self or same kind).
        if (chicken.GetComponent<FireChicken>() != null)
            return;

        Bomb bomb = chicken.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.Detonate();
            return;
        }

        // Electrics only die to bomb explosions.
        if (chicken.GetComponent<ElectricChicken>() != null)
            return;

        Destroy(chicken.gameObject);
    }

    private void KillInRadius(Vector2 blastOrigin)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float radiusSq = explosionRadius * explosionRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;

            if (owner != null && chicken.gameObject == owner)
                continue;

            if (chicken.GetComponent<BossChicken>() != null)
                continue;

            if (GhostChicken.IsProtected(chicken))
                continue;

            LaserChicken laser = chicken.GetComponent<LaserChicken>();
            if (laser != null && laser.IsImmune)
                continue;

            // Electrics only die to bomb explosions.
            if (chicken.GetComponent<ElectricChicken>() != null)
                continue;

            // Fire chickens don't kill other fire chickens via splash (mirror bomb-vs-bomb).
            if (chicken.GetComponent<FireChicken>() != null)
                continue;

            Vector2 toChicken = (Vector2)chicken.transform.position - blastOrigin;
            if (toChicken.sqrMagnitude > radiusSq)
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
}
