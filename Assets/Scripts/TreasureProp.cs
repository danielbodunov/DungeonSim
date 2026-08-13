using System;
using UnityEngine;

/// <summary>Prototype treasure content composed with a dungeon POI.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DungeonPointOfInterest))]
public sealed class TreasureProp : MonoBehaviour, IDungeonPointOfInterestInteraction
{
    [SerializeField, Min(0)] int rewardValue = 10;
    [SerializeField] Color resolvedTint = new(0.35f, 0.35f, 0.35f, 1f);

    DungeonPointOfInterest pointOfInterest;
    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;

    public int RewardValue => rewardValue;
    public bool IsResolved => pointOfInterest != null && pointOfInterest.IsResolved;
    public DungeonPointOfInterest PointOfInterest => pointOfInterest;

    public event Action<TreasureProp> Resolved;

    void Awake()
    {
        pointOfInterest = GetComponent<DungeonPointOfInterest>();
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
        ApplyVisualState();
    }

    [ContextMenu("Resolve Treasure")]
    void ResolveFromInspector()
    {
        TryResolve();
    }


    public bool TryResolve()
    {
        if (pointOfInterest == null || pointOfInterest.IsResolved)
            return false;

        pointOfInterest.Resolve();
        ApplyVisualState();
        Resolved?.Invoke(this);
        return true;
    }

    public bool TryCompleteInvestigation(
        DungeonPointOfInterest investigatedPointOfInterest)
    {
        return investigatedPointOfInterest == pointOfInterest && TryResolve();
    }

    public void SetRewardValue(int value)
    {
        rewardValue = Mathf.Max(0, value);
    }

    void ApplyVisualState()
    {
        if (!IsResolved || renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null)
                continue;

            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", resolvedTint);
            propertyBlock.SetColor("_Color", resolvedTint);
            target.SetPropertyBlock(propertyBlock);
        }
    }
}
