using UnityEngine;

/// <summary>
/// Implemented by world objects that can be deliberately recovered through
/// the between-expedition player interaction path.
/// </summary>
public interface IPlayerRecoverableWorldObject
{
    bool TryRecoverByPlayer(
        GameplayLoopController recoveryAuthority,
        out string failure);
}

/// <summary>
/// World-space presentation and POI adapter for one authoritative recovery
/// record owned by <see cref="NPCTraversal"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DungeonPointOfInterest))]
public sealed class RecoverableLootWorldDrop :
    MonoBehaviour,
    IDungeonPointOfInterestInteraction,
    IPlayerRecoverableWorldObject
{
    const string VisualRootName = "Loot Drop Bundle";
    const string SelectionHighlightName = "Player Recovery Selection";
    const float InvestigationDuration = 1f;
    const int SelectionRingSegments = 40;

    NPCTraversal recoveryOwner;
    string dropId;
    DungeonPointOfInterest pointOfInterest;
    GameObject selectionHighlight;
    static Material selectionMaterial;

    public string DropId => dropId;
    public DungeonPointOfInterest PointOfInterest => pointOfInterest;
    public bool HasAuthoritativeContents => TryGetContents(out _);
    public int ItemCount => TryGetContents(out RecoverableLootDrop drop)
        ? drop.ItemCount
        : 0;
    public int TotalValue => TryGetContents(out RecoverableLootDrop drop)
        ? drop.TotalValue
        : 0;
    public bool PlayerSelected =>
        selectionHighlight != null && selectionHighlight.activeSelf;

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
        EnsurePlayerInteractionCollider();
        EnsureSelectionHighlight();
        selectionHighlight.SetActive(false);
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

    public bool TryCompleteInvestigation(
        DungeonPointOfInterest investigatedPointOfInterest,
        NPCTraversalAgent investigator)
    {
        return investigatedPointOfInterest == pointOfInterest &&
            investigator != null &&
            investigator.TryTakeRecoverableLoot(this);
    }

    public void SetPlayerSelected(bool selected)
    {
        EnsureSelectionHighlight();
        selectionHighlight.SetActive(selected);
    }

    public bool TryRecoverByPlayer(
        GameplayLoopController recoveryAuthority,
        out string failure)
    {
        if (recoveryAuthority == null)
        {
            failure = "No dungeon recovery authority is available.";
            return false;
        }

        return recoveryAuthority.TryRecoverLootDrop(
            dropId,
            out _,
            out failure);
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

    void EnsurePlayerInteractionCollider()
    {
        SphereCollider interactionCollider = GetComponent<SphereCollider>();
        if (interactionCollider == null)
            interactionCollider = gameObject.AddComponent<SphereCollider>();
        interactionCollider.isTrigger = true;
        interactionCollider.center = new Vector3(
            0f,
            LootBundleVisualFactory.BundleSize * 0.5f,
            0f);
        interactionCollider.radius = LootBundleVisualFactory.BundleSize * 2.5f;
    }

    void EnsureSelectionHighlight()
    {
        if (selectionHighlight != null)
            return;

        Transform existing = transform.Find(SelectionHighlightName);
        if (existing != null)
        {
            selectionHighlight = existing.gameObject;
            return;
        }

        selectionHighlight = new GameObject(SelectionHighlightName);
        selectionHighlight.layer = gameObject.layer;
        selectionHighlight.transform.SetParent(transform, false);
        selectionHighlight.transform.localPosition = new Vector3(
            0f,
            LootBundleVisualFactory.BundleSize * 0.5f,
            -0.04f);

        LineRenderer ring = selectionHighlight.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = SelectionRingSegments;
        ring.startWidth = 0.012f;
        ring.endWidth = 0.012f;
        ring.numCornerVertices = 2;
        ring.numCapVertices = 2;
        ring.sortingOrder = 25;
        ring.sharedMaterial = GetSelectionMaterial();
        float radius = LootBundleVisualFactory.BundleSize * 1.65f;
        for (int i = 0; i < SelectionRingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / SelectionRingSegments;
            ring.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
        }
    }

    static Material GetSelectionMaterial()
    {
        if (selectionMaterial != null)
            return selectionMaterial;

        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Standard");
        if (shader == null)
            return null;

        selectionMaterial = new Material(shader)
        {
            name = "Runtime Player Recovery Selection",
            color = new Color(1f, 0.72f, 0.12f, 1f),
            hideFlags = HideFlags.HideAndDontSave
        };
        if (selectionMaterial.HasProperty("_BaseColor"))
        {
            selectionMaterial.SetColor(
                "_BaseColor", new Color(1f, 0.72f, 0.12f, 1f));
        }
        return selectionMaterial;
    }
}
