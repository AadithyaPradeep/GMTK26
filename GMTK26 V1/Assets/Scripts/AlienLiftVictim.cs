using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Applied to chickens caught in an alien gravity field.
/// </summary>
public class AlienLiftVictim : MonoBehaviour
{
    private static readonly Color GrabTint = new Color(0.55f, 1f, 0.55f, 1f);
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int GrabbedHash = Animator.StringToHash("Grabbed");

    [SerializeField] private Color countdownColor = Color.white;

    private AlienChicken alien;
    private AlienChicken lastAlien;
    private ChickenWander wander;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Bomb bomb;
    private TextMeshPro countdownText;
    private GameObject countdownGo;

    private bool lifted;
    private float groundY;
    private float targetY;
    private Vector3 liftOffset;
    private bool hasOriginalVisuals;
    private Sprite originalSprite;
    private Color originalColor = Color.white;
    private string outlineStateName;
    private string idleStateName;

    private readonly List<Behaviour> frozenPowers = new List<Behaviour>();
    private readonly List<bool> frozenWasEnabled = new List<bool>();

    public bool IsLifted => lifted;

    public bool IsEnemy
    {
        get
        {
            if (GetComponent<Bomb>() != null)
                return true;
            if (GetComponent<FireChicken>() != null)
                return true;
            if (GetComponent<ElectricChicken>() != null)
                return true;
            if (GetComponent<LaserChicken>() != null)
                return true;
            if (GetComponent<AlienChicken>() != null)
                return true;
            return false;
        }
    }

    private void Awake()
    {
        wander = GetComponent<ChickenWander>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bomb = GetComponent<Bomb>();
    }

    private void OnDestroy()
    {
        if (alien != null)
            alien.NotifyVictimDestroyed(this);
        DestroyCountdown();
    }

    private void Update()
    {
        if (!lifted)
            return;

        if (alien != null)
        {
            // Stay locked relative to the alien so the field carries victims with it.
            Vector3 alienPos = alien.transform.position;
            groundY = alienPos.y;
            Vector3 follow = new Vector3(
                alienPos.x + liftOffset.x,
                Mathf.MoveTowards(
                    transform.position.y,
                    alienPos.y + alien.LiftHeight,
                    alien.LiftSpeed * Time.deltaTime),
                alienPos.z + liftOffset.z);
            transform.position = follow;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.MoveTowards(pos.y, targetY, 4f * Time.deltaTime);
            transform.position = pos;
        }

        if (wander != null)
            wander.SetGravityFrozen(true);
    }

    public void BeginLift(AlienChicken source)
    {
        if (lifted || source == null)
            return;

        alien = source;
        lastAlien = source;
        lifted = true;
        groundY = source.transform.position.y;
        targetY = groundY + source.LiftHeight;
        liftOffset = transform.position - source.transform.position;
        liftOffset.y = source.LiftHeight;

        if (wander != null)
            wander.SetGravityFrozen(true);

        FreezePowers();
        ShowOutline(true);
        if (alien != null)
            alien.RegisterVictim(this);
    }

    /// <summary>Farmer picked this chicken up — leave the field until dropped.</summary>
    public void OnFarmerGrabbed()
    {
        if (!lifted)
            return;

        AlienChicken source = alien;
        ClearLiftState(restoreGroundY: false);
        if (source != null)
            source.NotifyVictimReleased(this);
    }

    /// <summary>Farmer dropped this chicken — re-lift if still inside the field.</summary>
    public void OnFarmerDropped()
    {
        AlienChicken field = lastAlien != null && lastAlien.IsFieldActive
            ? lastAlien
            : FindActiveAlienContaining(transform.position);

        if (field != null && field.IsInsideField(transform.position))
        {
            BeginLift(field);
            return;
        }

        // Fully rescued outside the field.
        if (wander != null)
            wander.SetGravityFrozen(false);

        RestoreVisuals();
    }

    private static AlienChicken FindActiveAlienContaining(Vector2 pos)
    {
        AlienChicken[] aliens = FindObjectsByType<AlienChicken>();
        for (int i = 0; i < aliens.Length; i++)
        {
            AlienChicken a = aliens[i];
            if (a != null && a.IsFieldActive && a.IsInsideField(pos))
                return a;
        }

        return null;
    }

    public void ForceRelease()
    {
        ClearLiftState(restoreGroundY: true);
        if (wander != null)
            wander.SetGravityFrozen(false);
    }

    public void KillByAlien()
    {
        if (this == null)
            return;

        AlienChicken source = alien;
        ClearLiftState(restoreGroundY: false);
        if (source != null)
            source.NotifyVictimDestroyed(this);

        // Detonate bombs instead of silently deleting when possible.
        if (bomb != null)
        {
            bomb.enabled = true;
            bomb.Detonate();
            return;
        }

        Destroy(gameObject);
    }

