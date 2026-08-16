using UnityEngine;

/// <summary>
/// World-space presentation and POI adapter for one authoritative recovery
/// record owned by <see cref="NPCTraversal"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DungeonPointOfInterest))]
public sealed class RecoverableLootWorldDrop : MonoBehaviour
{
    const string VisualRootName = "Loot Drop Bundle";
    const float InvestigationDuration = 1f;

    NPCTraversal recoveryOwner;
    string dropId;
    DungeonPointOfInterest pointOfInterest;

    public string DropId => dropId;
    public DungeonPointOfInterest PointOfInterest => pointOfInterest;
    public bool HasAuthoritativeContents => TryGetContents(out _);
    public int ItemCount => TryGetContents(out RecoverableLootDrop drop)
        ? drop.ItemCount
        : 0;
    public int TotalValue => TryGetContents(out RecoverableLootDrop drop)
        ? drop.TotalValue
        : 0;

    internal void Initialize(
        NPCTraversal owner,
        RecoverableLootDrop drop,
        TileGridGenerator grid)
    {
        recoveryOwner = owner;
        dropId = drop != null ? drop.DropId : string.Empty;
        pointOfInterest = GetComponent<DungeonPointOfInterest>();
        pointOfInterest.Configure(
            DungeonPointOfInterestType.RecoverableLoot,
            dropId,
            InvestigationDuration,
            transform,
            true);

        if (drop != null)
            pointOfInterest.Bind(grid, drop.DropCell);

        BuildVisual(drop != null ? drop.ItemCount : 0);
    }

    void OnEnable()
    {
        if (pointOfInterest == null)
            pointOfInterest = GetComponent<DungeonPointOfInterest>();
        if (recoveryOwner != null &&
            TryGetContents(out RecoverableLootDrop drop))
        {
            pointOfInterest.Bind(recoveryOwner.DungeonGrid, drop.DropCell);
        }
    }

    public bool TryGetContents(out RecoverableLootDrop drop)
    {
        if (recoveryOwner != null &&
            recoveryOwner.TryGetRecoverableLootDrop(dropId, out drop))
        {
            return true;
        }

        drop = null;
        return false;
    }

    /// <summary>
    /// Claims this drop through its authoritative owner. The world view is
    /// removed only after the record has been successfully claimed.
    /// </summary>
    public bool TryClaim(out RecoverableLootDrop claimedDrop)
    {
        if (recoveryOwner != null)
            return recoveryOwner.TryClaimRecoverableLoot(dropId, out claimedDrop);

        claimedDrop = null;
        return false;
    }

    void BuildVisual(int itemCount)
    {
        LootBundleVisualFactory.CreateBundle(
            transform,
            VisualRootName,
            gameObject.layer,
            new Vector3(0f, LootBundleVisualFactory.BundleSize * 0.5f, 0f),
            itemCount);
    }
}
