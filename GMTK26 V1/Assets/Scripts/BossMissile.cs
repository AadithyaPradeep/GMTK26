using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Missile that fans out in different directions, then curves toward the player.
/// Explodes on lifetime, hit, or laser strike.
/// </summary>
public class BossMissile : MonoBehaviour
{
    private static readonly List<BossMissile> Active = new List<BossMissile>();

    [SerializeField] private float speed = 2.6f;
    [SerializeField] private float turnRateDegrees = 140f;
    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float armDelay = 0.12f;
    [SerializeField] private float lifetime = 5.5f;
    [SerializeField] private float blastRadius = 1.2f;
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

    /// <summary>
    /// Starts flying at an angled direction, then homes toward the target.
    /// </summary>
    public void LaunchCurving(Transform homeTarget, float initialAngleOffsetDegrees)
    {
        target = homeTarget;
        age = 0f;
        armed = armDelay <= 0f;
        exploded = false;

        Vector2 from = transform.position;
        Vector2 toTarget = target != null
            ? (Vector2)target.position - from
            : Vector2.left;

        if (toTarget.sqrMagnitude < 0.0001f)
            toTarget = Vector2.left;

        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg + initialAngleOffsetDegrees;
        float rad = angle * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        ApplyRotation();
    }

    public void LaunchStraight(Vector2 aimPoint, float inaccuracyDegrees)
    {
        age = 0f;
        armed = armDelay <= 0f;
        exploded = false;
        target = null;

        Vector2 from = transform.position;
        Vector2 to = aimPoint - from;
        if (to.sqrMagnitude < 0.0001f)
            to = Vector2.left;

        float angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        angle += Random.Range(-inaccuracyDegrees, inaccuracyDegrees);
        float rad = angle * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        ApplyRotation();
    }

    public void Launch(Transform targetTransform)
    {
        LaunchCurving(targetTransform, Random.Range(-40f, 40f));
    }

    public void Launch(Vector2 lockedAimPoint)
    {
        LaunchStraight(lockedAimPoint, 12f);
    }

    public void ConfigureExplosion(GameObject prefab)
    {
        if (prefab != null)
            explosionPrefab = prefab;
    }

    public void ConfigureFlight(float moveSpeed, float life, float turnRate = -1f)
    {
        if (moveSpeed > 0f)
            speed = moveSpeed;
        if (life > 0f)
            lifetime = life;
        if (turnRate > 0f)
            turnRateDegrees = turnRate;
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

        Vector2 pos = transform.position;

        if (target != null)
        {
            Vector2 toTarget = (Vector2)target.position - pos;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector2 desired = toTarget.normalized;
                float maxRad = turnRateDegrees * Mathf.Deg2Rad * Time.deltaTime;
                direction = Vector3.RotateTowards(direction, desired, maxRad, 0f);
                direction.Normalize();
                ApplyRotation();
            }
        }

        Vector2 next = pos + direction * (speed * Time.deltaTime);
        transform.position = next;

        if (!armed)
            return;

        if (target != null && ((Vector2)target.position - next).sqrMagnitude <= hitRadius * hitRadius)
        {
            Explode();
            return;
        }

        if (TryHitChickenNear(next))
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

            // Electrics only die to bomb explosions.
            if (chicken.GetComponent<ElectricChicken>() != null)
                continue;

            Destroy(chicken.gameObject);
        }
    }

    private static bool CanDamage(GameObject go)
    {
        if (go == null)
            return false;
        if (go.GetComponent<BossChicken>() != null)
            return false;

        if (GhostChicken.IsProtected(go))
            return false;

        LaserChicken laser = go.GetComponent<LaserChicken>();
        // Held player laser can be destroyed by missiles (protect it!).
        if (laser != null && laser.IsImmune && !laser.IsHeld)
            return false;

        return true;
    }
}
