using System.Collections;
using UnityEngine;

/// <summary>
/// Beam-only hazard used by the boss special attack (no chicken sprite).
/// </summary>
public class BossSideBeam : MonoBehaviour
{
    [SerializeField] private float nativeBeamLength = 8f;
    [SerializeField] private float halfThickness = 0.45f;
    [SerializeField] private float duration = 5f;

    private SpriteRenderer beamRenderer;
    private Vector2 direction;
    private bool facingLeft;
    private bool running;

    public void Begin(GameObject laserBeamPrefab, bool faceLeft, float beamDuration)
    {
        facingLeft = faceLeft;
        direction = faceLeft ? Vector2.left : Vector2.right;
        duration = Mathf.Max(0.1f, beamDuration);

        if (laserBeamPrefab != null)
        {
            GameObject beam = Instantiate(laserBeamPrefab, transform);
            beam.transform.localPosition = Vector3.zero;
            beam.transform.localRotation = Quaternion.identity;
            beamRenderer = beam.GetComponent<SpriteRenderer>();
            if (beamRenderer != null)
                beamRenderer.flipX = faceLeft;
        }

        running = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplyDamage();
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyDamage()
    {
        Vector2 origin = transform.position;

        // Detonate missiles in the beam.
        for (int i = BossMissile.ActiveMissiles.Count - 1; i >= 0; i--)
        {
            BossMissile missile = BossMissile.ActiveMissiles[i];
            if (missile == null)
                continue;
            if (IsInBeam(origin, missile.transform.position))
                missile.Explode();
        }

        // Can destroy the player's held laser chicken.
        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander chicken = chickens[i];
            if (chicken == null)
                continue;
            if (chicken.GetComponent<BossChicken>() != null)
                continue;

            LaserChicken laser = chicken.GetComponent<LaserChicken>();
            if (laser == null)
                continue;
            // Only threaten the held player laser (ignore other immune/side leftovers).
            if (!laser.IsHeld)
                continue;
            if (!IsInBeam(origin, chicken.transform.position))
                continue;

            Destroy(chicken.gameObject);
        }
    }

    private bool IsInBeam(Vector2 origin, Vector2 point)
    {
        Vector2 toPoint = point - origin;
        float along = Vector2.Dot(toPoint, direction);
        if (along < 0f || along > nativeBeamLength)
            return false;

        Vector2 perp = new Vector2(-direction.y, direction.x);
        float across = Mathf.Abs(Vector2.Dot(toPoint, perp));
        return across <= halfThickness;
    }
}
