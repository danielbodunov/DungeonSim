using UnityEngine;

/// <summary>
/// Presentation-facing view of physical loot custody. Implementations remain
/// the authority for their inventory; visuals only observe the aggregate.
/// </summary>
public interface ICarriedLootPresentationSource
{
    int CarriedLootPresentationItemCount { get; }
    event System.Action CarriedLootPresentationChanged;
}

/// <summary>
/// Displays one generic bundle when this NPC carries any physical loot.
/// The bundle count is always derived from the authoritative custody source.
/// </summary>
[DisallowMultipleComponent]
public sealed class NPCCarriedLootVisual : MonoBehaviour
{
    const string VisualRootName = "Carried Loot Bundle";

    static Material sackMaterial;
    static Material tieMaterial;

    ICarriedLootPresentationSource source;
    Transform visualRoot;
    int representedItemCount = -1;

    public bool IsVisible => visualRoot != null && visualRoot.gameObject.activeSelf;
    public int RepresentedItemCount => Mathf.Max(0, representedItemCount);

    void Awake()
    {
        ResolveSource();
        EnsureVisual();
        RefreshFromSource();
    }

    void OnEnable()
    {
        ResolveSource();
        if (source != null)
            source.CarriedLootPresentationChanged += RefreshFromSource;
        RefreshFromSource();
    }

    void OnDisable()
    {
        if (source != null)
            source.CarriedLootPresentationChanged -= RefreshFromSource;
    }

    void ResolveSource()
    {
        if (source != null)
            return;

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ICarriedLootPresentationSource candidate)
            {
                source = candidate;
                return;
            }
        }
    }

    public void RefreshFromSource()
    {
        int itemCount = source != null
            ? Mathf.Max(0, source.CarriedLootPresentationItemCount)
            : 0;
        representedItemCount = itemCount;

        EnsureVisual();
        if (visualRoot == null)
            return;

        visualRoot.gameObject.SetActive(itemCount > 0);
        float fullness = 1f + Mathf.Min(3, Mathf.Max(0, itemCount - 1)) * 0.08f;
        visualRoot.localScale = Vector3.one * fullness;
    }

    void EnsureVisual()
    {
        if (visualRoot != null)
            return;

        Transform existing = transform.Find(VisualRootName);
        if (existing != null)
        {
            visualRoot = existing;
            return;
        }

        CalculatePlacement(
            out Vector3 localPosition,
            out float bundleSize);

        var root = new GameObject(VisualRootName);
        root.layer = gameObject.layer;
        visualRoot = root.transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = localPosition;
        visualRoot.localRotation = Quaternion.identity;

        CreatePart(
            "Sack",
            PrimitiveType.Sphere,
            new Vector3(0f, 0f, 0f),
            new Vector3(0.82f, 1f, 0.62f) * bundleSize,
            GetSackMaterial());
        CreatePart(
            "Neck",
            PrimitiveType.Cylinder,
            new Vector3(0f, bundleSize * 0.48f, 0f),
            new Vector3(0.25f, 0.16f, 0.25f) * bundleSize,
            GetSackMaterial());
        CreatePart(
            "Tie",
            PrimitiveType.Cylinder,
            new Vector3(0f, bundleSize * 0.36f, 0f),
            new Vector3(0.34f, 0.055f, 0.34f) * bundleSize,
            GetTieMaterial());

        root.SetActive(false);
    }

    void CalculatePlacement(out Vector3 localPosition, out float bundleSize)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null || target is LineRenderer)
                continue;
            if (!hasBounds)
            {
                bounds = target.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(target.bounds);
            }
        }

        if (!hasBounds)
        {
            bundleSize = 0.12f;
            localPosition = new Vector3(0.14f, 0.18f, 0.08f);
            return;
        }

        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        Vector3 scale = transform.lossyScale;
        float localHeight = bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float localHalfWidth = bounds.extents.x /
            Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float localHalfDepth = bounds.extents.z /
            Mathf.Max(0.0001f, Mathf.Abs(scale.z));

        bundleSize = Mathf.Clamp(localHeight * 0.22f, 0.08f, 0.2f);
        localPosition = new Vector3(
            localCenter.x + localHalfWidth + bundleSize * 0.2f,
            localCenter.y - localHeight * 0.08f,
            localCenter.z + localHalfDepth + bundleSize * 0.15f);
    }

    void CreatePart(
        string partName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.layer = gameObject.layer;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Collider generatedCollider = part.GetComponent<Collider>();
        if (generatedCollider != null)
        {
            generatedCollider.enabled = false;
            Destroy(generatedCollider);
        }

        Renderer targetRenderer = part.GetComponent<Renderer>();
        if (targetRenderer != null && material != null)
            targetRenderer.sharedMaterial = material;
    }

    static Material GetSackMaterial()
    {
        if (sackMaterial == null)
            sackMaterial = CreateMaterial(
                "Runtime Carried Loot Sack",
                new Color(0.34f, 0.16f, 0.065f, 1f));
        return sackMaterial;
    }

    static Material GetTieMaterial()
    {
        if (tieMaterial == null)
            tieMaterial = CreateMaterial(
                "Runtime Carried Loot Tie",
                new Color(0.72f, 0.52f, 0.2f, 1f));
        return tieMaterial;
    }

    static Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        if (shader == null)
            return null;

        var material = new Material(shader)
        {
            name = materialName,
            hideFlags = HideFlags.HideAndDontSave
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }
}
