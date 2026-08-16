using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NPCDebugConnection
{
    public Vector3 from;
    public Vector3 to;
    public bool isLadder;
}

public readonly struct NPCTraversalConnection : System.IEquatable<NPCTraversalConnection>
{
    public readonly Vector2Int first;
    public readonly Vector2Int second;

    public NPCTraversalConnection(Vector2Int a, Vector2Int b)
    {
        if (a.x < b.x || (a.x == b.x && a.y <= b.y))
        {
            first = a;
            second = b;
        }
        else
        {
            first = b;
            second = a;
        }
    }

    public bool Equals(NPCTraversalConnection other) =>
        first == other.first && second == other.second;
    public override bool Equals(object obj) =>
        obj is NPCTraversalConnection other && Equals(other);
    public override int GetHashCode() => (first.GetHashCode() * 397) ^ second.GetHashCode();
}

public enum NPCTraversalAgentBehaviorState
{
    Inactive,
    Exploring,
    Moving,
    Investigating,
    PerformingTask,
    ReturningHome,
    Dead
}

/// <summary>
/// Builds runtime NPC routes from placed tile openings and generated ladders.
/// Add this beside TileGridGenerator and assign an NPC prefab.
/// </summary>
public class NPCTraversal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TileGridGenerator grid;
    [SerializeField] PropGenerator props;
    [SerializeField] GameObject npcPrefab;

    [Header("Spawn")]
    [SerializeField] bool useManualStartCell;
    [SerializeField] Vector2Int manualStartCell = new(1, 1);
    [SerializeField] Vector3 spawnOffset;
    [SerializeField, Min(0.1f), Tooltip("How far above the entrance anchor to begin looking for its floor.")]
    float spawnProbeHeight = 2f;
    [SerializeField, Min(0.1f), Tooltip("How far below a tall entrance room to search for a walkable floor.")]
    float spawnProbeDepth = 20f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] float moveSpeed = 2f;
    [SerializeField, Min(0.01f), Tooltip("Movement speed while climbing ladders. Defaults to 80% of Move Speed.")]
    float ladderSpeed = 1.6f;
    [SerializeField, Tooltip("World-space Z offset from the generated ladder anchor. Use this to place NPCs slightly in front of the visible ladder while climbing.")]
    float ladderTraversalZOffset = 0.3f;
    [SerializeField] bool patrolAutomatically = true;
    [SerializeField, Min(0f)] float waitAtDestination = 1f;

    [Header("Stamina Costs")]
    [SerializeField, Min(0f), Tooltip("Stamina spent per world unit walked.")]
    float movementStaminaCost = 0.5f;
    [SerializeField, Min(1f), Tooltip("Multiplier applied to movement stamina while climbing.")]
    float ladderStaminaMultiplier = 2f;
    [SerializeField, Min(0.01f), Tooltip("Stamina spent per second while performing an exploration task at a cell.")]
    float taskStaminaCostPerSecond = 0.5f;

    [Header("Walkable Surface Validation")]
    [SerializeField] LayerMask walkableLayers = ~0;
    [SerializeField, Range(2, 12)] int validationSamples = 5;
    [SerializeField, Min(0.01f)] float groundProbeHeight = 0.75f;
    [SerializeField, Min(0.01f)] float groundProbeDistance = 1.5f;
    [SerializeField, Min(0f), Tooltip("Checks slightly to either side of the NPC so narrow seams between colliders are treated as continuous ground.")]
    float groundProbeHalfWidth = 0.08f;
    [SerializeField, Min(0f), Tooltip("How far above the expected route height a valid floor may be. This prevents roof colliders from being selected.")]
    float maxSurfaceRise = 0.1f;
    [SerializeField, Min(0f)] float maxStepHeight = 0.3f;
    [SerializeField, Range(0f, 89f)] float maxWalkableSlope = 50f;
    [SerializeField, Min(0f)] float groundOffset;

    [Header("Fall Recovery")]
    [SerializeField, Min(0f), Tooltip("How long an NPC remains stunned after landing from an unintended fall.")]
    float fallRecoveryDelay = 0.75f;
    [SerializeField, Min(0.1f), Tooltip("Maximum distance below the NPC to search for a recovery floor.")]
    float fallRecoveryProbeDepth = 20f;
    [SerializeField, Min(0f), Tooltip("Falls shorter than this do not deal damage.")]
    float fallDamageFreeDistance = 0.5f;
    [SerializeField, Min(0f), Tooltip("Damage per world unit beyond the free fall distance.")]
    float fallDamagePerUnit = 1f;

    readonly Dictionary<Vector2Int, List<RouteEdge>> graph = new();
    readonly RaycastHit[] groundHits = new RaycastHit[16];
    readonly List<Vector3> debugWalkableSamples = new();
    readonly List<Vector3> debugRejectedSamples = new();
    DungeonEntrance activeEntrance;
    NPCTraversalAgent agent;
    readonly List<NPCTraversalAgent> agents = new();
    [SerializeField] List<RecoverableLootDrop> recoverableLootDrops = new();
    [SerializeField] List<AdventurerDeathLootOutcome> deathLootOutcomes = new();
    [SerializeField] List<AdventurerEscapeLootOutcome> successfulEscapeLootOutcomes = new();
    readonly Dictionary<string, RecoverableLootWorldDrop> recoverableLootWorldDrops =
        new();
    [SerializeField, HideInInspector] int nextRecoverableLootDropNumber = 1;
    [SerializeField, HideInInspector] int nextRuntimeAgentId = 1;
    int floorConnectionCount;
    int ladderConnectionCount;

    class RouteEdge
    {
        public Vector2Int destination;
        public List<Vector3> waypoints;
        public bool isLadder;
    }

    internal class RouteStep
    {
        public Vector2Int from;
        public Vector2Int to;
        public List<Vector3> waypoints;
    }

    public NPCTraversalAgent ActiveAgent => agent;
    public TileGridGenerator DungeonGrid => grid;
    public IReadOnlyList<NPCTraversalAgent> ActiveAgents => agents;
    public int ActiveAgentCount
    {
        get
        {
            PruneDestroyedAgents();
            return agents.Count;
        }
    }
    public IReadOnlyList<Vector3> DebugWalkableSamples => debugWalkableSamples;
    public IReadOnlyList<Vector3> DebugRejectedSamples => debugRejectedSamples;
    public DungeonEntrance ActiveEntrance => activeEntrance;
    public IReadOnlyList<RecoverableLootDrop> RecoverableLootDrops =>
        recoverableLootDrops;
    public IReadOnlyList<AdventurerDeathLootOutcome> DeathLootOutcomes =>
        deathLootOutcomes;
    public IReadOnlyList<AdventurerEscapeLootOutcome> SuccessfulEscapeLootOutcomes =>
        successfulEscapeLootOutcomes;
    public int DeathLootOutcomeCount => deathLootOutcomes.Count;
    public int SuccessfulEscapeLootOutcomeCount => successfulEscapeLootOutcomes.Count;
    public int RecoverableLootDropCount => recoverableLootDrops.Count;
    public int PhysicalRecoverableLootDropCount
    {
        get
        {
            PruneRecoverableLootWorldDrops();
            return recoverableLootWorldDrops.Count;
        }
    }
    public int RecoverableLootItemCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < recoverableLootDrops.Count; i++)
                if (recoverableLootDrops[i] != null)
                    total += recoverableLootDrops[i].ItemCount;
            return total;
        }
    }
    public int RecoverableLootValue
    {
        get
        {
            int total = 0;
            for (int i = 0; i < recoverableLootDrops.Count; i++)
                if (recoverableLootDrops[i] != null)
                    total += recoverableLootDrops[i].TotalValue;
            return total;
        }
    }
    public int EscapedDungeonLootItemCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < successfulEscapeLootOutcomes.Count; i++)
                if (successfulEscapeLootOutcomes[i] != null)
                    total += successfulEscapeLootOutcomes[i].EscapedItemCount;
            return total;
        }
    }
    public int EscapedDungeonLootValue
    {
        get
        {
            int total = 0;
            for (int i = 0; i < successfulEscapeLootOutcomes.Count; i++)
                if (successfulEscapeLootOutcomes[i] != null)
                    total += successfulEscapeLootOutcomes[i].EscapedValue;
            return total;
        }
    }
    internal float MaximumUnplannedDrop => maxStepHeight;
    internal float FallRecoveryDelay => fallRecoveryDelay;
    public event System.Action<NPCCharacter> AdventurerDied;
    public event System.Action<NPCTraversalAgent> AdventurerDefeated;
    public event System.Action<NPCTraversalAgent, Vector2Int, bool> AdventurerCellEntered;
    public event System.Func<NPCTraversalAgent, Vector2Int, bool> InvestigationDecisionRequested;
    public event System.Action<RecoverableLootDrop> RecoverableLootCreated;
    public event System.Action<RecoverableLootDrop> RecoverableLootClaimed;
    public event System.Action<AdventurerDeathLootOutcome> DeathLootOutcomeRecorded;
    public event System.Action<AdventurerEscapeLootOutcome> SuccessfulEscapeLootOutcomeRecorded;

    internal bool TryGetInvestigationTarget(
        NPCTraversalAgent visitor,
        Vector2Int cell,
        out DungeonPointOfInterest target)
    {
        if (grid != null && grid.TryGetAvailablePointOfInterest(cell, out target))
            return true;

        target = null;

        if (InvestigationDecisionRequested == null)
            return false;

        foreach (System.Func<NPCTraversalAgent, Vector2Int, bool> decision in
                 InvestigationDecisionRequested.GetInvocationList())
            if (decision(visitor, cell))
                return true;
        return false;
    }

    internal void NotifyAdventurerDied(NPCTraversalAgent visitor)
    {
        TryCreateRecoverableLootDrop(visitor);
        AdventurerDefeated?.Invoke(visitor);
        AdventurerDied?.Invoke(visitor != null ? visitor.Character : null);
    }

    internal void NotifyAdventurerEscaped(NPCTraversalAgent visitor)
    {
        if (visitor == null)
            return;

        int sourceRuntimeAgentId = visitor.RuntimeAgentId;
        if (!visitor.TryClaimSuccessfulEscapeLootFinalization())
        {
            RecordDuplicateSuccessfulEscapeLootAttempt(sourceRuntimeAgentId);
            return;
        }

        IReadOnlyList<CarriedDungeonTreasure> carried =
            visitor.CarriedDungeonTreasure;
        int carriedItemCountBefore = visitor.CarriedDungeonTreasureCount;
        int carriedValueBefore = visitor.CarriedDungeonTreasureValue;
        var escapedItems = new List<EscapedLootItem>(carried.Count);
        for (int i = 0; i < carried.Count; i++)
        {
            CarriedDungeonTreasure item = carried[i];
            if (item == null)
                continue;
            escapedItems.Add(new EscapedLootItem(
                item.TreasureId,
                item.Value,
                item.OriginatedAsDungeonBait
                    ? RecoverableLootOrigin.DungeonTreasure
                    : RecoverableLootOrigin.AdventurerPossession,
                item.SourceCell,
                item.OriginatedAsDungeonBait));
        }

        visitor.ClearCarriedLootAfterSuccessfulEscape();
        var outcome = new AdventurerEscapeLootOutcome(
            sourceRuntimeAgentId,
            visitor.Character != null
                ? visitor.Character.CharacterName
                : visitor.name,
            visitor.CurrentCell,
            visitor.transform.position,
            carriedItemCountBefore,
            carriedValueBefore,
            escapedItems,
            visitor.CarriedDungeonTreasureCount,
            visitor.CarriedDungeonTreasureValue);
        successfulEscapeLootOutcomes.Add(outcome);
        SuccessfulEscapeLootOutcomeRecorded?.Invoke(outcome);
    }

    bool TryCreateRecoverableLootDrop(NPCTraversalAgent visitor)
    {
        if (visitor == null)
            return false;
        int sourceRuntimeAgentId = visitor.RuntimeAgentId;
        if (!visitor.TryClaimDeathLootRecovery())
        {
            RecordDuplicateDeathLootAttempt(sourceRuntimeAgentId);
            return false;
        }

        IReadOnlyList<CarriedDungeonTreasure> carried =
            visitor.CarriedDungeonTreasure;
        int carriedItemCountBefore = visitor.CarriedDungeonTreasureCount;
        int carriedValueBefore = visitor.CarriedDungeonTreasureValue;
        Vector3 deathPosition = visitor.transform.position;
        Vector2Int deathCell = ResolveDeathCell(visitor, deathPosition);
        var recoveredItems = new List<RecoverableLootItem>(carried.Count);
        for (int i = 0; i < carried.Count; i++)
        {
            CarriedDungeonTreasure item = carried[i];
            if (item == null)
                continue;
            recoveredItems.Add(new RecoverableLootItem(
                item.TreasureId,
                item.Value,
                item.OriginatedAsDungeonBait
                    ? RecoverableLootOrigin.DungeonTreasure
                    : RecoverableLootOrigin.AdventurerPossession,
                item.SourceCell,
                item.OriginatedAsDungeonBait));
        }

        RecoverableLootDrop drop = null;
        if (recoveredItems.Count > 0)
        {
            string dropId = CreateUniqueRecoverableLootDropId();
            drop = new RecoverableLootDrop(
                dropId,
                deathCell,
                deathPosition,
                visitor.Character != null
                    ? visitor.Character.CharacterName
                    : visitor.name,
                recoveredItems);
            recoverableLootDrops.Add(drop);
            if (!MaterializeRecoverableLootDrop(drop))
            {
                Debug.LogWarning(
                    $"Recovery record '{drop.DropId}' was created, but its physical " +
                    $"drop could not be placed at cell {drop.DropCell}.",
                    this);
            }
        }

        visitor.ClearCarriedLootAfterDeath();
        var outcome = new AdventurerDeathLootOutcome(
            sourceRuntimeAgentId,
            visitor.Character != null
                ? visitor.Character.CharacterName
                : visitor.name,
            deathCell,
            deathPosition,
            carriedItemCountBefore,
            carriedValueBefore,
            recoveredItems.Count,
            drop != null ? drop.TotalValue : 0,
            drop != null ? drop.DropId : string.Empty,
            visitor.CarriedDungeonTreasureCount,
            visitor.CarriedDungeonTreasureValue);
        deathLootOutcomes.Add(outcome);
        if (drop != null)
            RecoverableLootCreated?.Invoke(drop);
        DeathLootOutcomeRecorded?.Invoke(outcome);
        return drop != null;
    }

    Vector2Int ResolveDeathCell(
        NPCTraversalAgent visitor,
        Vector3 deathPosition)
    {
        if (grid != null &&
            grid.TryWorldToCell(deathPosition, out Vector2Int positionCell) &&
            grid.IsPlacedCell(positionCell.x, positionCell.y))
        {
            return positionCell;
        }

        return visitor != null ? visitor.CurrentCell : default;
    }

    public bool TryGetRecoverableLootDrop(
        string dropId,
        out RecoverableLootDrop drop)
    {
        if (!string.IsNullOrWhiteSpace(dropId))
        {
            for (int i = 0; i < recoverableLootDrops.Count; i++)
            {
                RecoverableLootDrop candidate = recoverableLootDrops[i];
                if (candidate != null && candidate.DropId == dropId)
                {
                    drop = candidate;
                    return true;
                }
            }
        }

        drop = null;
        return false;
    }

    public bool TryGetRecoverableLootWorldDrop(
        string dropId,
        out RecoverableLootWorldDrop worldDrop)
    {
        PruneRecoverableLootWorldDrops();
        worldDrop = null;
        return !string.IsNullOrWhiteSpace(dropId) &&
            recoverableLootWorldDrops.TryGetValue(dropId, out worldDrop) &&
            worldDrop != null;
    }

    /// <summary>
    /// Removes one recovery record and its world presentation atomically from
    /// the dungeon side. Future consumers own what happens to the returned
    /// immutable snapshot.
    /// </summary>
    public bool TryClaimRecoverableLoot(
        string dropId,
        out RecoverableLootDrop claimedDrop)
    {
        for (int i = 0; i < recoverableLootDrops.Count; i++)
        {
            RecoverableLootDrop candidate = recoverableLootDrops[i];
            if (candidate == null || candidate.DropId != dropId)
                continue;

            recoverableLootDrops.RemoveAt(i);
            claimedDrop = candidate;
            DestroyRecoverableLootWorldDrop(dropId);
            RecoverableLootClaimed?.Invoke(claimedDrop);
            return true;
        }

        claimedDrop = null;
        return false;
    }

    public List<RecoverableLootDrop> CaptureRecoverableLootDrops()
    {
        var snapshot = new List<RecoverableLootDrop>(recoverableLootDrops.Count);
        for (int i = 0; i < recoverableLootDrops.Count; i++)
        {
            RecoverableLootDrop drop = recoverableLootDrops[i];
            if (drop != null && drop.ItemCount > 0)
                snapshot.Add(drop.Copy());
        }
        return snapshot;
    }

    public int RestoreRecoverableLootDrops(
        IReadOnlyList<RecoverableLootDrop> savedDrops)
    {
        ClearRecoverableLootDrops();
        if (savedDrops == null)
            return 0;

        var restoredIds = new HashSet<string>();
        for (int i = 0; i < savedDrops.Count; i++)
        {
            RecoverableLootDrop savedDrop = savedDrops[i];
            if (savedDrop == null ||
                string.IsNullOrWhiteSpace(savedDrop.DropId) ||
                savedDrop.ItemCount == 0 ||
                grid == null ||
                !grid.IsPlacedCell(savedDrop.DropCell.x, savedDrop.DropCell.y) ||
                !restoredIds.Add(savedDrop.DropId))
            {
                Debug.LogWarning(
                    $"Skipped invalid recoverable loot drop at save index {i}.",
                    this);
                continue;
            }

            RecoverableLootDrop restored = savedDrop.Copy();
            recoverableLootDrops.Add(restored);
            if (!MaterializeRecoverableLootDrop(restored))
            {
                recoverableLootDrops.RemoveAt(recoverableLootDrops.Count - 1);
                restoredIds.Remove(restored.DropId);
                Debug.LogWarning(
                    $"Could not restore physical loot drop '{restored.DropId}' at " +
                    $"cell {restored.DropCell}.",
                    this);
            }
        }

        return recoverableLootDrops.Count;
    }

    public void ClearRecoverableLootDrops()
    {
        recoverableLootDrops.Clear();
        foreach (RecoverableLootWorldDrop worldDrop in
                 recoverableLootWorldDrops.Values)
        {
            if (worldDrop == null)
                continue;
            worldDrop.gameObject.SetActive(false);
            Destroy(worldDrop.gameObject);
        }
        recoverableLootWorldDrops.Clear();
        nextRecoverableLootDropNumber = 1;
    }

    string CreateUniqueRecoverableLootDropId()
    {
        string candidate;
        do
        {
            candidate = $"recovered-loot-{nextRecoverableLootDropNumber:D4}";
            nextRecoverableLootDropNumber++;
        }
        while (TryGetRecoverableLootDrop(candidate, out _));
        return candidate;
    }

    bool MaterializeRecoverableLootDrop(RecoverableLootDrop drop)
    {
        if (drop == null || string.IsNullOrWhiteSpace(drop.DropId) ||
            drop.ItemCount == 0 || grid == null ||
            !grid.IsPlacedCell(drop.DropCell.x, drop.DropCell.y))
        {
            return false;
        }

        if (TryGetRecoverableLootWorldDrop(drop.DropId, out _))
            return true;

        var instance = new GameObject($"Recoverable Loot [{drop.DropId}]");
        instance.SetActive(false);
        instance.layer = gameObject.layer;
        instance.transform.SetParent(transform, true);
        instance.transform.SetPositionAndRotation(
            drop.WorldPosition,
            Quaternion.identity);
        instance.AddComponent<DungeonPointOfInterest>();
        var worldDrop = instance.AddComponent<RecoverableLootWorldDrop>();
        if (instance.GetComponentInChildren<DungeonLightReceiver>(true) == null)
            instance.AddComponent<DungeonLightReceiver>();
        worldDrop.Initialize(this, drop, grid);
        recoverableLootWorldDrops.Add(drop.DropId, worldDrop);
        instance.SetActive(true);
        return true;
    }

    void DestroyRecoverableLootWorldDrop(string dropId)
    {
        if (!recoverableLootWorldDrops.TryGetValue(dropId, out var worldDrop))
            return;

        recoverableLootWorldDrops.Remove(dropId);
        if (worldDrop != null)
        {
            worldDrop.gameObject.SetActive(false);
            Destroy(worldDrop.gameObject);
        }
    }

    void PruneRecoverableLootWorldDrops()
    {
        if (recoverableLootWorldDrops.Count == 0)
            return;

        var missingIds = new List<string>();
        foreach (KeyValuePair<string, RecoverableLootWorldDrop> pair in
                 recoverableLootWorldDrops)
        {
            if (pair.Value == null)
                missingIds.Add(pair.Key);
        }
        for (int i = 0; i < missingIds.Count; i++)
            recoverableLootWorldDrops.Remove(missingIds[i]);
    }

    void MaterializeExistingRecoverableLootDrops()
    {
        for (int i = 0; i < recoverableLootDrops.Count; i++)
            MaterializeRecoverableLootDrop(recoverableLootDrops[i]);
    }

    void RecordDuplicateDeathLootAttempt(int sourceRuntimeAgentId)
    {
        for (int i = deathLootOutcomes.Count - 1; i >= 0; i--)
        {
            AdventurerDeathLootOutcome outcome = deathLootOutcomes[i];
            if (outcome == null ||
                outcome.SourceRuntimeAgentId != sourceRuntimeAgentId)
            {
                continue;
            }

            outcome.RecordDuplicateProcessingAttempt();
            return;
        }
    }

    void RecordDuplicateSuccessfulEscapeLootAttempt(int sourceRuntimeAgentId)
    {
        for (int i = successfulEscapeLootOutcomes.Count - 1; i >= 0; i--)
        {
            AdventurerEscapeLootOutcome outcome = successfulEscapeLootOutcomes[i];
            if (outcome == null ||
                outcome.SourceRuntimeAgentId != sourceRuntimeAgentId)
            {
                continue;
            }

            outcome.RecordDuplicateProcessingAttempt();
            return;
        }
    }

    internal void NotifyCellEntered(
        NPCTraversalAgent visitor,
        Vector2Int cell,
        bool firstVisit)
    {
        if (visitor == null)
            return;

        AdventurerCellEntered?.Invoke(visitor, cell, firstVisit);
        grid?.NotifyNpcEnteredCell(visitor.Character, cell);
    }

    void Awake()
    {
        recoverableLootDrops ??= new List<RecoverableLootDrop>();
        deathLootOutcomes ??= new List<AdventurerDeathLootOutcome>();
        successfulEscapeLootOutcomes ??= new List<AdventurerEscapeLootOutcome>();
        nextRecoverableLootDropNumber = Mathf.Max(1, nextRecoverableLootDropNumber);
        nextRuntimeAgentId = Mathf.Max(1, nextRuntimeAgentId);
        if (grid == null)
            grid = GetComponent<TileGridGenerator>();
        if (props == null)
            props = GetComponent<PropGenerator>();
    }

    void OnEnable()
    {
        if (grid != null)
            grid.LayoutChanged += ResolveEntranceAfterLayoutChange;
        if (props != null)
            props.StructuresRegenerated += RebuildNavigation;
    }

    void Start()
    {
        // Also supports scenes where prop generation has already completed.
        if (props != null && props.StructureVersion > 0)
            RebuildNavigation();
        MaterializeExistingRecoverableLootDrops();
    }

    void OnDisable()
    {
        if (grid != null)
            grid.LayoutChanged -= ResolveEntranceAfterLayoutChange;
        if (props != null)
            props.StructuresRegenerated -= RebuildNavigation;
    }

    void ResolveEntranceAfterLayoutChange()
    {
        EnsureDefaultEntrance();
    }

    void RebuildNavigation()
    {
        BuildGraph();
        EnsureDefaultEntrance();
        PruneDestroyedAgents();
        for (int i = 0; i < agents.Count; i++)
            agents[i].RefreshNavigation(this);
    }

    void BuildGraph()
    {
        graph.Clear();
        debugWalkableSamples.Clear();
        debugRejectedSamples.Clear();
        floorConnectionCount = 0;
        ladderConnectionCount = 0;
        if (grid == null)
            return;

        Physics.SyncTransforms();

        for (int y = 0; y < grid.GridHeight; y++)
        for (int x = 0; x < grid.GridWidth; x++)
        {
            if (!grid.IsPlacedCell(x, y))
                continue;

            var cell = new Vector2Int(x, y);
            graph[cell] = new List<RouteEdge>();
            if (x > 0 && grid.IsPlacedCell(x - 1, y) &&
                grid.HasMatchingHorizontalEdge(x - 1, x, y) &&
                IsSurfaceRouteValid(
                    grid.GetCellWorldPosition(x - 1, y),
                    grid.GetCellWorldPosition(x, y)))
                AddTwoWayEdge(new Vector2Int(x - 1, y), cell);
        }

        if (props == null)
            return;

        foreach (GeneratedStructureRun run in props.GeneratedRuns)
        {
            if (!string.Equals(run.structureId, "Ladder", System.StringComparison.OrdinalIgnoreCase) ||
                run.traversalEndpoints == null)
                continue;

            // Every adjacent pair permits entry/exit at authored intermediate stops.
            for (int i = 1; i < run.traversalEndpoints.Count; i++)
                AddTwoWayLadderEdge(run.traversalEndpoints[i - 1], run.traversalEndpoints[i]);
        }

        Debug.Log(
            $"NPC navigation rebuilt: {graph.Count} placed cells, " +
            $"{floorConnectionCount} floor connections, " +
            $"{ladderConnectionCount} ladder connections.", this);
    }

    bool IsSurfaceRouteValid(Vector3 start, Vector3 end)
    {
        float? previousHeight = null;
        for (int i = 0; i < validationSamples; i++)
        {
            float t = validationSamples == 1 ? 0f : i / (float)(validationSamples - 1);
            Vector3 expected = Vector3.Lerp(start, end, t);
            if (!TryGetWalkableGround(expected, out RaycastHit hit))
            {
                debugRejectedSamples.Add(expected);
                return false;
            }

            debugWalkableSamples.Add(hit.point);

            if (previousHeight.HasValue &&
                Mathf.Abs(hit.point.y - previousHeight.Value) > maxStepHeight)
            {
                debugRejectedSamples.Add(hit.point);
                return false;
            }
            previousHeight = hit.point.y;
        }
        return true;
    }

    public bool TryGetWalkableGround(Vector3 expectedPosition, out RaycastHit hit)
    {
        hit = default;
        bool found = false;
        float closestHeightDifference = float.PositiveInfinity;
        int horizontalSamples = groundProbeHalfWidth > 0f ? 3 : 1;
        for (int sample = 0; sample < horizontalSamples; sample++)
        {
            float horizontalOffset = sample switch
            {
                1 => -groundProbeHalfWidth,
                2 => groundProbeHalfWidth,
                _ => 0f
            };
            Vector3 origin = expectedPosition +
                Vector3.right * horizontalOffset +
                Vector3.up * groundProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                groundProbeHeight + groundProbeDistance,
                walkableLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = groundHits[i];
                if (candidate.collider.GetComponentInParent<NPCTraversalAgent>() != null)
                    continue;

                float heightDifference = candidate.point.y - expectedPosition.y;
                if (heightDifference > maxSurfaceRise ||
                    heightDifference < -groundProbeDistance ||
                    Vector3.Angle(candidate.normal, Vector3.up) > maxWalkableSlope)
                    continue;

                float absoluteDifference = Mathf.Abs(heightDifference);
                if (absoluteDifference >= closestHeightDifference)
                    continue;

                closestHeightDifference = absoluteDifference;
                hit = candidate;
                found = true;
            }
        }

        return found;
    }

    public Vector3 GetGroundedPosition(Vector3 expectedPosition)
    {
        if (!TryGetWalkableGround(expectedPosition, out RaycastHit hit))
            return expectedPosition;
        expectedPosition.y = hit.point.y + groundOffset;
        return expectedPosition;
    }

    internal bool TryGetFallRecoveryLanding(
        Vector3 fallOrigin,
        out Vector2Int cell,
        out Vector3 landingPosition,
        out float fallDistance)
    {
        cell = default;
        landingPosition = fallOrigin;
        fallDistance = 0f;
        RaycastHit bestHit = default;
        bool found = false;
        float nearestDrop = float.PositiveInfinity;
        int horizontalSamples = groundProbeHalfWidth > 0f ? 3 : 1;
        for (int sample = 0; sample < horizontalSamples; sample++)
        {
            float horizontalOffset = sample switch
            {
                1 => -groundProbeHalfWidth,
                2 => groundProbeHalfWidth,
                _ => 0f
            };
            Vector3 origin = fallOrigin +
                Vector3.right * horizontalOffset +
                Vector3.up * groundProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                groundProbeHeight + fallRecoveryProbeDepth,
                walkableLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = groundHits[i];
                if (candidate.collider.GetComponentInParent<NPCTraversalAgent>() != null ||
                    candidate.point.y > fallOrigin.y + maxSurfaceRise ||
                    Vector3.Angle(candidate.normal, Vector3.up) > maxWalkableSlope)
                {
                    continue;
                }

                float drop = Mathf.Max(0f, fallOrigin.y - candidate.point.y);
                if (drop >= nearestDrop)
                    continue;
                nearestDrop = drop;
                bestHit = candidate;
                found = true;
            }
        }

        if (!found)
            return false;

        landingPosition = fallOrigin;
        landingPosition.y = bestHit.point.y + groundOffset;
        fallDistance = nearestDrop;
        if (grid.TryWorldToCell(landingPosition, out Vector2Int landedCell) &&
            graph.ContainsKey(landedCell))
        {
            cell = landedCell;
            return true;
        }

        return TryGetClosestCellAnchor(landingPosition, out cell, out _);
    }

    internal int CalculateFallDamage(float fallDistance)
    {
        float damagingDistance = Mathf.Max(
            0f, fallDistance - fallDamageFreeDistance);
        return damagingDistance > 0f
            ? Mathf.Max(1, Mathf.CeilToInt(damagingDistance * fallDamagePerUnit))
            : 0;
    }

    void AddTwoWayEdge(Vector2Int a, Vector2Int b)
    {
        if (!graph.ContainsKey(a) || !graph.ContainsKey(b))
            return;
        graph[a].Add(new RouteEdge { destination = b, waypoints = new List<Vector3> { grid.GetCellWorldPosition(b.x, b.y) } });
        graph[b].Add(new RouteEdge { destination = a, waypoints = new List<Vector3> { grid.GetCellWorldPosition(a.x, a.y) } });
        floorConnectionCount++;
    }

    void AddTwoWayLadderEdge(GeneratedTraversalEndpoint a, GeneratedTraversalEndpoint b)
    {
        if (!graph.ContainsKey(a.cell) || !graph.ContainsKey(b.cell))
            return;

        Vector3 aEntry = GetLadderEntryAtWalkingHeight(a);
        Vector3 bEntry = GetLadderEntryAtWalkingHeight(b);

        graph[a.cell].Add(new RouteEdge
        {
            destination = b.cell,
            // Stop at the ladder exit. If the route continues, its next edge
            // can lead straight toward that destination instead of snapping to
            // the center of this cell first.
            waypoints = new List<Vector3> { aEntry, bEntry },
            isLadder = true
        });
        graph[b.cell].Add(new RouteEdge
        {
            destination = a.cell,
            waypoints = new List<Vector3> { bEntry, aEntry },
            isLadder = true
        });
        ladderConnectionCount++;
    }

    Vector3 GetLadderEntryAtWalkingHeight(GeneratedTraversalEndpoint endpoint)
    {
        Vector3 expected = grid.GetCellWorldPosition(endpoint.cell.x, endpoint.cell.y);
        // Socket X/Z identifies the ladder lane. Its authored Y may sit at the
        // tile center, so floor probing supplies the actual entry height.
        expected.x = endpoint.worldPosition.x;
        expected.z = endpoint.worldPosition.z + ladderTraversalZOffset;
        return GetGroundedPosition(expected);
    }

    public NPCTraversalAgent SpawnAdventurer(
        NPCCharacterRecord characterRecord = null,
        bool beginVisit = true)
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning("NPCTraversal is ready, but no NPC prefab is assigned.", this);
            return null;
        }

        if (!TryFindSpawnPose(
            out Vector2Int start,
            out Vector3 spawnPosition,
            out Quaternion spawnRotation))
        {
            Debug.LogWarning(
                "NPCTraversal could not find a placed entrance cell with a walkable floor.",
                this);
            return null;
        }

        GameObject instance = Instantiate(npcPrefab, spawnPosition, spawnRotation);
        if (instance.GetComponentInChildren<DungeonLightReceiver>(true) == null)
            instance.AddComponent<DungeonLightReceiver>();
        NPCCharacter character = instance.GetComponent<NPCCharacter>();
        if (character == null)
            character = instance.AddComponent<NPCCharacter>();
        if (characterRecord != null)
            character.ApplyRecord(characterRecord);
        agent = instance.GetComponent<NPCTraversalAgent>();
        if (agent == null)
            agent = instance.AddComponent<NPCTraversalAgent>();
        agents.Add(agent);
        agent.Configure(
            this, start, spawnPosition, moveSpeed, ladderSpeed,
            patrolAutomatically, waitAtDestination,
            movementStaminaCost, ladderStaminaMultiplier,
            taskStaminaCostPerSecond,
            nextRuntimeAgentId++);
        if (instance.GetComponent<NPCCarriedLootVisual>() == null)
            instance.AddComponent<NPCCarriedLootVisual>();
        if (beginVisit)
            agent.BeginDungeonVisit();
        return agent;
    }

    public void DespawnAdventurer(NPCTraversalAgent visitor)
    {
        if (visitor == null)
            return;

        agents.Remove(visitor);
        if (agent == visitor)
            agent = agents.Count > 0 ? agents[agents.Count - 1] : null;
        Destroy(visitor.gameObject);
    }

    public void ClearAdventurers()
    {
        PruneDestroyedAgents();
        for (int i = 0; i < agents.Count; i++)
            if (agents[i] != null)
                Destroy(agents[i].gameObject);

        agents.Clear();
        agent = null;
    }

    void PruneDestroyedAgents()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
            if (agents[i] == null)
                agents.RemoveAt(i);

        if (agent == null && agents.Count > 0)
            agent = agents[agents.Count - 1];
    }

    bool TryFindSpawnPose(
        out Vector2Int start,
        out Vector3 position,
        out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        if (ResolveEntrance())
        {
            start = activeEntrance.Cell;
            if (!graph.ContainsKey(start))
            {
                Debug.LogWarning(
                    $"Dungeon entrance cell {start} is not part of the NPC navigation graph.",
                    activeEntrance);
                position = default;
                return false;
            }

            rotation = activeEntrance.EntryRotation;
            return TryGetEntranceFloorPosition(
                activeEntrance.EntryPosition, true, out position);
        }

        // Compatibility fallback for layouts that have not yet had a chance to
        // display their default semantic entrance.
        return TryFindLegacySpawnPose(out start, out position);
    }

    bool ResolveEntrance()
    {
        activeEntrance = null;
        return grid != null && grid.TryGetDungeonEntrance(out activeEntrance);
    }

    internal bool EnsureDefaultEntrance()
    {
        if (grid == null)
            return false;

        if (grid.HasManualEntrance)
            return ResolveEntrance();

        // A marker authored into the resolved layout already satisfies the
        // effective entrance contract. Existing fallbacks still flow through
        // the established selection path so layout changes can reposition them.
        if (!grid.HasFallbackEntrance && ResolveEntrance())
            return true;

        if (grid.PlacedCellCount == 0)
            return false;

        if (!TryFindLegacySpawnPose(out Vector2Int cell, out Vector3 position))
            return false;

        return grid.EnsureFallbackEntrance(cell, position) && ResolveEntrance();
    }

    bool TryFindLegacySpawnPose(out Vector2Int start, out Vector3 position)
    {
        if (useManualStartCell && graph.ContainsKey(manualStartCell) &&
            TryGetEntranceFloorPosition(manualStartCell, true, out position))
        {
            start = manualStartCell;
            return true;
        }

        var orderedCells = new List<Vector2Int>(graph.Keys);
        orderedCells.Sort((a, b) =>
        {
            Vector3 aWorld = grid.GetCellWorldPosition(a.x, a.y);
            Vector3 bWorld = grid.GetCellWorldPosition(b.x, b.y);
            int horizontal = aWorld.x.CompareTo(bWorld.x);
            return horizontal != 0
                ? horizontal
                : bWorld.y.CompareTo(aWorld.y);
        });

        return TryFindOrderedSpawn(orderedCells, true, false, out start, out position) ||
            TryFindOrderedSpawn(orderedCells, false, false, out start, out position) ||
            TryFindOrderedSpawn(orderedCells, true, true, out start, out position) ||
            TryFindOrderedSpawn(orderedCells, false, true, out start, out position);
    }

    bool TryFindOrderedSpawn(
        List<Vector2Int> orderedCells,
        bool requireConnection,
        bool allowDeepFloor,
        out Vector2Int start,
        out Vector3 position)
    {
        for (int i = 0; i < orderedCells.Count; i++)
        {
            Vector2Int cell = orderedCells[i];
            if (requireConnection && graph[cell].Count == 0)
                continue;
            if (TryGetEntranceFloorPosition(cell, allowDeepFloor, out position))
            {
                start = cell;
                return true;
            }
        }

        start = new Vector2Int(-1, -1);
        position = default;
        return false;
    }

    bool TryGetEntranceFloorPosition(
        Vector2Int cell, bool allowDeepFloor, out Vector3 position)
    {
        Vector3 anchor = grid.GetCellWorldPosition(cell.x, cell.y);
        return TryGetEntranceFloorPosition(anchor, allowDeepFloor, out position);
    }

    bool TryGetEntranceFloorPosition(
        Vector3 anchor, bool allowDeepFloor, out Vector3 position)
    {
        anchor.x += spawnOffset.x;
        anchor.z += spawnOffset.z;

        if (!allowDeepFloor)
        {
            position = anchor;
            if (!TryGetWalkableGround(anchor, out RaycastHit nearbyHit))
                return false;
            position.y = nearbyHit.point.y + groundOffset + spawnOffset.y;
            return true;
        }

        Vector3 rayOrigin = anchor + Vector3.up * spawnProbeHeight;
        int hitCount = Physics.RaycastNonAlloc(
            rayOrigin,
            Vector3.down,
            groundHits,
            spawnProbeHeight + spawnProbeDepth,
            walkableLayers,
            QueryTriggerInteraction.Ignore);

        RaycastHit bestHit = default;
        bool found = false;
        float closestHeightDifference = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = groundHits[i];
            float heightDifference = candidate.point.y - anchor.y;
            if (heightDifference > maxSurfaceRise ||
                heightDifference < -spawnProbeDepth ||
                Vector3.Angle(candidate.normal, Vector3.up) > maxWalkableSlope)
                continue;

            float absoluteDifference = Mathf.Abs(heightDifference);
            if (absoluteDifference >= closestHeightDifference)
                continue;

            closestHeightDifference = absoluteDifference;
            bestHit = candidate;
            found = true;
        }

        position = anchor;
        if (!found)
            return false;

        position.y = bestHit.point.y + groundOffset + spawnOffset.y;
        return true;
    }

    public List<Vector3> FindRoute(Vector2Int start, Vector2Int destination)
    {
        List<RouteStep> steps = FindRouteSteps(start, destination);
        if (steps == null)
            return null;

        var route = new List<Vector3>();
        foreach (RouteStep step in steps)
            route.AddRange(step.waypoints);
        return route;
    }

    internal List<RouteStep> FindRouteSteps(
        Vector2Int start,
        Vector2Int destination,
        HashSet<NPCTraversalConnection> allowedConnections = null)
    {
        if (!graph.ContainsKey(start) || !graph.ContainsKey(destination))
            return null;

        var queue = new Queue<Vector2Int>();
        var previousCell = new Dictionary<Vector2Int, Vector2Int>();
        var previousEdge = new Dictionary<Vector2Int, RouteEdge>();
        queue.Enqueue(start);
        previousCell[start] = start;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == destination)
                break;
            foreach (RouteEdge edge in graph[current])
            {
                if (allowedConnections != null &&
                    !allowedConnections.Contains(
                        new NPCTraversalConnection(current, edge.destination)))
                    continue;
                if (previousCell.ContainsKey(edge.destination))
                    continue;
                previousCell[edge.destination] = current;
                previousEdge[edge.destination] = edge;
                queue.Enqueue(edge.destination);
            }
        }

        if (!previousCell.ContainsKey(destination))
            return null;

        var steps = new List<RouteStep>();
        Vector2Int to = destination;
        while (to != start)
        {
            Vector2Int from = previousCell[to];
            RouteEdge edge = previousEdge[to];
            steps.Add(new RouteStep { from = from, to = to, waypoints = edge.waypoints });
            to = from;
        }
        steps.Reverse();
        return steps;
    }

    public bool TryGetRandomReachableCell(Vector2Int start, out Vector2Int result)
    {
        result = start;
        if (!graph.ContainsKey(start))
            return false;

        var reachable = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (RouteEdge edge in graph[current])
                if (visited.Add(edge.destination))
                {
                    reachable.Add(edge.destination);
                    queue.Enqueue(edge.destination);
                }
        }

        if (reachable.Count == 0)
            return false;
        result = reachable[Random.Range(0, reachable.Count)];
        return true;
    }

    /// <summary>Finds the closest reachable cell not yet explored by this agent.</summary>
    public bool TryGetNearestUnvisitedCell(
        Vector2Int start,
        IReadOnlyCollection<Vector2Int> visited,
        out Vector2Int result)
    {
        result = start;
        if (!graph.ContainsKey(start))
            return false;

        var visitedLookup = visited as HashSet<Vector2Int>
            ?? new HashSet<Vector2Int>(visited);
        var searched = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (RouteEdge edge in graph[current])
            {
                if (!searched.Add(edge.destination))
                    continue;
                if (!visitedLookup.Contains(edge.destination))
                {
                    result = edge.destination;
                    return true;
                }
                queue.Enqueue(edge.destination);
            }
        }
        return false;
    }

    public bool TryGetClosestCellAnchor(
        Vector3 worldPosition, out Vector2Int cell, out Vector3 anchor)
    {
        cell = default;
        anchor = worldPosition;
        float closestDistance = float.PositiveInfinity;
        bool found = false;
        foreach (Vector2Int candidate in graph.Keys)
        {
            Vector3 candidateAnchor = grid.GetCellWorldPosition(candidate.x, candidate.y);
            float distance = (candidateAnchor - worldPosition).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            cell = candidate;
            anchor = candidateAnchor;
            found = true;
        }
        return found;
    }

    public void GetDebugConnections(List<NPCDebugConnection> results)
    {
        results.Clear();
        var emitted = new HashSet<string>();
        foreach (var pair in graph)
        foreach (RouteEdge edge in pair.Value)
        {
            string key = pair.Key.x < edge.destination.x ||
                (pair.Key.x == edge.destination.x && pair.Key.y < edge.destination.y)
                ? $"{pair.Key}:{edge.destination}:{edge.isLadder}"
                : $"{edge.destination}:{pair.Key}:{edge.isLadder}";
            if (!emitted.Add(key))
                continue;

            results.Add(new NPCDebugConnection
            {
                from = grid.GetCellWorldPosition(pair.Key.x, pair.Key.y),
                to = grid.GetCellWorldPosition(edge.destination.x, edge.destination.y),
                isLadder = edge.isLadder
            });
        }
    }

    internal Vector3 GetCellWorldPosition(Vector2Int cell) =>
        grid.GetCellWorldPosition(cell.x, cell.y);
}

