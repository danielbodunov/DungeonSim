using System;
using UnityEngine;

/// <summary>A reusable physical-resource quote for any build transaction.</summary>
[Serializable]
public readonly struct BuildCost
{
    public PhysicalResourceCategory Category { get; }
    public int Amount { get; }

    public BuildCost(PhysicalResourceCategory category, int amount)
    {
        Category = category;
        Amount = Mathf.Max(0, amount);
    }

    public override string ToString() => $"{Amount} {GetDisplayName(Category)}";

    public static string GetDisplayName(PhysicalResourceCategory category) =>
        category == PhysicalResourceCategory.ConstructionMaterials
            ? "Construction Material"
            : category.ToString();
}
