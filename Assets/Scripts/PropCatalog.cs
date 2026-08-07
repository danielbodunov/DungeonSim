using System;
using System.Collections.Generic;
using UnityEngine;

public enum PropGenerationMode
{
    Single,
    Chained
}

[Serializable]
public class PropRolePrefab
{
    public PropSocketRole role = PropSocketRole.Single;
    public GameObject prefab;
}

[Serializable]
public class PropBundleItem
{
    public GameObject prefab;
    public Vector3 localPosition;
    public Vector3 localRotation;
}

[Serializable]
public class PropPieceBundle
{
    public PropSocketRole role = PropSocketRole.Single;
    public string bundleId = "Default";
    [NonReorderable]
    public List<PropBundleItem> items = new();
}

[Serializable]
public class PropLaneVariant
{
    public string laneId = "Default";
    [NonReorderable]
    public List<PropPieceBundle> bundles = new();

    public PropPieceBundle FindBundle(PropSocketRole role, string bundleId)
    {
        string requested = string.IsNullOrWhiteSpace(bundleId) ? "Default" : bundleId;
        foreach (PropPieceBundle bundle in bundles)
            if (bundle != null && bundle.role == role && string.Equals(
                bundle.bundleId, requested, StringComparison.OrdinalIgnoreCase))
                return bundle;

        if (!string.Equals(requested, "Default", StringComparison.OrdinalIgnoreCase))
            foreach (PropPieceBundle bundle in bundles)
                if (bundle != null && bundle.role == role && string.Equals(
                    bundle.bundleId, "Default", StringComparison.OrdinalIgnoreCase))
                    return bundle;
        return null;
    }
}

[Serializable]
public class PropDefinition
{
    public string structureId = "NewProp";
    public PropGenerationMode generationMode = PropGenerationMode.Single;
    [Range(0f, 1f)] public float spawnChance = 1f;
    public bool occupiesCell = true;
    public bool useSocketRotation = true;
    public Vector3 rotationOffset;
    [Tooltip("Lane-specific bundles. These take priority over the legacy one-prefab-per-role list below.")]
    [NonReorderable]
    public List<PropLaneVariant> laneVariants = new();
    [Tooltip("Legacy fallback used when no matching lane bundle is configured.")]
    [NonReorderable]
    public List<PropRolePrefab> prefabs = new();

    public PropPieceBundle GetBundle(
        string laneId, PropSocketRole role, string bundleId)
    {
        string requestedLane = string.IsNullOrWhiteSpace(laneId) ? "Default" : laneId;
        foreach (PropLaneVariant lane in laneVariants)
            if (lane != null && string.Equals(
                lane.laneId, requestedLane, StringComparison.OrdinalIgnoreCase))
                return lane.FindBundle(role, bundleId);

        if (!string.Equals(requestedLane, "Default", StringComparison.OrdinalIgnoreCase))
            foreach (PropLaneVariant lane in laneVariants)
                if (lane != null && string.Equals(
                    lane.laneId, "Default", StringComparison.OrdinalIgnoreCase))
                    return lane.FindBundle(role, bundleId);
        return null;
    }

    public GameObject GetPrefab(PropSocketRole role)
    {
        foreach (PropRolePrefab entry in prefabs)
            if (entry != null && entry.role == role)
                return entry.prefab;
        return null;
    }
}

[CreateAssetMenu(fileName = "PropCatalog", menuName = "Props/Prop Catalog")]
public class PropCatalog : ScriptableObject
{
    [NonReorderable]
    public List<PropDefinition> definitions = new();

    public PropDefinition Find(string structureId)
    {
        foreach (PropDefinition definition in definitions)
            if (definition != null && string.Equals(
                definition.structureId, structureId,
                StringComparison.OrdinalIgnoreCase))
                return definition;
        return null;
    }
}
