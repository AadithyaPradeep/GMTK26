using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Among Us–style lights-out: fullscreen darkness with a soft vision hole on the farmer.
/// Ghost chickens Begin/End haunt; multiple sources stack via refcount.
/// </summary>
public class HauntVisionController : MonoBehaviour
{
    public static HauntVisionController Instance { get; private set; }

    [Header("Vision")]
    [SerializeField] private Transform farmerTransform;
    [SerializeField] private float visionRadius = 3f;
    [SerializeField] private float softEdge = 0.45f;
    [SerializeField] private Color darknessColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private float fadeSpeed = 4f;

    private readonly HashSet<object> activeSources = new HashSet<object>();
    private Camera cam;
    private Transform overlayRoot;
    private MeshRenderer overlayRenderer;
    private Material overlayMaterial;
    private float currentAlpha;
    private float targetAlpha;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

    public static HauntVisionController EnsureExists(Transform farmer)
    {
        if (Instance != null)
        {
            if (Instance.farmerTransform == null && farmer != null)
                Instance.farmerTransform = farmer;
            return Instance;
        }

        HauntVisionController existing = FindAnyObjectByType<HauntVisionController>();
        if (existing != null)
        {
            if (existing.farmerTransform == null && farmer != null)
                existing.farmerTransform = farmer;
            return existing;
        }

        GameObject go = new GameObject("HauntVisionController");
        HauntVisionController ctrl = go.AddComponent<HauntVisionController>();
        ctrl.farmerTransform = farmer;
        return ctrl;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureOverlay();
        SetOverlayVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (overlayMaterial != null)
            Destroy(overlayMaterial);
    }

    public void BeginHaunt(object source)
    {
        if (source == null)
            return;

        EnsureOverlay();
        activeSources.Add(source);
        targetAlpha = darknessColor.a;
        SetOverlayVisible(true);
    }

    public void EndHaunt(object source)
    {
        if (source == null)
            return;

        activeSources.Remove(source);
        if (activeSources.Count == 0)
            targetAlpha = 0f;
    }

    private void LateUpdate()
    {
        if (overlayRenderer == null || overlayMaterial == null)
            return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);

        if (currentAlpha <= 0.001f && targetAlpha <= 0f)
        {
            currentAlpha = 0f;
            SetOverlayVisible(false);
            return;
        }

        UpdateOverlayTransform();
        UpdateOverlayMaterial();
    }

    private void EnsureOverlay()
    {
        if (overlayRenderer != null)
            return;

        cam = Camera.main;
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();

        if (farmerTransform == null)
        {
            GameObject farmer = GameObject.FindGameObjectWithTag("Player");
            if (farmer == null)
                farmer = GameObject.Find("Farmer");
            if (farmer != null)
                farmerTransform = farmer.transform;
        }

        Shader shader = Shader.Find("GMTK/HauntVision");
        if (shader == null)
        {
            Debug.LogError("HauntVisionController: missing shader GMTK/HauntVision.");
            return;
        }

        overlayRoot = new GameObject("HauntVisionOverlay").transform;
        overlayRoot.SetParent(cam != null ? cam.transform : transform, false);
        overlayRoot.localRotation = Quaternion.identity;
        overlayRoot.localPosition = new Vector3(0f, 0f, 1f);

        MeshFilter filter = overlayRoot.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = BuildQuadMesh();

        overlayRenderer = overlayRoot.gameObject.AddComponent<MeshRenderer>();
        overlayMaterial = new Material(shader);
        overlayRenderer.sharedMaterial = overlayMaterial;
        overlayRenderer.sortingOrder = 500;

        UpdateOverlayTransform();
        UpdateOverlayMaterial();
    }

    private static Mesh BuildQuadMesh()
    {
        Mesh mesh = new Mesh { name = "HauntVisionQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void UpdateOverlayTransform()
    {
        if (overlayRoot == null)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam != null && cam.orthographic)
        {
            float height = cam.orthographicSize * 2.2f;
            float width = height * cam.aspect;
            overlayRoot.localScale = new Vector3(width, height, 1f);
            overlayRoot.localPosition = new Vector3(0f, 0f, Mathf.Abs(cam.nearClipPlane) + 0.5f);
        }
        else
        {
            overlayRoot.localScale = new Vector3(40f, 24f, 1f);
        }
    }

    private void UpdateOverlayMaterial()
    {
        Vector3 center = farmerTransform != null
            ? farmerTransform.position
            : Vector3.zero;

        Color c = darknessColor;
        c.a = currentAlpha;
        overlayMaterial.SetColor(ColorId, c);
        overlayMaterial.SetVector(CenterId, new Vector4(center.x, center.y, center.z, 0f));
        overlayMaterial.SetFloat(RadiusId, visionRadius);
        overlayMaterial.SetFloat(SoftnessId, softEdge);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.gameObject.SetActive(visible);
    }
}
