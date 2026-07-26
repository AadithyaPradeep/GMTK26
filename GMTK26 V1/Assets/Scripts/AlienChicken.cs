using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World2 alien — pulses a green gravity field like Mind Cluck (active briefly, then cooldown roam).
/// </summary>
public class AlienChicken : MonoBehaviour
{
    [Header("Field")]
    [SerializeField] private float pullRadius = 3f;
    [SerializeField] private float liftHeight = 1.25f;
    [SerializeField] private float liftSpeed = 4f;
    [SerializeField] private float grabDelay = 0.45f;
    [SerializeField] private GameObject radiusVisual;

    [Header("Pulse (same timing as Mind Cluck)")]
    [SerializeField] private float pulseDuration = 5f;
    [SerializeField] private float minCooldown = 3.5f;
    [SerializeField] private float maxCooldown = 6f;

    [Header("Deaths")]
    [SerializeField] private float sequentialKillInterval = 5f;
    [SerializeField] private float markedKillSeconds = 3f;

    private static readonly List<AlienChicken> Active = new List<AlienChicken>();

    private readonly List<AlienLiftVictim> victims = new List<AlienLiftVictim>();
    private readonly Dictionary<ChickenWander, float> pendingGrabTimes = new Dictionary<ChickenWander, float>();
    private ChickenWander wander;
    private float sequentialKillTimer;
    private AlienLiftVictim markedRegular;
    private AlienLiftVictim markedEnemy;
    private float markedRegularTimer;
    private float markedEnemyTimer;

    public float PullRadius => pullRadius;
    public float LiftHeight => liftHeight;
    public float LiftSpeed => liftSpeed;
    public bool IsPulsing { get; private set; }
    public bool IsFieldActive => isActiveAndEnabled && IsPulsing;

