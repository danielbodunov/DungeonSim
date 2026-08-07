using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NPCDebugConnection
{
    public Vector3 from;
    public Vector3 to;
    public bool isLadder;
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
    [SerializeField, Min(0f), Tooltip("How far above the expected route height a valid floor may be. This prevents roof colliders from being selected.")]
    float maxSurfaceRise = 0.1f;
    [SerializeField, Min(0f)] float maxStepHeight = 0.3f;
    [SerializeField, Range(0f, 89f)] float maxWalkableSlope = 50f;
    [SerializeField, Min(0f)] float groundOffset;

    readonly Dictionary<Vector2Int, List<RouteEdge>> graph = new();
    readonly RaycastHit[] groundHits = new RaycastHit[16];
    readonly List<Vector3> debugWalkableSamples = new();
    readonly List<Vector3> debugRejectedSamples = new();
    NPCTraversalAgent agent;
    readonly List<NPCTraversalAgent> agents = new();
    int floorConnectionCount;
    int ladderConnectionCount;

    class RouteEdge
    {
        public Vector2Int destination;
        public List<Vector3> waypoints;
        public bool isLadder;
    }

    public NPCTraversalAgent ActiveAgent => agent;
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
    public event System.Action<NPCCharacter> AdventurerDied;

    internal void NotifyAdventurerDied(NPCCharacter character)
    {
        AdventurerDied?.Invoke(character);
    }

    internal void NotifyCellEntered(NPCTraversalAgent visitor, Vector2Int cell)
    {
        if (visitor != null)
            grid?.NotifyNpcEnteredCell(visitor.Character, cell);
    }

    void Awake()
    {
        if (grid == null)
            grid = GetComponent<TileGridGenerator>();
        if (props == null)
            props = GetComponent<PropGenerator>();
    }

    void OnEnable()
    {
        if (props != null)
            props.StructuresRegenerated += RebuildNavigation;
    }

    void Start()
    {
        // Also supports scenes where prop generation has already completed.
        if (props != null && props.StructureVersion > 0)
            RebuildNavigation();
    }

    void OnDisable()
    {
        if (props != null)
            props.StructuresRegenerated -= RebuildNavigation;
    }

    void RebuildNavigation()
    {
        BuildGraph();
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
        Vector3 origin = expectedPosition + Vector3.up * groundProbeHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            groundProbeHeight + groundProbeDistance,
            walkableLayers,
            QueryTriggerInteraction.Ignore);

        hit = default;
        bool found = false;
        float closestHeightDifference = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = groundHits[i];
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

        return found;
    }

    public Vector3 GetGroundedPosition(Vector3 expectedPosition)
    {
        if (!TryGetWalkableGround(expectedPosition, out RaycastHit hit))
            return expectedPosition;
        expectedPosition.y = hit.point.y + groundOffset;
        return expectedPosition;
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
        Vector3 aFloor = GetGroundedPosition(
            grid.GetCellWorldPosition(a.cell.x, a.cell.y));
        Vector3 bFloor = GetGroundedPosition(
            grid.GetCellWorldPosition(b.cell.x, b.cell.y));

        graph[a.cell].Add(new RouteEdge
        {
            destination = b.cell,
            waypoints = new List<Vector3> { aEntry, bEntry, bFloor },
            isLadder = true
        });
        graph[b.cell].Add(new RouteEdge
        {
            destination = a.cell,
            waypoints = new List<Vector3> { bEntry, aEntry, aFloor },
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
        expected.z = endpoint.worldPosition.z;
        return GetGroundedPosition(expected);
    }

    public NPCTraversalAgent SpawnAdventurer(NPCCharacterRecord characterRecord = null)
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning("NPCTraversal is ready, but no NPC prefab is assigned.", this);
            return null;
        }

        if (!TryFindSpawnPose(out Vector2Int start, out Vector3 spawnPosition))
        {
            Debug.LogWarning(
                "NPCTraversal could not find a placed entrance cell with a walkable floor.",
                this);
            return null;
        }

        GameObject instance = Instantiate(npcPrefab, spawnPosition, Quaternion.identity);
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
            this, start, moveSpeed, ladderSpeed,
            patrolAutomatically, waitAtDestination,
            movementStaminaCost, ladderStaminaMultiplier,
            taskStaminaCostPerSecond);
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

    bool TryFindSpawnPose(out Vector2Int start, out Vector3 position)
    {
        if (useManualStartCell && graph.ContainsKey(manualStartCell) &&
            TryGetEntranceFloorPosition(manualStartCell, out position))
        {
            start = manualStartCell;
            return true;
        }

        for (int y = 0; y < grid.GridHeight; y++)
        for (int x = 0; x < grid.GridWidth; x++)
        {
            var cell = new Vector2Int(x, y);
            if (graph.ContainsKey(cell) && TryGetEntranceFloorPosition(cell, out position))
            {
                start = cell;
                return true;
            }
        }

        start = new Vector2Int(-1, -1);
        position = default;
        return false;
    }

    bool TryGetEntranceFloorPosition(Vector2Int cell, out Vector3 position)
    {
        Vector3 anchor = grid.GetCellWorldPosition(cell.x, cell.y);
        anchor.x += spawnOffset.x;
        anchor.z += spawnOffset.z;
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
        if (!graph.ContainsKey(start) || !graph.ContainsKey(destination))
            return null;

        var queue = new Queue<Vector2Int>();
        var previous = new Dictionary<Vector2Int, RouteEdge>();
        queue.Enqueue(start);
        previous[start] = null;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == destination)
                break;
            foreach (RouteEdge edge in graph[current])
            {
                if (previous.ContainsKey(edge.destination))
                    continue;
                previous[edge.destination] = new RouteEdge
                {
                    destination = current,
                    waypoints = edge.waypoints
                };
                queue.Enqueue(edge.destination);
            }
        }

        if (!previous.ContainsKey(destination))
            return null;

        var edges = new List<RouteEdge>();
        Vector2Int step = destination;
        while (step != start)
        {
            RouteEdge edge = previous[step];
            edges.Add(edge);
            step = edge.destination;
        }
        edges.Reverse();

        var route = new List<Vector3>();
        foreach (RouteEdge edge in edges)
            route.AddRange(edge.waypoints);
        return route;
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
}

