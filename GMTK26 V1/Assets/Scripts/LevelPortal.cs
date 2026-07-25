using UnityEngine;

/// <summary>
/// Trigger portal — walking the farmer into it loads the next world scene.
/// Uses overlap + proximity because the farmer moves via transform.position.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelPortal : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "World2";
    [SerializeField] private string farmerObjectName = "Farmer";
    [SerializeField] private float enterRadius = 1.1f;

    private bool loading;
    private Transform farmer;
    private Collider2D portalCollider;

    private void Awake()
    {
        portalCollider = GetComponent<Collider2D>();
        if (portalCollider != null)
            portalCollider.isTrigger = true;
    }

    private void Start()
    {
        CacheFarmer();
    }

    private void CacheFarmer()
    {
        if (farmer != null)
            return;

        GameObject go = GameObject.Find(farmerObjectName);
        if (go != null)
            farmer = go.transform;
    }

    private void Update()
    {
        if (loading || SceneFader.IsBusy)
            return;

        CacheFarmer();
        if (farmer == null)
            return;

        if (IsFarmerInside())
            EnterPortal();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (loading || SceneFader.IsBusy || other == null)
            return;

        if (other.GetComponent<PlayerMovement>() == null
            && other.transform.root.name != farmerObjectName
            && other.name != farmerObjectName)
            return;

        EnterPortal();
    }

    private bool IsFarmerInside()
    {
        Vector2 farmerPos = farmer.position;

        if (portalCollider != null && portalCollider.enabled)
        {
            if (portalCollider.OverlapPoint(farmerPos))
                return true;

            Vector2 closest = portalCollider.ClosestPoint(farmerPos);
            if (Vector2.Distance(closest, farmerPos) <= 0.15f)
                return true;
        }

        return Vector2.Distance(farmerPos, transform.position) <= enterRadius;
    }

    private void EnterPortal()
    {
        if (loading || SceneFader.IsBusy)
            return;

        loading = true;
        SceneFader.Load(targetSceneName);
    }
}