public class NPCTraversalAgent : MonoBehaviour, ICarriedLootPresentationSource
{
    NPCTraversal navigation;
    NPCCharacter character;
    Vector2Int startCell;
    Vector2Int currentCell;
    Vector3 homePosition;
    float speed;
    float climbSpeed;
    bool autoPatrol;
    float waitTime;
    Coroutine movement;
    List<Vector3> activeRoute;
    int nextWaypointIndex = -1;
    readonly HashSet<Vector2Int> visitedCells = new();
    readonly HashSet<NPCTraversalConnection> familiarConnections = new();
    [SerializeField] List<CarriedDungeonTreasure> carriedDungeonTreasure = new();
    float movementStaminaCost;
    float ladderStaminaMultiplier;
    float taskStaminaCostPerSecond;
    bool returningHome;
    bool visitInProgress;
    DungeonPointOfInterest activeInvestigationTarget;
    float investigationTimeRemaining;
    bool isInvestigating;
    bool performingTask;
    bool deathLootRecoveryClaimed;
    bool successfulEscapeLootFinalizationClaimed;
    bool retreatFinalizationClaimed;
    bool diedDuringDungeonVisit;
    int runtimeAgentId;

    public IReadOnlyList<Vector3> ActiveRoute => activeRoute;
    public int NextWaypointIndex => nextWaypointIndex;
    public bool HasNextWaypoint => activeRoute != null &&
        nextWaypointIndex >= 0 && nextWaypointIndex < activeRoute.Count;
    public Vector3 NextWaypoint => HasNextWaypoint
        ? activeRoute[nextWaypointIndex]
        : transform.position;
    public NPCCharacter Character => character;
    public NPCTraversal Navigation => navigation;
    public int RuntimeAgentId => runtimeAgentId;
    public Vector2Int StartCell => startCell;
    public Vector2Int CurrentCell => currentCell;
    public Vector3 HomePosition => homePosition;
    public IReadOnlyCollection<Vector2Int> VisitedCells => visitedCells;
    public IReadOnlyCollection<NPCTraversalConnection> FamiliarConnections => familiarConnections;
    public IReadOnlyList<CarriedDungeonTreasure> CarriedDungeonTreasure =>
        carriedDungeonTreasure;
    public int CarriedDungeonTreasureCount => carriedDungeonTreasure.Count;
    public int CarriedLootPresentationItemCount =>
        carriedDungeonTreasure != null ? carriedDungeonTreasure.Count : 0;
    public int CarriedDungeonTreasureValue
    {
        get
        {
            int total = 0;
            for (int i = 0; i < carriedDungeonTreasure.Count; i++)
                if (carriedDungeonTreasure[i] != null)
                    total += carriedDungeonTreasure[i].Value;
            return total;
        }
    }
    public float RemainingStamina => character != null ? character.CurrentStamina : 0f;
    public bool IsReturningHome => returningHome;
    public bool VisitInProgress => visitInProgress;
    public bool DeathLootRecoveryProcessed => deathLootRecoveryClaimed;
    public bool SuccessfulEscapeLootFinalizationProcessed =>
        successfulEscapeLootFinalizationClaimed;
    public bool RetreatFinalizationProcessed => retreatFinalizationClaimed;
    public bool DiedDuringDungeonVisit => diedDuringDungeonVisit;
    public DungeonPointOfInterest ActiveInvestigationTarget => activeInvestigationTarget;
    public float InvestigationTimeRemaining => investigationTimeRemaining;
    public bool IsInvestigating => isInvestigating;
    public NPCTraversalAgentBehaviorState BehaviorState
    {
        get
        {
            if (character != null && character.IsDead)
                return NPCTraversalAgentBehaviorState.Dead;
            if (!visitInProgress)
                return NPCTraversalAgentBehaviorState.Inactive;
            if (returningHome)
                return NPCTraversalAgentBehaviorState.ReturningHome;
            if (isInvestigating)
                return NPCTraversalAgentBehaviorState.Investigating;
            if (performingTask)
                return NPCTraversalAgentBehaviorState.PerformingTask;
            if (movement != null)
                return NPCTraversalAgentBehaviorState.Moving;
            return NPCTraversalAgentBehaviorState.Exploring;
        }
    }

