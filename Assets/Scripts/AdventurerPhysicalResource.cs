using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Broad prototype categories used by physical build resources.</summary>
public enum PhysicalResourceCategory
{
    ConstructionMaterials,
    TrapComponents,
    ArcaneComponents
}

/// <summary>
/// Distinguishes resource stacks from treasure while both use the same
/// physical custody and recovery lifecycle.
/// </summary>
public enum RecoverableLootContentKind
{
    Treasure,
    PhysicalResource
}

/// <summary>
/// Configurable physical resource stack brought into a visit by an adventurer.
/// Unit value is balancing data only; it does not award Dread or currency.
/// </summary>
[Serializable]
public sealed class AdventurerResourcePayload
{
    [SerializeField] string resourceId;
    [SerializeField] PhysicalResourceCategory category;
    [SerializeField, Min(1)] int quantity = 1;
    [SerializeField, Min(0)] int unitValue;

    public string ResourceId => resourceId;
    public PhysicalResourceCategory Category => category;
    public int Quantity => Mathf.Max(1, quantity);
    public int UnitValue => Mathf.Max(0, unitValue);
    public int TotalValue => Quantity * UnitValue;
    public bool IsValid => !string.IsNullOrWhiteSpace(resourceId) && quantity > 0;

    public AdventurerResourcePayload(
        string resourceId,
        PhysicalResourceCategory category,
        int quantity,
        int unitValue)
    {
        this.resourceId = resourceId;
        this.category = category;
        this.quantity = Mathf.Max(1, quantity);
        this.unitValue = Mathf.Max(0, unitValue);
    }

    public AdventurerResourcePayload Copy()
    {
        return new AdventurerResourcePayload(
            resourceId,
            category,
            quantity,
            unitValue);
    }

    public static List<AdventurerResourcePayload> CopyAll(
        IReadOnlyList<AdventurerResourcePayload> source)
    {
        var result = new List<AdventurerResourcePayload>(source?.Count ?? 0);
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }
}

/// <summary>Two lightweight defaults for generated prototype adventurers.</summary>
public static class AdventurerResourceLoadouts
{
    public static List<AdventurerResourcePayload> CreatePrototypeLoadout(
        int configurationIndex,
        int adventurerLevel)
    {
        int level = Mathf.Max(1, adventurerLevel);
        if (configurationIndex % 2 == 0)
        {
            return new List<AdventurerResourcePayload>
            {
                new(
                    "construction-materials",
                    PhysicalResourceCategory.ConstructionMaterials,
                    1 + level,
                    2)
            };
        }

        return new List<AdventurerResourcePayload>
        {
            new(
                "trap-components",
                PhysicalResourceCategory.TrapComponents,
                level,
                4),
            new(
                "arcane-components",
                PhysicalResourceCategory.ArcaneComponents,
                1,
                6)
        };
    }
}
