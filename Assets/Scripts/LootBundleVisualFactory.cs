using UnityEngine;

/// <summary>Shared geometry and scale for generic carried and dropped loot.</summary>
internal static class LootBundleVisualFactory
{
    // Matches the lower bound previously used by the placeholder adventurer's
    // renderer-relative carried bundle sizing.
    internal const float BundleSize = 0.08f;

    static Material sackMaterial;
    static Material tieMaterial;

    internal static Transform CreateBundle(
        Transform parent,
        string rootName,
        int layer,
        Vector3 localPosition,
        int itemCount)
    {
        Transform existing = parent.Find(rootName);
        if (existing != null)
        {
            ApplyItemCount(existing, itemCount);
            return existing;
        }

        var rootObject = new GameObject(rootName);
        rootObject.layer = layer;
        Transform root = rootObject.transform;
        root.SetParent(parent, false);
        root.localPosition = localPosition;
        root.localRotation = Quaternion.identity;

        CreatePart(
            root,
            "Sack",
            PrimitiveType.Sphere,
            Vector3.zero,
            new Vector3(0.82f, 1f, 0.62f) * BundleSize,
            GetSackMaterial());
        CreatePart(
            root,
            "Neck",
            PrimitiveType.Cylinder,
            new Vector3(0f, BundleSize * 0.48f, 0f),
            new Vector3(0.25f, 0.16f, 0.25f) * BundleSize,
            GetSackMaterial());
        CreatePart(
            root,
            "Tie",
            PrimitiveType.Cylinder,
            new Vector3(0f, BundleSize * 0.36f, 0f),
            new Vector3(0.34f, 0.055f, 0.34f) * BundleSize,
            GetTieMaterial());

        ApplyItemCount(root, itemCount);
        return root;
    }

    internal static void ApplyItemCount(Transform root, int itemCount)
    {
        if (root == null)
            return;

        float fullness = 1f +
            Mathf.Min(3, Mathf.Max(0, itemCount - 1)) * 0.08f;
        root.localScale = Vector3.one * fullness;
    }

    static void CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.layer = parent.gameObject.layer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Collider generatedCollider = part.GetComponent<Collider>();
        if (generatedCollider != null)
        {
            generatedCollider.enabled = false;
            Object.Destroy(generatedCollider);
        }

        Renderer targetRenderer = part.GetComponent<Renderer>();
        if (targetRenderer != null && material != null)
            targetRenderer.sharedMaterial = material;
    }

    static Material GetSackMaterial()
    {
        if (sackMaterial == null)
            sackMaterial = CreateMaterial(
                "Runtime Loot Bundle Sack",
                new Color(0.34f, 0.16f, 0.065f, 1f));
        return sackMaterial;
    }

    static Material GetTieMaterial()
    {
        if (tieMaterial == null)
            tieMaterial = CreateMaterial(
                "Runtime Loot Bundle Tie",
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