    /// <summary>The bool is true when this is the cell's first visit this round.</summary>
    public event System.Action<NPCTraversalAgent, Vector2Int, bool> CellEntered;
    public event System.Action<NPCTraversalAgent> DungeonVisitCompleted;
    public event System.Action CarriedLootPresentationChanged;

    public void Configure(
        NPCTraversal owner,
        Vector2Int startCell,
        Vector3 entrancePosition,
        float moveSpeed,
        float ladderMoveSpeed,
        bool patrol,
        float wait,
        float staminaCostPerUnit,
        float ladderCostMultiplier,
        float taskCostPerSecond,
        int assignedRuntimeAgentId)
    {
        navigation = owner;
        this.startCell = startCell;
        currentCell = startCell;
        homePosition = entrancePosition;
        speed = moveSpeed;
        climbSpeed = ladderMoveSpeed;
        autoPatrol = patrol;
        waitTime = wait;
        movementStaminaCost = staminaCostPerUnit;
        ladderStaminaMultiplier = ladderCostMultiplier;
        taskStaminaCostPerSecond = taskCostPerSecond;
        runtimeAgentId = Mathf.Max(1, assignedRuntimeAgentId);
        carriedDungeonTreasure ??= new List<CarriedDungeonTreasure>();
        deathLootRecoveryClaimed = false;
        successfulEscapeLootFinalizationClaimed = false;
        retreatFinalizationClaimed = false;
        diedDuringDungeonVisit = false;
        character = GetComponent<NPCCharacter>();
        if (character == null)
            character = gameObject.AddComponent<NPCCharacter>();
        character.Died += OnCharacterDied;
    }

