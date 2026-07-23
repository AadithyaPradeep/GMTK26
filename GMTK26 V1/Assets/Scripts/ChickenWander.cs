using System.Collections;
using UnityEngine;

public class ChickenWander : MonoBehaviour
{
    [Header("Wander Area")]
    [SerializeField] private Vector2 areaMin = new Vector2(-7.5f, -4.5f);
    [SerializeField] private Vector2 areaMax = new Vector2(7.5f, 4.5f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float arrivalThreshold = 0.05f;

    [Header("Idle")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    [Header("Flee From Farmer")]
    public Transform farmerTransform;
    public float fleeDistance = 3f;
    public float fleeSpeedMultiplier = 2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 targetPosition;

    private bool isFleeing;
    private Coroutine wanderCoroutine;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    public void SetWanderArea(Vector2 min, Vector2 max)
    {
        areaMin = min;
        areaMax = max;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        wanderCoroutine = StartCoroutine(WanderLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        wanderCoroutine = null;
        isFleeing = false;
        if (animator != null)
            animator.SetBool(IsMovingHash, false);
    }

    private void Update()
    {
        if (farmerTransform == null)
        {
            if (isFleeing)
                EndFlee();
            return;
        }

        float distance = Vector2.Distance(transform.position, farmerTransform.position);
        bool shouldFlee = distance <= fleeDistance;

        if (shouldFlee)
        {
            if (!isFleeing)
                BeginFlee();

            MoveAwayFromFarmer();
        }
        else if (isFleeing)
        {
            EndFlee();
        }
    }

    private void BeginFlee()
    {
        isFleeing = true;
        animator.SetBool(IsMovingHash, true);
    }

    private void EndFlee()
    {
        isFleeing = false;
        animator.SetBool(IsMovingHash, false);
        // WanderLoop is still running and will pick a fresh target once isFleeing clears.
    }

    private void MoveAwayFromFarmer()
    {
        Vector2 current = transform.position;
        Vector2 farmerPos = farmerTransform.position;
        float step = moveSpeed * fleeSpeedMultiplier * Time.deltaTime;

        Vector2 next = GetEdgeAwareFleePosition(current, farmerPos, step);

        float deltaX = next.x - current.x;
        if (deltaX != 0f)
            spriteRenderer.flipX = deltaX < 0f;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.flipX = spriteRenderer.flipX;
        }

        transform.position = next;
    }

    /// <summary>
    /// Flee away from the farmer, staying inside the wander area.
    /// At edges/corners, slides along the boundary in the direction that
    /// increases distance from the farmer instead of getting stuck.
    /// </summary>
    private Vector2 GetEdgeAwareFleePosition(Vector2 current, Vector2 farmerPos, float step)
    {
        Vector2 away = current - farmerPos;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector2.right;
        else
            away.Normalize();

        Vector2 proposed = current + away * step;
        Vector2 clamped = ClampToArea(proposed);

        // Free space — pure flee worked.
        if ((clamped - proposed).sqrMagnitude < 0.0001f)
            return clamped;

        // Hit a bound: build a slide direction on free axes, away from the farmer.
        bool blockedX = Mathf.Abs(clamped.x - proposed.x) > 0.0001f;
        bool blockedY = Mathf.Abs(clamped.y - proposed.y) > 0.0001f;

        Vector2 slide = Vector2.zero;

        if (blockedX && !blockedY)
        {
            // Vertical wall — slide on Y toward greater distance from farmer.
            slide.y = Mathf.Sign(current.y - farmerPos.y);
            if (Mathf.Abs(slide.y) < 0.01f)
                slide.y = away.y >= 0f ? 1f : -1f;
        }
        else if (blockedY && !blockedX)
        {
            // Horizontal wall — slide on X toward greater distance from farmer.
            slide.x = Mathf.Sign(current.x - farmerPos.x);
            if (Mathf.Abs(slide.x) < 0.01f)
                slide.x = away.x >= 0f ? 1f : -1f;
        }
        else
        {
            // Corner (or fully blocked): pick the cardinal move that ends farthest from farmer.
            return PickFarthestInArea(current, farmerPos, step);
        }

        return ClampToArea(current + slide.normalized * step);
    }

    private Vector2 PickFarthestInArea(Vector2 current, Vector2 farmerPos, float step)
    {
        Vector2[] dirs =
        {
            Vector2.left, Vector2.right, Vector2.up, Vector2.down
        };

        Vector2 best = current;
        float bestDist = Vector2.Distance(current, farmerPos);

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2 candidate = ClampToArea(current + dirs[i] * step);
            if ((candidate - current).sqrMagnitude < 0.00001f)
                continue;

            float dist = Vector2.Distance(candidate, farmerPos);
            if (dist > bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    private Vector2 ClampToArea(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, areaMin.x, areaMax.x);
        position.y = Mathf.Clamp(position.y, areaMin.y, areaMax.y);
        return position;
    }

    private IEnumerator WanderLoop()
    {
        while (enabled)
        {
            // Pause wander while Update owns movement for fleeing.
            while (isFleeing)
                yield return null;

            PickNewTarget();
            yield return MoveToTarget();

            if (isFleeing)
                continue;

            animator.SetBool(IsMovingHash, false);

            // Interruptible idle: check every frame so flee can start immediately.
            float idleDuration = Random.Range(minIdleTime, maxIdleTime);
            float elapsed = 0f;
            while (elapsed < idleDuration && !isFleeing)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    public void Highlight()
    {
        animator.SetBool("Highlight", true);
    }
    public void NoHighlight()
    {
        animator.SetBool("Highlight", false);
    }
    private void PickNewTarget()
    {
        targetPosition = new Vector2(
            Random.Range(areaMin.x, areaMax.x),
            Random.Range(areaMin.y, areaMax.y)
        );
    }

    private IEnumerator MoveToTarget()
    {
        animator.SetBool(IsMovingHash, true);

        while (!isFleeing &&
               Vector2.Distance(transform.position, targetPosition) > arrivalThreshold)
        {
            Vector2 current = transform.position;
            Vector2 next = Vector2.MoveTowards(current, targetPosition, moveSpeed * Time.deltaTime);

            float deltaX = next.x - current.x;
            if (deltaX != 0f)
                spriteRenderer.flipX = deltaX < 0f;


            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.flipX = spriteRenderer.flipX;
            }

            transform.position = next;
            yield return null;
        }

        if (!isFleeing)
            transform.position = targetPosition;
    }
}
