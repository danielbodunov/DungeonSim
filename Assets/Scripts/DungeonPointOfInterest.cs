using System;
using UnityEngine;

public enum DungeonPointOfInterestType
{
    Generic,
    Treasure,
    Shrine,
    Door
}

/// <summary>
/// Cell-bound gameplay marker for content an adventurer may investigate.
/// Visual feedback and rewards subscribe to its state changes separately.
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonPointOfInterest : MonoBehaviour
{
    [SerializeField] DungeonPointOfInterestType type;
    [SerializeField] string targetId;
    [SerializeField, Min(0f)] float investigationDuration = 1f;
    [SerializeField] Transform interactionPoint;
    [SerializeField] bool available = true;

    TileGridGenerator grid;
    Vector2Int cell;
    bool isBound;

    public DungeonPointOfInterestType Type => type;
    public string TargetId => targetId;
    public float InvestigationDuration => investigationDuration;
    public Vector3 InteractionPosition => interactionPoint != null
        ? interactionPoint.position
        : transform.position;
    public bool IsAvailable => available && isActiveAndEnabled && isBound;
    public bool IsResolved => !available;
    public bool IsBound => isBound;
    public Vector2Int Cell => cell;
    public TileGridGenerator Grid => grid;

    public event Action<DungeonPointOfInterest> AvailabilityChanged;

    void OnEnable()
    {
        TryBindToContainingGrid();
    }

    void Start()
    {
        // OnEnable may run before an instantiated tile has reached its final pose.
        if (!isBound)
            TryBindToContainingGrid();
    }

    void OnTransformParentChanged()
    {
        if (isActiveAndEnabled)
            TryBindToContainingGrid();
    }

    void OnDisable()
    {
        if (grid != null)
            grid.UnregisterPointOfInterest(this);
        grid = null;
        isBound = false;
    }

    public void SetAvailable(bool value)
    {
        if (available == value)
            return;

        available = value;
        AvailabilityChanged?.Invoke(this);
    }

    public void Resolve() => SetAvailable(false);

    public void ResetAvailability() => SetAvailable(true);

    internal void Bind(TileGridGenerator owningGrid, Vector2Int owningCell)
    {
        if (grid != null && (grid != owningGrid || cell != owningCell))
            grid.UnregisterPointOfInterest(this);

        grid = owningGrid;
        cell = owningCell;
        isBound = grid != null;
        grid?.RegisterPointOfInterest(this);
    }

    void TryBindToContainingGrid()
    {
        TileGridGenerator containingGrid = GetComponentInParent<TileGridGenerator>();
        if (containingGrid != null &&
            containingGrid.TryWorldToCell(transform.position, out Vector2Int owningCell) &&
            containingGrid.IsPlacedCell(owningCell.x, owningCell.y))
        {
            Bind(containingGrid, owningCell);
        }
    }
}