    void OnDestroy()
    {
        if (character != null)
            character.Died -= OnCharacterDied;
    }

    internal bool TryClaimDeathLootRecovery()
    {
        if (deathLootRecoveryClaimed || successfulEscapeLootFinalizationClaimed ||
            retreatFinalizationClaimed)
            return false;
        deathLootRecoveryClaimed = true;
        return true;
    }

    internal bool TryClaimSuccessfulEscapeLootFinalization()
    {
        if (successfulEscapeLootFinalizationClaimed || deathLootRecoveryClaimed ||
            retreatFinalizationClaimed)
            return false;
        successfulEscapeLootFinalizationClaimed = true;
        return true;
    }

    internal void ClearCarriedLootAfterDeath()
    {
        ClearCarriedLoot();
    }

    internal void ClearCarriedLootAfterSuccessfulEscape()
    {
        ClearCarriedLoot();
    }

    /// <summary>
    /// Finalizes a still-active visit removed by phase/session cleanup. This is
    /// mutually exclusive with both entrance escape and defeat processing.
    /// </summary>
    internal bool TryFinalizeForcedRetreat()
    {
        if (!visitInProgress || retreatFinalizationClaimed ||
            deathLootRecoveryClaimed || successfulEscapeLootFinalizationClaimed)
        {
            return false;
        }

        retreatFinalizationClaimed = true;
        if (movement != null)
            StopCoroutine(movement);
        movement = null;
        activeRoute = null;
        nextWaypointIndex = -1;
        returningHome = false;
        visitInProgress = false;
        ClearActiveActivityState();
        ClearCarriedLoot();
        return true;
    }

