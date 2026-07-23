using UnityEngine;

public class HighlightCluck : MonoBehaviour
{

    [SerializeField] private LayerMask cluckLayer;
    [SerializeField] private float range = 1f;

    private PlayerMovement mov;
    public ChickenWander lastObject;
    private bool disabled = false;
    RaycastHit2D[] results = new RaycastHit2D[1];
    void Start()
    {
        mov = GetComponent<PlayerMovement>();
    }


    void Update()
    {
        if (disabled) return;

        var count = Physics2D.RaycastNonAlloc(transform.position, mov.GetLastDirection(), results, range, cluckLayer);
        RaycastHit2D hit = count > 0 ? results[0] : default;
        Collider2D collider = hit.collider;

        if (collider != null)
        {
            if (lastObject != null) lastObject.NoHighlight();
            lastObject = collider.transform.GetComponentInParent<ChickenWander>();
            lastObject.Highlight();

        }
        else if (lastObject != null)
        {
            lastObject.NoHighlight();
            lastObject = null;

        }
    }


    public void ToogleInteraction()
    {
        disabled = !disabled;
    }

    public ChickenWander GetSelectedClucks()
    {
        return lastObject != null ? lastObject : null;
    }


}