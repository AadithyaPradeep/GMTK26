using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Moves an arrow in a direction; kills on body contact with blue hit VFX (no splash).
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    private Vector2 origin;
    private Vector2 direction;
    private float maxDistance;
    private float speed;
    private GameObject owner;
    private GameObject hitExplosionPrefab;
    private float hitVfxDuration;
    private float bodyHitRadius;
    private CinemachineImpulseSource impulseSource;

    private float traveled;
    private bool dead;

    public void Launch(
        Vector2 launchOrigin,
        Vector2 launchDirection,
        float travelDistance,
        float moveSpeed,
        GameObject ownerChicken,
        GameObject hitExplosion,
        float blastVfxDuration,
        float hitRadius,
        CinemachineImpulseSource impulse)
    {
        origin = launchOrigin;
        direction = launchDirection.normalized;
        maxDistance = Mathf.Max(0.5f, travelDistance);
        speed = Mathf.Max(0.5f, moveSpeed);
        owner = ownerChicken;
        hitExplosionPrefab = hitExplosion;
        hitVfxDuration = Mathf.Max(0.05f, blastVfxDuration);
        bodyHitRadius = Mathf.Max(0.05f, hitRadius);
        impulseSource = impulse;

        transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
            HitTarget(hit != null ? (Vector2)hit.transform.position : next, hit);
            return;
        }

        if (traveled >= maxDistance)
            Miss();
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
            if (chicken.GetComponent<SkeleCluck>() != null)
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

    private void HitTarget(Vector2 hitOrigin, ChickenWander directHit)
    {
        if (dead)
            return;

        dead = true;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        if (impulseSource != null)
            impulseSource.GenerateImpulse();

        if (hitExplosionPrefab != null)
        {
            GameObject fx = Instantiate(hitExplosionPrefab, hitOrigin, Quaternion.identity);
            Destroy(fx, hitVfxDuration);
        }

        if (directHit != null)
            ApplyHit(directHit);

        Destroy(gameObject);
    }

    private void Miss()
    {
        if (dead)
            return;

        dead = true;
        Destroy(gameObject);
    }

    private void ApplyHit(ChickenWander chicken)
    {
        if (chicken == null)
            return;

        if (GhostChicken.IsProtected(chicken))
            return;

        // Arrows never harm skeleton chickens (self or same kind).
        if (chicken.GetComponent<SkeleCluck>() != null)
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
}