    void OnCharacterDied(NPCCharacter deadCharacter)
    {
        if (retreatFinalizationClaimed)
        {
            diedDuringDungeonVisit = false;
            return;
        }

        diedDuringDungeonVisit = visitInProgress;
        if (movement != null)
            StopCoroutine(movement);
        movement = null;
        ClearActiveActivityState();
        visitInProgress = false;
        navigation?.NotifyAdventurerDied(this);
        navigation?.DespawnAdventurer(this);
    }

    public void RefreshNavigation(NPCTraversal owner)
    {
        navigation = owner;
        if (movement != null)
        {
            StopCoroutine(movement);
            movement = null;
        }

        activeRoute = null;
        nextWaypointIndex = -1;
        ClearActiveActivityState();
        if (!navigation.TryGetClosestCellAnchor(
            transform.position, out currentCell, out Vector3 anchor))
            return;

        // A tile or generated prop may have changed directly beneath the NPC.
        // Re-anchor it to stable cell ground before planning against the new graph.
        transform.position = navigation.GetGroundedPosition(anchor);
        // Editing can invalidate the active route. Keep this visit's memory and
        // resume exploration from the nearest surviving cell.
        if (visitInProgress)
            StartNextExplorationStep();
        else if (autoPatrol)
            BeginDungeonVisit();
    }