public class NPCTraversalAgent : MonoBehaviour
{
    NPCTraversal navigation;
    NPCCharacter character;
    Vector2Int startCell;
    Vector2Int currentCell;
    float speed;
    float climbSpeed;
    bool autoPatrol;
    float waitTime;
    Coroutine movement;
    List<Vector3> activeRoute;
    int nextWaypointIndex = -1;
    readonly HashSet<Vector2Int> visitedCells = new();
    float movementStaminaCost;
    float ladderStaminaMultiplier;
    float taskStaminaCostPerSecond;
    bool returningHome;
    bool visitInProgress;

    public IReadOnlyList<Vector3> ActiveRoute => activeRoute;
    public int NextWaypointIndex => nextWaypointIndex;
    public bool HasNextWaypoint => activeRoute != null &&
        nextWaypointIndex >= 0 && nextWaypointIndex < activeRoute.Count;
    public Vector3 NextWaypoint => HasNextWaypoint
        ? activeRoute[nextWaypointIndex]
        : transform.position;
    public NPCCharacter Character => character;
    public Vector2Int StartCell => startCell;
    public Vector2Int CurrentCell => currentCell;
    public IReadOnlyCollection<Vector2Int> VisitedCells => visitedCells;
    public float RemainingStamina => character != null ? character.CurrentStamina : 0f;
    public bool IsReturningHome => returningHome;
    public bool VisitInProgress => visitInProgress;

    /// <summary>The bool is true when this is the cell's first visit this round.</summary>
    public event System.Action<NPCTraversalAgent, Vector2Int, bool> CellEntered;
    public event System.Action<NPCTraversalAgent> DungeonVisitCompleted;