    public static bool AnyActive
    {
        get
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i] != null && Active[i].IsFieldActive)
                    return true;
                if (Active[i] == null)
                    Active.RemoveAt(i);
            }
            return false;
        }
    }

    private void Awake()
    {
        wander = GetComponent<ChickenWander>();
        if (radiusVisual == null)
        {
            Transform rad = transform.Find("Radius");
            if (rad != null)
                radiusVisual = rad.gameObject;
        }

        SetRadiusVisible(false);
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);

        IsPulsing = false;
        SetRadiusVisible(false);

        if (wander != null)
            wander.SetGravityFrozen(false);

        sequentialKillTimer = sequentialKillInterval;
        markedRegularTimer = 0f;
        markedEnemyTimer = 0f;
        pendingGrabTimes.Clear();
        SetCooldownSprint(true);

        StartCoroutine(PulseLoop());
    }

    private void OnDisable()
    {
        Active.Remove(this);
        IsPulsing = false;
        SetRadiusVisible(false);
        SetCooldownSprint(false);
        ReleaseAllVictims();
        pendingGrabTimes.Clear();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        Active.Remove(this);
        ReleaseAllVictims();
        pendingGrabTimes.Clear();
    }

    private IEnumerator PulseLoop()
    {
        while (enabled)
        {
            float cooldown = Random.Range(minCooldown, maxCooldown);
            yield return new WaitForSeconds(cooldown);

            BeginPulse();
            yield return new WaitForSeconds(pulseDuration);
            EndPulse();
        }
    }

    private void BeginPulse()
    {
        IsPulsing = true;
        SetRadiusVisible(true);
        sequentialKillTimer = sequentialKillInterval;
        pendingGrabTimes.Clear();
        SetCooldownSprint(false);
    }

    private void EndPulse()
    {
        IsPulsing = false;
        SetRadiusVisible(false);
        pendingGrabTimes.Clear();
        ReleaseAllVictims();
        // Sprint like a panic chicken so released bombs don't instantly blow it up.
        SetCooldownSprint(true);
    }

    private void SetCooldownSprint(bool sprint)
    {
        PanicChicken panic = GetComponent<PanicChicken>();
        if (sprint)
        {
            if (panic == null)
                gameObject.AddComponent<PanicChicken>();
        }
        else if (panic != null)
        {
            DestroyImmediate(panic);
        }

        if (wander != null)
            wander.RefreshTypeFlags();
    }

    private void SetRadiusVisible(bool visible)
    {
        if (radiusVisual != null)
            radiusVisual.SetActive(visible);
    }

    private void Update()
    {
        if (!IsPulsing)
            return;

        ScanAndLift();
        UpdateMarkedTimers();
        UpdateSequentialKills();
    }

    private void ScanAndLift()
    {
        PruneVictims();

        ChickenWander[] chickens = FindObjectsByType<ChickenWander>();
        float radiusSq = pullRadius * pullRadius;
        Vector2 origin = transform.position;
        HashSet<ChickenWander> stillInRange = new HashSet<ChickenWander>();

        for (int i = 0; i < chickens.Length; i++)
        {
            ChickenWander c = chickens[i];
            if (c == null)
                continue;
            if (c.gameObject == gameObject)
                continue;

            // Farmer is holding this chicken — leave it alone until dropped.
            if (c.transform.parent != null && c.transform.parent.GetComponent<GrabCluck>() != null)
            {
                RemovePending(c);
                continue;
            }

            AlienLiftVictim victim = c.GetComponent<AlienLiftVictim>();
            if (victim != null && victim.IsLifted)
            {
                RemovePending(c);
                continue;
            }

            Vector2 delta = (Vector2)c.transform.position - origin;
            if (delta.sqrMagnitude > radiusSq)
            {
                RemovePending(c);
                continue;
            }

            stillInRange.Add(c);

            if (!pendingGrabTimes.TryGetValue(c, out float enterTime))
            {
                pendingGrabTimes[c] = Time.time;
                continue;
            }

            if (Time.time - enterTime < grabDelay)
                continue;

            pendingGrabTimes.Remove(c);
            Lift(c);
        }

        // Drop pending entries for chickens that vanished mid-delay.
        if (pendingGrabTimes.Count > 0)
        {
            List<ChickenWander> stale = null;
            foreach (var kv in pendingGrabTimes)
            {
                if (kv.Key != null && stillInRange.Contains(kv.Key))
                    continue;
                if (stale == null)
                    stale = new List<ChickenWander>();
                stale.Add(kv.Key);
            }

            if (stale != null)
            {
                for (int i = 0; i < stale.Count; i++)
                    pendingGrabTimes.Remove(stale[i]);
            }
        }
    }

    private void RemovePending(ChickenWander chicken)
    {
        if (chicken != null)
            pendingGrabTimes.Remove(chicken);
    }

    private void Lift(ChickenWander chicken)
    {
        if (chicken == null || !IsPulsing)
            return;

        AlienLiftVictim victim = chicken.GetComponent<AlienLiftVictim>();
        if (victim == null)
            victim = chicken.gameObject.AddComponent<AlienLiftVictim>();

        if (victim.IsLifted)
            return;

        victim.BeginLift(this);
        RegisterVictim(victim);
    }

    public void NotifyVictimReleased(AlienLiftVictim victim)
    {
        if (victim == null)
            return;

        victims.Remove(victim);
        if (markedRegular == victim)
            ClearMarkedRegular();
        if (markedEnemy == victim)
            ClearMarkedEnemy();
    }

    public void RegisterVictim(AlienLiftVictim victim)
    {
        if (victim == null)
            return;
        if (!victims.Contains(victim))
            victims.Add(victim);
    }

    public void NotifyVictimDestroyed(AlienLiftVictim victim)
    {
        NotifyVictimReleased(victim);
    }

    private void UpdateSequentialKills()
    {
        if (victims.Count == 0)
        {
            sequentialKillTimer = sequentialKillInterval;
            return;
        }

        sequentialKillTimer -= Time.deltaTime;
        if (sequentialKillTimer > 0f)
            return;

        sequentialKillTimer = sequentialKillInterval;
        AlienLiftVictim pick = PickAnyVictim();
        if (pick != null)
            pick.KillByAlien();
    }

    private void UpdateMarkedTimers()
    {
        if (markedRegular == null || !markedRegular.IsLifted)
            AssignMarkedRegular();
        else
        {
            markedRegularTimer -= Time.deltaTime;
            markedRegular.SetDeathCountdown(markedRegularTimer);
            if (markedRegularTimer <= 0f)
            {
                AlienLiftVictim dead = markedRegular;
                ClearMarkedRegular();
                dead.KillByAlien();
                AssignMarkedRegular();
            }
        }

        if (markedEnemy == null || !markedEnemy.IsLifted)
            AssignMarkedEnemy();
        else
        {
            markedEnemyTimer -= Time.deltaTime;
            markedEnemy.SetDeathCountdown(markedEnemyTimer);
            if (markedEnemyTimer <= 0f)
            {
                AlienLiftVictim dead = markedEnemy;
                ClearMarkedEnemy();
                dead.KillByAlien();
                AssignMarkedEnemy();
            }
        }
    }

    private void AssignMarkedRegular()
    {
        ClearMarkedRegular();
        AlienLiftVictim pick = PickTypedVictim(isEnemy: false);
        if (pick == null)
            return;

        markedRegular = pick;
        markedRegularTimer = markedKillSeconds;
        markedRegular.SetDeathCountdown(markedRegularTimer);
    }

    private void AssignMarkedEnemy()
    {
        ClearMarkedEnemy();
        AlienLiftVictim pick = PickTypedVictim(isEnemy: true);
        if (pick == null)
            return;

        markedEnemy = pick;
        markedEnemyTimer = markedKillSeconds;
        markedEnemy.SetDeathCountdown(markedEnemyTimer);
    }

    private void ClearMarkedRegular()
    {
        if (markedRegular != null)
            markedRegular.ClearDeathCountdown();
        markedRegular = null;
        markedRegularTimer = 0f;
    }

    private void ClearMarkedEnemy()
    {
        if (markedEnemy != null)
            markedEnemy.ClearDeathCountdown();
        markedEnemy = null;
        markedEnemyTimer = 0f;
    }

    private AlienLiftVictim PickTypedVictim(bool isEnemy)
    {
        PruneVictims();
        List<AlienLiftVictim> pool = new List<AlienLiftVictim>();
        for (int i = 0; i < victims.Count; i++)
        {
            AlienLiftVictim v = victims[i];
            if (v == null || !v.IsLifted)
                continue;
            if (v.IsEnemy != isEnemy)
                continue;
            if (!isEnemy && v == markedEnemy)
                continue;
            if (isEnemy && v == markedRegular)
                continue;
            pool.Add(v);
        }

        if (pool.Count == 0)
            return null;

        return pool[Random.Range(0, pool.Count)];
    }

    private AlienLiftVictim PickAnyVictim()
    {
        PruneVictims();
        List<AlienLiftVictim> pool = new List<AlienLiftVictim>();
        for (int i = 0; i < victims.Count; i++)
        {
            AlienLiftVictim v = victims[i];
            if (v == null || !v.IsLifted)
                continue;
            pool.Add(v);
        }

        if (pool.Count == 0)
            return null;

        return pool[Random.Range(0, pool.Count)];
    }

    private void PruneVictims()
    {
        for (int i = victims.Count - 1; i >= 0; i--)
        {
            if (victims[i] == null)
                victims.RemoveAt(i);
        }
    }

    private void ReleaseAllVictims()
    {
        ClearMarkedRegular();
        ClearMarkedEnemy();

        for (int i = victims.Count - 1; i >= 0; i--)
        {
            AlienLiftVictim v = victims[i];
            if (v != null)
                v.ForceRelease();
        }

        victims.Clear();
    }

    public bool IsInsideField(Vector2 worldPos)
    {
        if (!IsFieldActive)
            return false;

        float radiusSq = pullRadius * pullRadius;
        return ((Vector2)transform.position - worldPos).sqrMagnitude <= radiusSq;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsPulsing
            ? new Color(0.2f, 1f, 0.35f, 0.85f)
            : new Color(0.2f, 1f, 0.35f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
#endif
}
