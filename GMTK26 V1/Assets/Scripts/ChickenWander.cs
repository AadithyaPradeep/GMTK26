using System.Collections;
using UnityEngine;

public class ChickenWander : MonoBehaviour
{
    [Header("Wander Area")]
    [SerializeField] private Vector2 areaMin = new Vector2(-5f, -5f);
    [SerializeField] private Vector2 areaMax = new Vector2(5f, 5f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float arrivalThreshold = 0.05f;

    [Header("Idle")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 targetPosition;

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
        StartCoroutine(WanderLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (animator != null)
            animator.SetBool(IsMovingHash, false);
    }

    private IEnumerator WanderLoop()
    {
        while (enabled)
        {
            PickNewTarget();
            yield return MoveToTarget();
            animator.SetBool(IsMovingHash, false);

            float idleDuration = Random.Range(minIdleTime, maxIdleTime);
            yield return new WaitForSeconds(idleDuration);
        }
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

        while (Vector2.Distance(transform.position, targetPosition) > arrivalThreshold)
        {
            Vector2 current = transform.position;
            Vector2 next = Vector2.MoveTowards(current, targetPosition, moveSpeed * Time.deltaTime);

            float deltaX = next.x - current.x;
            if (deltaX != 0f)
                spriteRenderer.flipX = deltaX < 0f;

            transform.position = next;
            yield return null;
        }

        transform.position = targetPosition;
    }
}
