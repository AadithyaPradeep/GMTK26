using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabCluck : MonoBehaviour
{
    public HighlightCluck hc;
    public Animator aimAnimator;
    public GameObject hand;
    public CinemachineImpulseSource source;
    public float strength;

    [SerializeField] private float dropOffsetX = 1.25f;
    [SerializeField] private Vector3 holdLocalPosition = new Vector3(0f, 0.3f, 0f);

    private Transform grabbedCluck;
    private Animator heldAnimator;
    private SpriteRenderer farmerSprite;
    private bool lockManualLaser; // boss-wave laser stuck in hands; Space only shoots

    private void Awake()
    {
        farmerSprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        if (grabbedCluck == null && heldAnimator != null)
            ClearHoldState();

        if (Keyboard.current == null)
            return;

        // Boss laser in hands: hold Space/E for machine-gun bursts.
        if (grabbedCluck != null)
        {
            LaserChicken heldLaser = grabbedCluck.GetComponent<LaserChicken>();
            if (heldLaser != null && heldLaser.IsManualFire)
            {
                // One shot per press (story laser cooldown) or hold for gun bursts.
                if (GrabPressedThisFrame() || GrabHeld())
                    heldLaser.TryFireManual();
                return;
            }

            if (lockManualLaser)
                return;

            if (!GrabPressedThisFrame())
                return;

            Drop();
            return;
        }

        if (GrabHeld() && LaserChicken.TryFireAnyManual())
            return;

        if (!GrabPressedThisFrame())
            return;

        TryGrab();
    }

    private static bool GrabPressedThisFrame()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame);
    }

    private static bool GrabHeld()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.spaceKey.isPressed || kb.eKey.isPressed);
    }

    /// <summary>Puts a chicken/gun directly into the farmer's hands (boss-wave weapon).</summary>
    public bool ForceGrab(Transform cluck, bool lockAsManualLaser = false, Vector3? holdLocalOverride = null)
    {
        if (cluck == null)
            return false;

        if (grabbedCluck != null)
            Drop();

        ChickenWander wander = cluck.GetComponent<ChickenWander>();
        BoxCollider2D col = cluck.GetComponent<BoxCollider2D>();
        Animator anim = cluck.GetComponent<Animator>();

        grabbedCluck = cluck;
        heldAnimator = anim;
        lockManualLaser = lockAsManualLaser;

        LaserChicken laser = cluck.GetComponent<LaserChicken>();
        if (laser != null)
            laser.IsHeld = true;

        FireChicken fire = cluck.GetComponent<FireChicken>();
        if (fire != null)
            fire.IsHeld = true;

        SkeleCluck skele = cluck.GetComponent<SkeleCluck>();
        if (skele != null)
            skele.IsHeld = true;

        AlienLiftVictim lift = cluck.GetComponent<AlienLiftVictim>();
        if (lift != null)
            lift.OnFarmerGrabbed();

        cluck.SetParent(transform);
        if (source != null)
        {
            Vector3 dif = cluck.position - transform.position;
            source.GenerateImpulseWithVelocity(strength * dif.normalized);
        }

        cluck.localPosition = holdLocalOverride ?? holdLocalPosition;
        cluck.localRotation = Quaternion.identity;

        if (col != null)
            col.enabled = false;

        if (wander != null)
            wander.enabled = false;

        if (heldAnimator != null)
            TrySetAnimBool(heldAnimator, "Grabbed", true);

        if (hc != null)
            hc.ClearSelection();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayGrab();

        return true;
    }

    public void ClearBossLaserLock()
    {
        lockManualLaser = false;
    }

    private void TryGrab()
    {
        if (hc == null)
            return;

        ChickenWander selected = hc.GetSelectedClucks();
        if (selected == null)
            return;

        ForceGrab(selected.transform, lockAsManualLaser: false);
        selected.NoHighlight();
    }

    private void Drop()
    {
        if (grabbedCluck == null)
        {
            ClearHoldState();
            return;
        }

        Transform cluck = grabbedCluck;
        ChickenWander wander = cluck.GetComponent<ChickenWander>();
        BoxCollider2D col = cluck.GetComponent<BoxCollider2D>();
        LaserChicken laser = cluck.GetComponent<LaserChicken>();
        ElectricChicken electric = cluck.GetComponent<ElectricChicken>();
        FireChicken fire = cluck.GetComponent<FireChicken>();
        SkeleCluck skele = cluck.GetComponent<SkeleCluck>();

        if (heldAnimator != null)
            TrySetAnimBool(heldAnimator, "Grabbed", false);

        cluck.SetParent(null);

        bool facingLeft = farmerSprite != null && farmerSprite.flipX;
        float offset = facingLeft ? -dropOffsetX : dropOffsetX;
        cluck.position = new Vector3(transform.position.x + offset, transform.position.y, cluck.position.z);

        if (col != null)
            col.enabled = true;

        bool keepLocked = (laser != null && laser.IsFiring)
            || (electric != null && electric.IsStriking)
            || (fire != null && fire.IsFiring)
            || (skele != null && skele.IsFiring);

        if (wander != null && !keepLocked)
            wander.enabled = true;

        if (laser != null)
            laser.IsHeld = false;

        if (fire != null)
            fire.IsHeld = false;

        if (skele != null)
            skele.IsHeld = false;

        AlienLiftVictim lift = cluck.GetComponent<AlienLiftVictim>();
        if (lift != null)
            lift.OnFarmerDropped();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayDrop();

        grabbedCluck = null;
        heldAnimator = null;
        lockManualLaser = false;
    }

    private void ClearHoldState()
    {
        grabbedCluck = null;
        heldAnimator = null;
        lockManualLaser = false;
    }

    private static void TrySetAnimBool(Animator animator, string param, bool value)
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
}
