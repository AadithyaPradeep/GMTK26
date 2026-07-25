using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Curving homing rocket that follows a chicken target.
/// Explodes on contact, lifetime expiry, or laser hit.
/// </summary>
public class BossMissile : MonoBehaviour
{
    private static readonly List<BossMissile> Active = new List<BossMissile>();

    [SerializeField] private float speed = 5f;
    [SerializeField] private float turnRateDegrees = 220f;
    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float armDelay = 0.1f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float blastRadius = 1.35f;
    [SerializeField] private float explosionVfxDuration = 0.7f;
    [SerializeField] private GameObject explosionPrefab;

    private Transform target;
    private Vector2 direction;
    private float age;
    private bool armed;
    private bool exploded;

    public static IReadOnlyList<BossMissile> ActiveMissiles => Active;

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public void Launch(Transform targetTransform)
    {
        target = targetTransform;
        age = 0f;
        armed = armDelay <= 0f;
        exploded = false;

        Vector2 from = transform.position;
        if (target != null)
        {
            Vector2 to = (Vector2)target.position - from;
            direction = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
        }
        else
        {
            direction = Vector2.right;
        }

        ApplyRotation();
    }

    public void Launch(Vector2 lockedAimPoint)
    {
        // Fallback: create a dummy aim by flying toward the point without a live target.
        target = null;
        age = 0f;
        armed = armDelay <= 0f;
        exploded = false;

        Vector2 from = transform.position;
        Vector2 to = lockedAimPoint - from;
        direction = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
        ApplyRotation();
    }

    public void ConfigureExplosion(GameObject prefab)
    {
        if (prefab != null)
            explosionPrefab = prefab;
    }

    private void Update()
    {
        if (exploded)
            return;

        age += Time.deltaTime;
        if (!armed && age >= armDelay)
            armed = true;

        if (age >= lifetime)
        {
            Explode();
            return;
        }

        if (target == null)
        {
            Explode();
            return;
        }

        Vector2 pos = transform.position;
        Vector2 toTarget = (Vector2)target.position - pos;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector2 desired = toTarget.normalized;
            float maxRad = turnRateDegrees * Mathf.Deg2Rad * Time.deltaTime;
            direction = Vector3.RotateTowards(direction, desired, maxRad, 0f);
            direction.Normalize();
            ApplyRotation();
        }

        Vector2 next = pos + direction * (speed * Time.deltaTime);
        transform.position = next;

        if (!armed)
            return;

        if (toTarget.sqrMagnitude <= hitRadius * hitRadius)
            Explode();
        else if (TryHitChickenNear(next))
            Explode();
    }

    private void ApplyRotation()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool TryHitChickenNear(Vector2 pos)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float hitSq = hitRadius * hitRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;
            if (!CanDamage(chicken.gameObject))
                continue;

            if (((Vector2)chicken.transform.position - pos).sqrMagnitude <= hitSq)
                return true;
        }

        return false;
    }

    public void Explode()
    {
        if (exploded)
            return;

        exploded = true;
        Vector2 origin = transform.position;

        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, origin, Quaternion.identity);
            Destroy(fx, explosionVfxDuration);
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayExplosion();

        ApplyBlast(origin);
        Destroy(gameObject);
    }

    private void ApplyBlast(Vector2 origin)
    {
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float radiusSq = blastRadius * blastRadius;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;
            if (!CanDamage(chicken.gameObject))
                continue;

            if (((Vector2)chicken.transform.position - origin).sqrMagnitude > radiusSq)
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

    private static bool CanDamage(GameObject go)
    {
        if (go == null)
            return false;
        if (go.GetComponent<BossChicken>() != null)
            return false;

        LaserChicken laser = go.GetComponent<LaserChicken>();
        if (laser != null && laser.IsImmune)
            return false;

        return true;
    }
}
