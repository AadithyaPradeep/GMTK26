using System.Collections;
using System.Collections.Generic;
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

    [Header("Mind Cluck Attract")]
    [SerializeField] private float attractSpeedMultiplier = 1.25f;
    [SerializeField] private float attractStopDistance = 0.35f;

    [Header("Bomb Stay Near Normals")]
    [Tooltip("If a bomb is farther than this from every normal chicken, it moves closer.")]
    [SerializeField] private float bombMaxDistanceFromNormals = 4f;
    [SerializeField] private float bombApproachSpeedMultiplier = 1.35f;

    [Header("Panic")]
    [SerializeField] private float panicSpeedMultiplier = 2.9f;

    [Header("Boss Panic")]
    [SerializeField] private float bossPanicSpeedMultiplier = 2.2f;
    [SerializeField] private float bossHuddleRetargetTime = 0.7f;
    [SerializeField] private float leftHuddleWidth = 2.8f;

    [Header("Boss March")]
    [SerializeField] private float bossMarchSpeedMultiplier = 2.4f;
    [SerializeField] private float bossMarchYDrift = 1.6f;
    [SerializeField] private float bossMarchYRetargetTime = 0.9f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 targetPosition;

    private bool isFleeing;
    private bool isAttracted;
    private bool isApproachingNormals;
    private bool isMindCluck;
    private bool isBomb;
    private bool isPanic;
    private bool isGhost;
    private bool gravityFrozen;
    private bool bossPanic;
    private bool bossMarchLeft;
    private Transform bossTransform;
    private float bossHuddleTimer;
    private float marchYTarget;
    private float marchYTimer;
    private Coroutine wanderCoroutine;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly List<ChickenWander> All = new List<ChickenWander>();

    private float CurrentMoveSpeed
    {
        get
        {
            if (bossPanic)
            {
                float mult = isPanic
                    ? Mathf.Max(bossPanicSpeedMultiplier, panicSpeedMultiplier)
                    : bossPanicSpeedMultiplier;
                return moveSpeed * mult;
            }
            if (isPanic)
                return moveSpeed * panicSpeedMultiplier;
            return moveSpeed;
        }
    }

    public void SetWanderArea(Vector2 min, Vector2 max)
    {
        areaMin = min;
        areaMax = max;
    }

    /// <summary>Scale base move speed (World3 faster chickens, etc.).</summary>
    public void MultiplyMoveSpeed(float multiplier)
    {
        if (multiplier <= 0f)
            return;
        moveSpeed *= multiplier;
    }

    public void SetBossPanic(bool enabled, Transform boss)
    {
        bossPanic = enabled;
        bossTransform = boss;
        bossHuddleTimer = 0f;
        if (!enabled && isFleeing)
            EndFlee();
    }

    public bool IsBossHuddling => bossPanic;

    public void SetGravityFrozen(bool frozen)
    {
        gravityFrozen = frozen;
        if (!frozen)
            return;

        if (isFleeing)
            EndFlee();
        if (isAttracted)
            EndAttract();
        if (isApproachingNormals)
            EndApproachNormals();

        if (animator != null)
            animator.SetBool(IsMovingHash, false);
    }

    public bool IsGravityFrozen => gravityFrozen;

    /// <summary>Wave 6: flock chickens gather and stay on the far left.</summary>
    public static void SetBossLeftHuddleForFlock(bool enabled)
    {
        for (int i = All.Count - 1; i >= 0; i--)
        {
            ChickenWander c = All[i];
            if (c == null)
            {
                All.RemoveAt(i);
                continue;
            }

            if (!BossChicken.IsFlockTarget(c.gameObject))
                continue;

            bool already = c.bossPanic;
            c.SetBossPanic(enabled, null);
            // Snap only on first enter so we don't teleport every frame.
            if (enabled && !already)
                c.SnapToLeftHuddle();
        }
    }

    public static void SetBossScrambleForFlock(bool enabled, Transform boss)
    {
        SetBossLeftHuddleForFlock(enabled);
    }

    /// <summary>Legacy name — same as left huddle for flock.</summary>
    public static void SetBossPanicForAllNormals(bool enabled, Transform boss)
    {
        SetBossLeftHuddleForFlock(enabled);
    }

    public void RefreshTypeFlags()
    {
        isMindCluck = GetComponent<MindCluck>() != null;
        isBomb = GetComponent<Bomb>() != null;
        isPanic = GetComponent<PanicChicken>() != null || GetComponent<RogueChicken>() != null;
        isGhost = GetComponent<GhostChicken>() != null;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshTypeFlags();
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
        wanderCoroutine = StartCoroutine(WanderLoop());
    }

    private void OnDisable()
    {
        All.Remove(this);
        StopAllCoroutines();
        wanderCoroutine = null;
        isFleeing = false;
        isAttracted = false;
        isApproachingNormals = false;
        if (animator != null)
            animator.SetBool(IsMovingHash, false);
    }

    public void SetBossMarchLeft(bool enabled)
    {
        bossMarchLeft = enabled;
        if (enabled)
        {
            if (isFleeing)
                EndFlee();
            if (isAttracted)
                EndAttract();
            if (isApproachingNormals)
                EndApproachNormals();

            marchYTarget = transform.position.y;
            marchYTimer = 0f;
        }
    }

    private void Update()
    {
        if (gravityFrozen)
            return;

        if (bossMarchLeft)
        {
            UpdateBossMarchLeft();
            return;
        }

        if (bossPanic)
        {
            if (isAttracted)
                EndAttract();
            if (isApproachingNormals)
                EndApproachNormals();
            UpdateBossPanicMovement();
            return;
        }

        // Ghosts always chase the farmer (haunt when close — see GhostChicken).
        if (isGhost)
        {
            if (isAttracted)
                EndAttract();
            if (isApproachingNormals)
                EndApproachNormals();
            if (isFleeing)
                EndFlee();
            ChaseFarmer();
            return;
        }

        // Mind / panic chickens are never attracted — they keep fleeing / sprinting.
        bool canBeAttracted = !isMindCluck && !isPanic;

        // While a Mind Cluck pulse can pull this chicken, skip farmer flee.
        if (canBeAttracted && MindCluck.TryGetAttracting(transform.position, transform, out _))
        {
            if (isFleeing)
                EndFlee();
            if (isApproachingNormals)
                EndApproachNormals();

            TryAttractToMindCluck();
            return;
        }

        if (isAttracted)
            EndAttract();

        if (TryFleeFromFarmer())
        {
            if (isApproachingNormals)
                EndApproachNormals();
            return;
        }

        TryApproachNormals();
    }

    private void UpdateBossMarchLeft()
    {
        Vector2 pos = transform.position;

        marchYTimer -= Time.deltaTime;
        if (marchYTimer <= 0f)
        {
            marchYTarget = Random.Range(areaMin.y, areaMax.y);
            marchYTimer = Random.Range(bossMarchYRetargetTime * 0.7f, bossMarchYRetargetTime * 1.35f);
        }

        float speed = moveSpeed * bossMarchSpeedMultiplier;
        float nextX = pos.x - speed * Time.deltaTime;
        float nextY = Mathf.MoveTowards(pos.y, marchYTarget, bossMarchYDrift * Time.deltaTime);

        nextX = Mathf.Max(nextX, areaMin.x);
        nextY = Mathf.Clamp(nextY, areaMin.y, areaMax.y);
        transform.position = new Vector3(nextX, nextY, transform.position.z);

        if (spriteRenderer != null)
            spriteRenderer.flipX = true;

        if (animator != null)
            animator.SetBool(IsMovingHash, true);
    }

    private void UpdateBossPanicMovement()
    {
        Vector2 current = transform.position;

        // Stay packed on the far left of the playable area.
        bossHuddleTimer -= Time.deltaTime;
        if (bossHuddleTimer <= 0f || Vector2.Distance(current, targetPosition) <= arrivalThreshold)
        {
            PickLeftHuddleTarget();
            bossHuddleTimer = Random.Range(bossHuddleRetargetTime * 0.6f, bossHuddleRetargetTime * 1.3f);
        }

        if (!isFleeing)
            BeginFlee();
        MoveToward(targetPosition, CurrentMoveSpeed);
    }

    private void PickLeftHuddleTarget()
    {
        float maxX = areaMin.x + Mathf.Max(0.5f, leftHuddleWidth);
        targetPosition = new Vector2(
            Random.Range(areaMin.x, maxX),
            Random.Range(areaMin.y, areaMax.y));
    }

    /// <summary>Instantly teleport into the left huddle zone.</summary>
    private void SnapToLeftHuddle()
    {
        PickLeftHuddleTarget();
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        bossHuddleTimer = Random.Range(bossHuddleRetargetTime * 0.6f, bossHuddleRetargetTime * 1.3f);
        if (animator != null)
            animator.SetBool(IsMovingHash, false);
    }

    private void ChaseFarmer()
    {
        if (farmerTransform == null)
        {
            if (animator != null)
                animator.SetBool(IsMovingHash, false);
            return;
        }

        float dist = Vector2.Distance(transform.position, farmerTransform.position);
        if (dist <= arrivalThreshold)
        {
            if (animator != null)
                animator.SetBool(IsMovingHash, false);
            return;
        }

        if (animator != null)
            animator.SetBool(IsMovingHash, true);

        MoveToward(farmerTransform.position, CurrentMoveSpeed);
    }

    private bool TryFleeFromFarmer()
    {
        if (farmerTransform == null)
        {
            if (isFleeing)
                EndFlee();
            return false;
        }

        float distance = Vector2.Distance(transform.position, farmerTransform.position);
        bool shouldFlee = distance <= fleeDistance;

        if (shouldFlee)
        {
            if (isAttracted)
                EndAttract();

            if (!isFleeing)
                BeginFlee();

            MoveAwayFromFarmer();
            return true;
        }

        if (isFleeing)
            EndFlee();

        return false;
    }

    private bool TryApproachNormals()
    {
        // Regular bombs close on normals; rogue bombs sprint like panics instead.
        if (!isBomb || isPanic)
            return false;

        if (!TryGetNearestNormal(out Vector2 normalPos, out float dist))
        {
            if (isApproachingNormals)
                EndApproachNormals();
            return false;
        }

        // Close enough — resume random wander.
        if (dist <= bombMaxDistanceFromNormals)
        {
            if (isApproachingNormals)
                EndApproachNormals();
            return false;
        }

        if (!isApproachingNormals)
            BeginApproachNormals();

        MoveToward(normalPos, moveSpeed * bombApproachSpeedMultiplier);
        return true;
    }

    private bool TryGetNearestNormal(out Vector2 position, out float distance)
    {
        position = default;
        distance = float.MaxValue;

        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        bool found = false;

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander other = chickens[i];
            if (other == null || other == this)
                continue;

            // Only plain normals / panics — not bombs, minds, electrics, lasers, or ghosts.
            if (other.GetComponent<Bomb>() != null)
                continue;
            if (other.GetComponent<MindCluck>() != null)
                continue;
            if (other.GetComponent<ElectricChicken>() != null)
                continue;
            if (other.GetComponent<LaserChicken>() != null)
                continue;
            if (other.GetComponent<GhostChicken>() != null)
                continue;

            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < distance)
            {
                distance = d;
                position = other.transform.position;
                found = true;
            }
        }

        return found;
    }

    private void TryAttractToMindCluck()
    {
        // Mind Clucks never get pulled by other Mind Clucks.
        if (isMindCluck)
        {
            if (isAttracted)
                EndAttract();
            return;
        }

        // Only pulled while a nearby Mind Cluck is mid-pulse (small radius + short duration).
        if (!MindCluck.TryGetAttracting(transform.position, transform, out MindCluck mind))
        {
            if (isAttracted)
                EndAttract();
            return;
        }

        Vector2 mindPos = mind.transform.position;
        float dist = Vector2.Distance(transform.position, mindPos);

        // Close enough — pause in place for the rest of the pulse, then wander resumes.
        if (dist <= attractStopDistance)
        {
            if (!isAttracted)
                BeginAttract();
            animator.SetBool(IsMovingHash, false);
            return;
        }

        if (!isAttracted)
            BeginAttract();

        MoveToward(mindPos, moveSpeed * attractSpeedMultiplier);
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
    }

    private void BeginAttract()
    {
        isAttracted = true;
        animator.SetBool(IsMovingHash, true);
    }

    private void EndAttract()
    {
        isAttracted = false;
        animator.SetBool(IsMovingHash, false);
    }

    private void BeginApproachNormals()
    {
        isApproachingNormals = true;
        animator.SetBool(IsMovingHash, true);
    }

    private void EndApproachNormals()
    {
        isApproachingNormals = false;
        animator.SetBool(IsMovingHash, false);
    }

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 current = transform.position;
        Vector2 next = Vector2.MoveTowards(current, target, speed * Time.deltaTime);
        next = ClampToArea(next);

        ApplyFacing(current, next);
        transform.position = next;
    }

    private void MoveAwayFromFarmer()
    {
        Vector2 current = transform.position;
        Vector2 farmerPos = farmerTransform.position;
        float step = CurrentMoveSpeed * fleeSpeedMultiplier * Time.deltaTime;

        Vector2 next = GetEdgeAwareFleePosition(current, farmerPos, step);

        ApplyFacing(current, next);
        transform.position = next;
    }

    private void ApplyFacing(Vector2 current, Vector2 next)
    {
        float deltaX = next.x - current.x;
        if (deltaX != 0f)
            spriteRenderer.flipX = deltaX < 0f;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.flipX = spriteRenderer.flipX;
        }
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

    private bool IsWanderInterrupted => isFleeing || isAttracted || isApproachingNormals || bossPanic || bossMarchLeft || gravityFrozen || isGhost;

    private IEnumerator WanderLoop()
    {
        while (enabled)
        {
            // Pause wander while Update owns movement for flee / attract / approach.
            while (IsWanderInterrupted)
                yield return null;

            PickNewTarget();
            yield return MoveToTarget();

            if (IsWanderInterrupted)
                continue;

            // Panic chickens never idle — immediately pick the next sprint target.
            if (isPanic || bossPanic)
                continue;

            animator.SetBool(IsMovingHash, false);

            // Interruptible idle: check every frame so overrides can start immediately.
            float idleDuration = Random.Range(minIdleTime, maxIdleTime);
            float elapsed = 0f;
            while (elapsed < idleDuration && !IsWanderInterrupted)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    public void Highlight()
    {
        SetAnimBool("Highlight", true);
    }

    public void NoHighlight()
    {
        SetAnimBool("Highlight", false);
    }

    private void SetAnimBool(string param, bool value)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        for (int i = 0; i < animator.parameterCount; i++)
        {
            if (animator.GetParameter(i).name == param)
            {
                animator.SetBool(param, value);
                return;
            }
        }
    }

    private void PickNewTarget()
    {
        // Bombs (not rogues) wander randomly near normals so they don't drift away every idle cycle.
        if (isBomb && !isPanic && TryGetNearestNormal(out Vector2 anchor, out _))
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(0f, bombMaxDistanceFromNormals * 0.85f);
            targetPosition = ClampToArea(anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            return;
        }

        targetPosition = new Vector2(
            Random.Range(areaMin.x, areaMax.x),
            Random.Range(areaMin.y, areaMax.y)
        );
    }

    private IEnumerator MoveToTarget()
    {
        animator.SetBool(IsMovingHash, true);

        while (!IsWanderInterrupted &&
               Vector2.Distance(transform.position, targetPosition) > arrivalThreshold)
        {
            Vector2 current = transform.position;
            Vector2 next = Vector2.MoveTowards(current, targetPosition, CurrentMoveSpeed * Time.deltaTime);

            ApplyFacing(current, next);

            transform.position = next;
            yield return null;
        }

        if (!IsWanderInterrupted)
            transform.position = targetPosition;
    }
}
