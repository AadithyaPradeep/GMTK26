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

    private void Awake()
    {
        farmerSprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Chicken can die while held (explosion / laser / etc.).
        if (grabbedCluck == null && heldAnimator != null)
            ClearHoldState();

        if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (grabbedCluck != null)
            Drop();
        else
            TryGrab();
    }

    private void TryGrab()
    {
        if (hc == null)
            return;

        ChickenWander selected = hc.GetSelectedClucks();
        if (selected == null)
            return;

        Transform cluck = selected.transform;
        ChickenWander wander = selected;
        BoxCollider2D col = cluck.GetComponent<BoxCollider2D>();
        Animator anim = cluck.GetComponent<Animator>();

        grabbedCluck = cluck;
        heldAnimator = anim;

        LaserChicken laser = cluck.GetComponent<LaserChicken>();
        if (laser != null)
            laser.IsHeld = true;

        cluck.SetParent(transform);
        Vector3 dif = cluck.position - transform.position;
        if (source != null)
            source.GenerateImpulseWithVelocity(strength * dif.normalized);

        cluck.localPosition = holdLocalPosition;

        if (col != null)
            col.enabled = false;

        if (wander != null)
            wander.enabled = false;

        if (heldAnimator != null)
            heldAnimator.SetBool("Grabbed", true);

        selected.NoHighlight();
        if (hc != null)
            hc.ClearSelection();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayGrab();
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

        if (heldAnimator != null)
            heldAnimator.SetBool("Grabbed", false);

        cluck.SetParent(null);

        bool facingLeft = farmerSprite != null && farmerSprite.flipX;
        float offset = facingLeft ? -dropOffsetX : dropOffsetX;
        cluck.position = new Vector3(transform.position.x + offset, transform.position.y, cluck.position.z);

        if (col != null)
            col.enabled = true;

        // Don't restart wander mid laser/electric attack.
        bool keepLocked = (laser != null && laser.IsFiring)
            || (electric != null && electric.IsStriking);

        if (wander != null && !keepLocked)
            wander.enabled = true;

        if (laser != null)
            laser.IsHeld = false;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayDrop();

        grabbedCluck = null;
        heldAnimator = null;
    }

    private void ClearHoldState()
    {
        grabbedCluck = null;
        heldAnimator = null;
    }
}