    /// <summary>Starts a fresh exploration round for this character.</summary>
    public bool BeginDungeonVisit()
    {
        if (navigation == null || movement != null || visitInProgress)
            return false;

        visitedCells.Clear();
        familiarConnections.Clear();
        ClearCarriedLoot();
        deathLootRecoveryClaimed = false;
        successfulEscapeLootFinalizationClaimed = false;
        retreatFinalizationClaimed = false;
        diedDuringDungeonVisit = false;
        if (character == null)
            return false;
        character.ResetVisitResources();
        returningHome = false;
        ClearActiveActivityState();
        visitInProgress = true;
        RecordArrival(currentCell);
        StartNextExplorationStep();
        return true;
    }

    public bool MoveToCell(Vector2Int destination)
    {
        return MoveToCell(destination, false);
    }

    bool MoveToCell(Vector2Int destination, bool familiarOnly)
    {
        if (navigation == null)
            return false;
        List<NPCTraversal.RouteStep> steps = navigation.FindRouteSteps(
            currentCell,
            destination,
            familiarOnly ? familiarConnections : null);
        if (steps == null)
            return false;
        if (movement != null)
            StopCoroutine(movement);
        activeRoute = new List<Vector3>();
        foreach (NPCTraversal.RouteStep step in steps)
            activeRoute.AddRange(step.waypoints);
        if (familiarOnly && destination == startCell)
            activeRoute.Add(homePosition);
        nextWaypointIndex = activeRoute.Count > 0 ? 0 : -1;
        movement = StartCoroutine(FollowRoute(steps));
        return true;
    }

