using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger portal — walking the farmer into it loads the next world scene.
/// Uses proximity because the farmer moves via transform.position (triggers are unreliable).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelPortal : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "World2";
    [SerializeField] private string farmerObjectName = "Farmer";
    [SerializeField] private float enterRadius = 3.5f;

    private bool loading;
    private Transform farmer;
    private Collider2D portalCollider;

    public void SetTargetScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            targetSceneName = sceneName.Trim();
    }

    public void Configure(string sceneName, float radius = 3.5f)
    {
        loading = false;
        enabled = true;
        SetTargetScene(sceneName);
        enterRadius = Mathf.Max(2f, radius);
        EnsureTriggerCollider();
        CacheFarmer(force: true);
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void OnEnable()
    {
        loading = false;
        EnsureTriggerCollider();
        CacheFarmer(force: true);
    }

    private void Start()
    {
        CacheFarmer(force: true);
    }

    private void EnsureTriggerCollider()
    {
        portalCollider = GetComponent<Collider2D>();
        if (portalCollider == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 3.5f);
            portalCollider = box;
        }

        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = true;
            cols[i].isTrigger = true;
        }

        if (portalCollider is BoxCollider2D boxCol)
            boxCol.size = new Vector2(Mathf.Max(boxCol.size.x, 3f), Mathf.Max(boxCol.size.y, 3.5f));
    }

    private void CacheFarmer(bool force = false)
    {
        if (!force && farmer != null)
            return;

        GameObject go = GameObject.Find(farmerObjectName);
        if (go != null)
            farmer = go.transform;
    }

    private void Update()
    {
        if (loading)
            return;

        if (portalCollider != null && !portalCollider.isTrigger)
            portalCollider.isTrigger = true;

        CacheFarmer();
        if (farmer == null)
            return;

        if (IsFarmerInside())
            EnterPortal();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryEnterFromCollider(other);

    private void OnTriggerStay2D(Collider2D other) => TryEnterFromCollider(other);

    private void TryEnterFromCollider(Collider2D other)
    {
        if (loading || other == null)
            return;

        if (other.GetComponent<PlayerMovement>() == null
            && other.GetComponentInParent<PlayerMovement>() == null
            && other.transform.root.name != farmerObjectName
            && other.name != farmerObjectName)
            return;

        EnterPortal();
    }

    private bool IsFarmerInside()
    {
        if (farmer == null)
            return false;

        return Vector2.Distance(farmer.position, transform.position) <= enterRadius;
    }

    private void EnterPortal()
    {
        if (loading)
            return;

        string scene = ResolveTargetScene();
        if (string.IsNullOrEmpty(scene))
            return;

        loading = true;
        Time.timeScale = 1f;

        // Prefer fader, but always fall back to a hard scene load so the portal never soft-locks.
        SceneFader.Load(scene, () =>
        {
            // If fader somehow can't start, load directly.
            if (SceneManager.GetActiveScene().name != scene)
                SceneManager.LoadScene(scene);
        });
    }

    private string ResolveTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
            return targetSceneName;

        // World2 finish portal should always return to World 1.
        if (SceneManager.GetActiveScene().name == "World2")
            return "SampleScene";

        return "World2";
    }
}