    public void Configure(
        NPCTraversal owner,
        Vector2Int startCell,
        float moveSpeed,
        float ladderMoveSpeed,
        bool patrol,
        float wait,
        float staminaCostPerUnit,
        float ladderCostMultiplier,
        float taskCostPerSecond)
    {
        navigation = owner;
        this.startCell = startCell;
        currentCell = startCell;
        speed = moveSpeed;
        climbSpeed = ladderMoveSpeed;
        autoPatrol = patrol;
        waitTime = wait;
        movementStaminaCost = staminaCostPerUnit;
        ladderStaminaMultiplier = ladderCostMultiplier;
        taskStaminaCostPerSecond = taskCostPerSecond;
        character = GetComponent<NPCCharacter>();
        if (character == null)
            character = gameObject.AddComponent<NPCCharacter>();
        character.Died += OnCharacterDied;
        BeginDungeonVisit();
    }

    void OnDestroy()
    {
        if (character != null)
            character.Died -= OnCharacterDied;
    }

    void OnCharacterDied(NPCCharacter deadCharacter)
    {
        if (movement != null)
            StopCoroutine(movement);
        movement = null;
        visitInProgress = false;
        navigation?.NotifyAdventurerDied(deadCharacter);
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
        if (character == null)
            return false;
        character.ResetVisitResources();
        returningHome = false;
        visitInProgress = true;
        RecordArrival(currentCell);
        StartNextExplorationStep();
        return true;
    }

    public bool MoveToCell(Vector2Int destination)
    {
        if (navigation == null)
            return false;
        List<Vector3> route = navigation.FindRoute(currentCell, destination);
        if (route == null)
            return false;
        if (movement != null)
            StopCoroutine(movement);
        activeRoute = route;
        nextWaypointIndex = route.Count > 0 ? 0 : -1;
        movement = StartCoroutine(FollowRoute(route, destination));
        return true;
    }

    IEnumerator FollowRoute(List<Vector3> route, Vector2Int destination)
    {
        for (int i = 0; i < route.Count; i++)
        {
            nextWaypointIndex = i;
            Vector3 waypoint = route[i];
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
                    transform.position, target, segmentSpeed * Time.deltaTime);
                if (!isLadderSegment)
                    next = navigation.GetGroundedPosition(next);
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

        currentCell = destination;
        RecordArrival(destination);
        activeRoute = null;
        nextWaypointIndex = -1;
        movement = null;
        if (!returningHome && waitTime > 0f && character.CurrentStamina > 0f)
            yield return SpendStaminaWhileWaiting(waitTime);

        if (returningHome && currentCell == startCell)
        {
            CompleteDungeonVisit();
            yield break;
        }

        StartNextExplorationStep();
    }

    void RecordArrival(Vector2Int cell)
    {
        if (!visitInProgress)
            return;

        navigation?.NotifyCellEntered(this, cell);
        if (character == null || character.IsDead)
            return;

        bool firstVisit = visitedCells.Add(cell);
        if (firstVisit)
        {
            if (character != null)
                character.RecordCellExplored();
        }
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
        if (currentCell != startCell && MoveToCell(startCell))
            return;

        // Already home, or the graph changed so home can no longer be reached.
        // Only a successful return counts as a completed dungeon visit.
        if (currentCell == startCell)
            CompleteDungeonVisit();
    }

    IEnumerator SpendStaminaWhileWaiting(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && character.CurrentStamina > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, duration - elapsed);
            character.SpendStamina(taskStaminaCostPerSecond * delta);
            elapsed += delta;
            yield return null;
        }
    }

    IEnumerator PerformTaskUntilExhausted()
    {
        while (character.CurrentStamina > 0f)
        {
            character.SpendStamina(taskStaminaCostPerSecond * Time.deltaTime);
            yield return null;
        }
        movement = null;
        StartNextExplorationStep();
    }

    void CompleteDungeonVisit()
    {
        if (!visitInProgress)
            return;

        visitInProgress = false;
        returningHome = false;
        if (character != null)
            character.RecordDungeonVisitCompleted();
        DungeonVisitCompleted?.Invoke(this);
        navigation?.DespawnAdventurer(this);
    }
}