    IEnumerator FollowRoute(List<NPCTraversal.RouteStep> steps)
    {
        int waypointIndex = 0;
        foreach (NPCTraversal.RouteStep step in steps)
        {
            for (int i = 0; i < step.waypoints.Count; i++, waypointIndex++)
            {
                nextWaypointIndex = waypointIndex;
                Vector3 waypoint = step.waypoints[i];
                Vector3 segmentStart = transform.position;
                bool isLadderSegment = Mathf.Abs(waypoint.y - segmentStart.y) > 0.25f &&
                    Mathf.Abs(waypoint.x - segmentStart.x) < 0.15f;
                Vector3 target = isLadderSegment
                    ? waypoint
                    : navigation.GetGroundedPosition(waypoint);
                while ((transform.position - target).sqrMagnitude > 0.0001f)
                {
                    float segmentSpeed = isLadderSegment ? climbSpeed : speed;
                    Vector3 next = Vector3.MoveTowards(
                        transform.position,
                        target,
                        segmentSpeed * DungeonSimulationState.DeltaTime);
                    if (!isLadderSegment)
                    {
                        Vector3 groundedNext = navigation.GetGroundedPosition(next);
                        if (transform.position.y - groundedNext.y >
                            navigation.MaximumUnplannedDrop)
                        {
                            yield return RecoverFromUnexpectedFall(
                                next, transform.position.y);
                            yield break;
                        }
                        next = groundedNext;
                    }
                    float distanceMoved = Vector3.Distance(transform.position, next);
                    transform.position = next;
                    if (!returningHome && character != null)
                    {
                        float multiplier = isLadderSegment ? ladderStaminaMultiplier : 1f;
                        character.SpendStamina(distanceMoved * movementStaminaCost * multiplier);
                    }
                    yield return null;
                }
            }

            if (DungeonSimulationState.IsPaused)
                yield return DungeonSimulationState.WaitUntilRunning();
            familiarConnections.Add(new NPCTraversalConnection(step.from, step.to));
            currentCell = step.to;
            RecordArrival(currentCell);

            if (!returningHome && character.CurrentStamina > 0f &&
                navigation.TryGetInvestigationTarget(
                    this, currentCell, out DungeonPointOfInterest investigationTarget))
            {
                activeInvestigationTarget = investigationTarget;
                isInvestigating = true;
                float investigationDuration = investigationTarget != null
                    ? investigationTarget.InvestigationDuration
                    : waitTime;
                investigationTimeRemaining = investigationDuration;
                bool investigationCompleted = investigationDuration <= 0f;
                if (investigationDuration > 0f)
                {
                    yield return SpendStaminaWhileWaiting(
                        investigationDuration,
                        completed => investigationCompleted = completed);
                }

                if (DungeonSimulationState.IsPaused)
                    yield return DungeonSimulationState.WaitUntilRunning();
                if (investigationCompleted && investigationTarget != null &&
                    investigationTarget.IsAvailable &&
                    investigationTarget.Cell == currentCell)
                {
                    investigationTarget.TryCompleteInvestigation(this);
                }
                activeInvestigationTarget = null;
                investigationTimeRemaining = 0f;
                isInvestigating = false;
            }
        }

        if (returningHome && currentCell == startCell)
        {
            nextWaypointIndex = Mathf.Max(0, activeRoute.Count - 1);
            yield return MoveToHomePosition();
        }

        activeRoute = null;
        nextWaypointIndex = -1;
        movement = null;

        if (returningHome && currentCell == startCell)
        {
            CompleteDungeonVisit();
            yield break;
        }

        StartNextExplorationStep();
    }