    public void SetDeathCountdown(float secondsLeft)
    {
        EnsureCountdownText();
        if (countdownGo == null || countdownText == null)
            return;

        countdownGo.SetActive(true);
        countdownText.color = countdownColor;
        countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, secondsLeft)).ToString();
    }

    public void ClearDeathCountdown()
    {
        if (countdownGo != null)
            countdownGo.SetActive(false);
    }

    private void ClearLiftState(bool restoreGroundY)
    {
        lifted = false;
        ShowOutline(false);
        ClearDeathCountdown();
        RestorePowers();

        if (restoreGroundY)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }

        alien = null;
    }

    private void FreezePowers()
    {
        RestorePowers();

        // Pause every special power while midair — including blue bombs and fire.
        FreezePower(GetComponent<Bomb>());
        FreezePower(GetComponent<FireChicken>());
        FreezePower(GetComponent<LaserChicken>());
        FreezePower(GetComponent<ElectricChicken>());
        FreezePower(GetComponent<MindCluck>());
    }

    private void FreezePower(Behaviour power)
    {
        if (power == null)
            return;

        frozenPowers.Add(power);
        frozenWasEnabled.Add(power.enabled);
        power.enabled = false;
    }

    private void RestorePowers()
    {
        for (int i = 0; i < frozenPowers.Count; i++)
        {
            Behaviour power = frozenPowers[i];
            if (power != null)
                power.enabled = frozenWasEnabled[i];
        }

        frozenPowers.Clear();
        frozenWasEnabled.Clear();
    }

    private void ShowOutline(bool on)
    {
        if (on)
        {
            CacheOriginalVisuals();
            outlineStateName = ResolveOutlineStateName();
            idleStateName = ResolveIdleStateName();

            if (!string.IsNullOrEmpty(outlineStateName) && animator != null)
                animator.Play(outlineStateName, 0, 0f);

            // Slight green tint on top of the alien-grabbed anim for every chicken.
            if (spriteRenderer != null)
                spriteRenderer.color = GrabTint;
        }
        else
        {
            RestoreVisuals();
        }
    }

    private void CacheOriginalVisuals()
    {
        if (hasOriginalVisuals)
            return;

        hasOriginalVisuals = true;
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            originalColor = spriteRenderer.color;
        }
    }

    private void RestoreVisuals()
    {
        if (animator != null)
        {
            // Leave alien-grabbed state; Play(0) was invalid and left the last grabbed frame stuck.
            if (!string.IsNullOrEmpty(idleStateName) && HasState(idleStateName))
                animator.Play(idleStateName, 0, 0f);
            else if (HasState("CluckIdle"))
                animator.Play("CluckIdle", 0, 0f);
            else
                animator.Rebind();

            if (HasParam(IsMovingHash))
                animator.SetBool(IsMovingHash, false);
            // Don't clear Grabbed if farmer is currently holding this chicken.
            if (HasParam(GrabbedHash) && !IsHeldByFarmer())
                animator.SetBool(GrabbedHash, false);

            animator.Update(0f);
        }

        if (spriteRenderer != null && hasOriginalVisuals)
        {
            spriteRenderer.color = originalColor;
            if (originalSprite != null)
                spriteRenderer.sprite = originalSprite;
        }

        hasOriginalVisuals = false;
        outlineStateName = null;
    }

    private bool IsHeldByFarmer()
    {
        return transform.parent != null && transform.parent.GetComponent<GrabCluck>() != null;
    }

    private bool HasParam(int hash)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hash)
                return true;
        }

        return false;
    }

    private string ResolveOutlineStateName()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        if (HasState("BlueFireCluckAlienGrabbed"))
            return "BlueFireCluckAlienGrabbed";
        if (GetComponent<FireChicken>() != null && HasState("FireCluckAlienGrabbed"))
            return "FireCluckAlienGrabbed";
        if (HasState("CluckAlienGrabbed"))
            return "CluckAlienGrabbed";
        if (HasState("FireCluckAlienGrabbed"))
            return "FireCluckAlienGrabbed";
        return null;
    }

    private string ResolveIdleStateName()
    {
        if (HasState("CluckIdle"))
            return "CluckIdle";
        if (HasState("Idle"))
            return "Idle";
        return null;
    }

    private bool HasState(string stateName)
    {
        if (animator == null)
            return false;
        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    private void EnsureCountdownText()
    {
        if (countdownText != null)
            return;

        // Prefer an existing Timer child (bombs / fire chickens).
        Transform existing = transform.Find("Timer");
        if (existing != null)
        {
            countdownText = existing.GetComponent<TextMeshPro>();
            countdownGo = existing.gameObject;
            if (countdownText != null)
                return;
        }

        countdownGo = new GameObject("AlienDeathTimer");
        countdownGo.transform.SetParent(transform, false);
        countdownGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        TextMeshPro tmp = countdownGo.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 4f;
        tmp.color = countdownColor;
        tmp.text = "";
        countdownText = tmp;
    }

    private void DestroyCountdown()
    {
        if (countdownGo != null && countdownGo.name == "AlienDeathTimer")
            Destroy(countdownGo);
    }
}
