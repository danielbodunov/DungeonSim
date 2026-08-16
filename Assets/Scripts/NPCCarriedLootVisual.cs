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
        LootBundleVisualFactory.ApplyItemCount(visualRoot, itemCount);
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

        CalculatePlacement(out Vector3 localPosition);
        visualRoot = LootBundleVisualFactory.CreateBundle(
            transform,
            VisualRootName,
            gameObject.layer,
            localPosition,
            0);
        visualRoot.gameObject.SetActive(false);
    }

    void CalculatePlacement(out Vector3 localPosition)
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

        float bundleSize = LootBundleVisualFactory.BundleSize;
        localPosition = new Vector3(
            localCenter.x + localHalfWidth + bundleSize * 0.2f,
            localCenter.y - localHeight * 0.08f,
            localCenter.z + localHalfDepth + bundleSize * 0.15f);
    }
}