    IEnumerator RecoverFromUnexpectedFall(
        Vector3 fallPosition,
        float fallStartHeight)
    {
        if (DungeonSimulationState.IsPaused)
            yield return DungeonSimulationState.WaitUntilRunning();
        activeRoute = null;
        nextWaypointIndex = -1;

        Vector3 fallOrigin = fallPosition;
        fallOrigin.y = fallStartHeight;
        if (navigation.TryGetFallRecoveryLanding(
            fallOrigin,
            out Vector2Int landedCell,
            out Vector3 landingPosition,
            out float fallDistance))
        {
            transform.position = landingPosition;
            currentCell = landedCell;
            int damage = navigation.CalculateFallDamage(fallDistance);
            if (damage > 0 && character != null && !character.IsDead)
            {
                NPCActionResolver.ResolveDamage(
                    character,
                    this,
                    damage,
                    transform.position + Vector3.up * 0.35f);
            }
        }

        if (character == null || character.IsDead)
            yield break;

        if (navigation.FallRecoveryDelay > 0f)
        {
            yield return DungeonSimulationState.WaitForSimulationSeconds(
                navigation.FallRecoveryDelay);
        }

        if (DungeonSimulationState.IsPaused)
            yield return DungeonSimulationState.WaitUntilRunning();
        movement = null;
        if (!visitInProgress || character == null || character.IsDead)
            yield break;

        RecordArrival(currentCell);
        StartNextExplorationStep();
    }

    public bool TryTakeTreasure(TreasureProp treasure)
    {
        if (!visitInProgress || treasure == null || treasure.IsResolved)
            return false;

        DungeonPointOfInterest pointOfInterest = treasure.PointOfInterest;
        if (pointOfInterest == null || !pointOfInterest.IsAvailable ||
            pointOfInterest.Cell != currentCell || !treasure.TryResolve())
        {
            return false;
        }

        string treasureId = string.IsNullOrWhiteSpace(pointOfInterest.TargetId)
            ? treasure.name
            : pointOfInterest.TargetId;
        carriedDungeonTreasure.Add(new CarriedDungeonTreasure(
            treasureId,
            treasure.RewardValue,
            pointOfInterest.Cell,
            true));
        CarriedLootPresentationChanged?.Invoke();
        return true;
    }

    void ClearCarriedLoot()
    {
        if (carriedDungeonTreasure == null || carriedDungeonTreasure.Count == 0)
            return;

        carriedDungeonTreasure.Clear();
        CarriedLootPresentationChanged?.Invoke();
    }

    void RecordArrival(Vector2Int cell)
    {
        if (!visitInProgress)
            return;

        if (character == null || character.IsDead)
            return;

        bool firstVisit = visitedCells.Add(cell);
        if (firstVisit)
            character.RecordCellExplored();
        navigation?.NotifyCellEntered(this, cell, firstVisit);
        if (character.IsDead)
            return;
        CellEntered?.Invoke(this, cell, firstVisit);
    }

    void StartNextExplorationStep()
    {
        if (!visitInProgress || navigation == null || movement != null)
            return;

        if (!returningHome && character.CurrentStamina > 0f &&
            navigation.TryGetNearestUnvisitedCell(
                currentCell, visitedCells, out Vector2Int destination))
        {
            MoveToCell(destination);
            return;
        }

        // Once all reachable cells are known, keep roaming and doing tasks until
        // the visit's stamina is genuinely exhausted.
        if (!returningHome && character.CurrentStamina > 0f)
        {
            if (navigation.TryGetRandomReachableCell(currentCell, out Vector2Int roamCell))
            {
                MoveToCell(roamCell);
                return;
            }

            movement = StartCoroutine(PerformTaskUntilExhausted());
            return;
        }

        returningHome = true;
        TryStartReturnMovement();
    }

    /// <summary>Stops the current activity and requests the normal familiar route home.</summary>
    public bool TryForceReturnHome()
    {
        if (!visitInProgress || navigation == null || character == null || character.IsDead)
            return false;

        if (movement != null)
            StopCoroutine(movement);
        movement = null;
        activeRoute = null;
        nextWaypointIndex = -1;
        ClearActiveActivityState();
        returningHome = true;
        return TryStartReturnMovement();
    }

    bool TryStartReturnMovement()
    {
        if (currentCell != startCell)
            return MoveToCell(startCell, true);

        // Already home. Only reaching the entrance position completes the visit.
        movement = StartCoroutine(ReturnFromEntranceCell());
        return true;
    }

    IEnumerator ReturnFromEntranceCell()
    {
        activeRoute = new List<Vector3> { homePosition };
        nextWaypointIndex = 0;
        yield return MoveToHomePosition();
        if (DungeonSimulationState.IsPaused)
            yield return DungeonSimulationState.WaitUntilRunning();
        activeRoute = null;
        nextWaypointIndex = -1;
        movement = null;
        CompleteDungeonVisit();
    }

    IEnumerator MoveToHomePosition()
    {
        while ((transform.position - homePosition).sqrMagnitude > 0.0001f)
        {
            Vector3 next = Vector3.MoveTowards(
                transform.position,
                homePosition,
                speed * DungeonSimulationState.DeltaTime);
            transform.position = navigation.GetGroundedPosition(next);
            yield return null;
        }
    }

    IEnumerator SpendStaminaWhileWaiting(
        float duration,
        System.Action<bool> completion = null)
    {
        float elapsed = 0f;
        while (elapsed < duration && character.CurrentStamina > 0f)
        {
            float delta = Mathf.Min(
                DungeonSimulationState.DeltaTime,
                duration - elapsed);
            character.SpendStamina(taskStaminaCostPerSecond * delta);
            elapsed += delta;
            investigationTimeRemaining = Mathf.Max(0f, duration - elapsed);
            yield return null;
        }
        completion?.Invoke(elapsed >= duration);
    }

    IEnumerator PerformTaskUntilExhausted()
    {
        performingTask = true;
        while (character.CurrentStamina > 0f)
        {
            character.SpendStamina(
                taskStaminaCostPerSecond * DungeonSimulationState.DeltaTime);
            yield return null;
        }
        performingTask = false;
        movement = null;
        StartNextExplorationStep();
    }

    void ClearActiveActivityState()
    {
        activeInvestigationTarget = null;
        investigationTimeRemaining = 0f;
        isInvestigating = false;
        performingTask = false;
    }

    void CompleteDungeonVisit()
    {
        if (!visitInProgress)
            return;

        visitInProgress = false;
        returningHome = false;
        ClearActiveActivityState();
        navigation?.NotifyAdventurerEscaped(this);
        if (character != null)
            character.RecordDungeonVisitCompleted();
        DungeonVisitCompleted?.Invoke(this);
        navigation?.DespawnAdventurer(this);
    }
}
