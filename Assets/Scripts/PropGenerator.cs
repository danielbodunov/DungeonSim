using UnityEngine;
using System.Collections.Generic;

public class PropGenerator : MonoBehaviour
{
    [SerializeField] PropCatalog propCatalog;
    [SerializeField] string ladderStructureId = "Ladder";
    [SerializeField] string ladderTopPrefabName = "Ladder_Start";
    [SerializeField] string ladderMiddlePrefabName = "Ladder_Continue";
    [SerializeField] string ladderBottomPrefabName = "Ladder_End";
    [SerializeField] Vector3 ladderWorldEulerAngles = Vector3.zero;
    [SerializeField, Min(0.001f)] float ladderSocketAlignmentTolerance = 0.15f;
    [SerializeField] bool logLadderDiagnostics = true;

    TileGridGenerator gridGenerator;
    GameObject ladderTopPrefab;
    GameObject ladderMiddlePrefab;
    GameObject ladderBottomPrefab;
    HashSet<Vector2Int> occupiedPropCells = new();
    List<GameObject> spawnedProps = new();
    readonly List<GeneratedStructureRun> generatedRuns = new();
    Coroutine pendingGeneration;
    int pendingGenerationSeed;

    public IReadOnlyList<GeneratedStructureRun> GeneratedRuns => generatedRuns;
    public int StructureVersion { get; private set; }
    public int GenerationSeed { get; private set; }
    public int SaveGenerationSeed => pendingGeneration != null
        ? pendingGenerationSeed
        : GenerationSeed;
    public event System.Action StructuresRegenerated;

    public List<GeneratedStructureRun> GetRunsAtCell(Vector2Int cell)
    {
        var results = new List<GeneratedStructureRun>();
        foreach (GeneratedStructureRun run in generatedRuns)
        {
            foreach (GeneratedStructurePiece piece in run.pieces)
            {
                if (piece.cell != cell)
                    continue;
                results.Add(run);
                break;
            }
        }
        return results;
    }

    struct LadderPiece
    {
        public int x;
        public int y;
        public BakedPropSocket socket;

        public LadderPiece(int x, int y, BakedPropSocket socket)
        {
            this.x = x;
            this.y = y;
            this.socket = socket;
        }
    }

    struct LadderCandidate
    {
        public List<LadderPiece> run;
        public float weight;

        public LadderCandidate(List<LadderPiece> run, float weight)
        {
            this.run = run;
            this.weight = weight;
        }
    }

    public void Initialize(TileGridGenerator generator)
    {
        gridGenerator = generator;
        LoadPropPrefabs();
    }

    public void GenerateProps()
    {
        GenerateProps(Random.Range(-1000000000, 1000000000));
    }

    public void GenerateProps(int generationSeed)
    {
        if (gridGenerator == null)
            return;

        pendingGenerationSeed = generationSeed;
        if (pendingGeneration == null)
            pendingGeneration = StartCoroutine(RegenerateAfterFrame());
    }

    System.Collections.IEnumerator RegenerateAfterFrame()
    {
        yield return null;

        foreach (var prop in spawnedProps)
        {
            if (prop != null)
            {
                prop.SetActive(false);
                Destroy(prop);
            }
        }
        spawnedProps.Clear();
        occupiedPropCells.Clear();
        generatedRuns.Clear();
        StructureVersion++;
        GenerationSeed = pendingGenerationSeed;

        Random.State previousRandomState = Random.state;
        Random.InitState(GenerationSeed);
        try
        {
            PlaceLadders();
            PlaceSingleProps();
        }
        finally
        {
            Random.state = previousRandomState;
        }
        pendingGeneration = null;
        StructuresRegenerated?.Invoke();
    }

    void LoadPropPrefabs()
    {
        if (propCatalog == null)
            propCatalog = Resources.Load<PropCatalog>("PropCatalog");

        PropDefinition ladder = propCatalog?.Find(ladderStructureId);
        ladderTopPrefab = ladder?.GetPrefab(PropSocketRole.Start)
            ?? LoadPropPrefab(ladderTopPrefabName);
        ladderMiddlePrefab = ladder?.GetPrefab(PropSocketRole.Continue)
            ?? LoadPropPrefab(ladderMiddlePrefabName);
        ladderBottomPrefab = ladder?.GetPrefab(PropSocketRole.End)
            ?? LoadPropPrefab(ladderBottomPrefabName);
    }

    GameObject LoadPropPrefab(string prefabName)
    {
        var prefab = Resources.Load<GameObject>($"Props/{prefabName}");
        if (prefab == null)
            prefab = Resources.Load<GameObject>(prefabName);

        if (prefab == null)
            Debug.LogWarning($"Could not find prop prefab '{prefabName}'. Add it under Assets/Resources/Props or assign the prefab name in the prop generator.");
        return prefab;
    }

    void PlaceLadders()
    {
        if (ladderTopPrefab == null || ladderMiddlePrefab == null || ladderBottomPrefab == null)
        {
            Debug.LogWarning("Ladder prefabs were not found. Check the Start, Continue, and End prefab names on PropGenerator and ensure those assets exist under Assets/Resources/Props.");
            return;
        }

        int compatibleStarts = 0;
        int completedRuns = 0;
        var failures = new List<string>();

        for (int x = 1; x < gridGenerator.GridWidth - 1; x++)
        for (int y = 1; y < gridGenerator.GridHeight - 1; y++)
        {
            if (!gridGenerator.IsPlacedCell(x, y))
                continue;

            if (occupiedPropCells.Contains(new Vector2Int(x, y)))
                continue;

            var candidates = new List<LadderCandidate>();
            foreach (BakedPropSocket startSocket in gridGenerator.GetCellPropSockets(x, y))
            {
                if (!IsSocket(startSocket, PropSocketRole.Start) ||
                    gridGenerator.GetRuntimeSocketDirection(startSocket) != PropSocketDirection.South)
                    continue;

                compatibleStarts++;
                if (!TryGetLadderRun(x, y, startSocket, out var run, out string failure))
                {
                    if (failures.Count < 8)
                        failures.Add($"[{x},{y}] {gridGenerator.GetCellProfileId(x, y)} lane '{startSocket.laneId}': {failure}");
                    continue;
                }

                candidates.Add(new LadderCandidate(run, startSocket.selectionWeight));
            }

            if (candidates.Count == 0)
                continue;

            List<LadderPiece> selectedRun = SelectWeightedRun(candidates);
            if (selectedRun == null)
                continue;

            completedRuns++;

            GeneratedStructureRun generatedRun = BuildGeneratedRun(selectedRun);
            if (generatedRun != null)
                generatedRuns.Add(generatedRun);

            for (int i = 0; i < selectedRun.Count; i++)
            {
                GameObject fallbackPrefab = i == 0
                    ? ladderTopPrefab
                    : i == selectedRun.Count - 1
                        ? ladderBottomPrefab
                        : ladderMiddlePrefab;
                PlaceLadderPiece(fallbackPrefab, selectedRun[i]);
            }
        }

        if (logLadderDiagnostics)
        {
            string details = failures.Count > 0
                ? $"\n- {string.Join("\n- ", failures)}"
                : string.Empty;
            Debug.Log($"Ladder generation: {compatibleStarts} compatible starts, {completedRuns} completed runs.{details}");
        }
    }

    void PlaceSingleProps()
    {
        if (propCatalog == null)
            return;

        foreach (PropDefinition definition in propCatalog.definitions)
        {
            if (definition == null || definition.generationMode != PropGenerationMode.Single)
                continue;

            GameObject fallbackPrefab = definition.GetPrefab(PropSocketRole.Single);

            for (int x = 1; x < gridGenerator.GridWidth - 1; x++)
            for (int y = 1; y < gridGenerator.GridHeight - 1; y++)
            {
                if (!gridGenerator.IsPlacedCell(x, y))
                    continue;

                var cell = new Vector2Int(x, y);
                if (definition.occupiesCell && occupiedPropCells.Contains(cell))
                    continue;
                if (Random.value > definition.spawnChance)
                    continue;

                var candidates = new List<BakedPropSocket>();
                float totalWeight = 0f;
                foreach (BakedPropSocket socket in gridGenerator.GetCellPropSockets(x, y))
                {
                    if (socket == null || socket.role != PropSocketRole.Single ||
                        !string.Equals(socket.structureId, definition.structureId,
                            System.StringComparison.OrdinalIgnoreCase) ||
                        socket.selectionWeight <= 0f)
                        continue;

                    candidates.Add(socket);
                    totalWeight += socket.selectionWeight;
                }

                if (candidates.Count == 0 || totalWeight <= 0f)
                    continue;

                float selection = Random.value * totalWeight;
                BakedPropSocket selected = candidates[candidates.Count - 1];
                foreach (BakedPropSocket candidate in candidates)
                {
                    selection -= candidate.selectionWeight;
                    if (selection <= 0f)
                    {
                        selected = candidate;
                        break;
                    }
                }

                if (!gridGenerator.TryGetPropSocketWorldPose(
                    x, y, selected, out Vector3 position, out Quaternion socketRotation))
                    continue;

                int spawned = SpawnResolvedBundle(
                    definition, fallbackPrefab, selected, position, socketRotation,
                    x, y);
                if (spawned > 0 && definition.occupiesCell)
                    occupiedPropCells.Add(cell);
            }
        }
    }

    List<LadderPiece> SelectWeightedRun(List<LadderCandidate> candidates)
    {
        float totalWeight = 0f;
        foreach (LadderCandidate candidate in candidates)
            totalWeight += candidate.weight;

        if (totalWeight <= 0f)
            return null;

        float selection = Random.value * totalWeight;
        foreach (LadderCandidate candidate in candidates)
        {
            selection -= candidate.weight;
            if (selection <= 0f)
                return candidate.run;
        }

        return candidates[candidates.Count - 1].run;
    }

    bool TryGetLadderRun(
        int x,
        int y,
        BakedPropSocket startSocket,
        out List<LadderPiece> run,
        out string failure)
    {
        failure = string.Empty;
        run = new List<LadderPiece> { new LadderPiece(x, y, startSocket) };
        if (!TryGetLadderAnchorWorldPosition(
            x, y, startSocket, out Vector3 startPosition))
        {
            failure = "the Start socket has no runtime world pose";
            return false;
        }

        Vector2 alignmentAnchor = new Vector2(startPosition.x, startPosition.z);
        for (int nextY = y + 1; nextY < gridGenerator.GridHeight - 1; nextY++)
        {
            if (!gridGenerator.IsPlacedCell(x, nextY))
            {
                failure = $"cell [{x},{nextY}] is not placed";
                return false;
            }

            if (occupiedPropCells.Contains(new Vector2Int(x, nextY)))
            {
                failure = $"cell [{x},{nextY}] is already occupied by another prop";
                return false;
            }

            if (!gridGenerator.HasMatchingVerticalEdge(x, nextY - 1, nextY))
            {
                failure = $"{gridGenerator.GetCellProfileId(x, nextY - 1)} and {gridGenerator.GetCellProfileId(x, nextY)} do not have matching vertical edges";
                return false;
            }

            BakedPropSocket continuation = FindAlignedSocket(
                x, nextY, PropSocketRole.Continue, startSocket.laneId,
                alignmentAnchor,
                out float continuationDistance);
            BakedPropSocket end = FindAlignedSocket(
                x, nextY, PropSocketRole.End, startSocket.laneId,
                alignmentAnchor,
                out float endDistance);
            if (end != null)
            {
                run.Add(new LadderPiece(x, nextY, end));
                return run.Count >= 2;
            }

            if (continuation == null)
            {
                float nearestDistance = Mathf.Min(continuationDistance, endDistance);
                string alignment = float.IsPositiveInfinity(nearestDistance)
                    ? "no compatible Continue or End sockets"
                    : $"nearest compatible socket is {nearestDistance:0.###} units from the ladder anchor (tolerance {ladderSocketAlignmentTolerance:0.###})";
                failure = $"{gridGenerator.GetCellProfileId(x, nextY)} has {alignment}";
                return false;
            }

            run.Add(new LadderPiece(x, nextY, continuation));
        }

        failure = "the run reached the grid boundary without an End socket";
        return false;
    }

    BakedPropSocket FindAlignedSocket(
        int x,
        int y,
        PropSocketRole role,
        string laneId,
        Vector2 alignmentAnchor,
        out float closestDistance)
    {
        closestDistance = float.PositiveInfinity;
        BakedPropSocket closest = null;
        foreach (BakedPropSocket socket in gridGenerator.GetCellPropSockets(x, y))
            if (IsSocket(socket, role) &&
                string.Equals(socket.laneId, laneId,
                    System.StringComparison.OrdinalIgnoreCase) &&
                IsCompatibleDirection(gridGenerator.GetRuntimeSocketDirection(socket), role) &&
                TryGetLadderAnchorWorldPosition(
                    x, y, socket, out Vector3 position))
            {
                float distance = Vector2.Distance(
                    alignmentAnchor, new Vector2(position.x, position.z));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = socket;
                }
            }

        return closestDistance <= ladderSocketAlignmentTolerance ? closest : null;
    }

    bool TryGetLadderAnchorWorldPosition(
        int x,
        int y,
        BakedPropSocket socket,
        out Vector3 position)
    {
        if (!gridGenerator.TryGetPropSocketWorldPose(
            x, y, socket, out position, out Quaternion socketRotation))
        {
            return false;
        }

        PropDefinition definition = propCatalog?.Find(ladderStructureId);
        PropPieceBundle bundle = definition?.GetBundle(
            socket.laneId, socket.role, socket.bundleId);
        GameObject ladderPrefab = GetLadderPrefab(socket.role);
        if (bundle == null || ladderPrefab == null)
            return true;

        Quaternion baseRotation = (definition.useSocketRotation
            ? socketRotation
            : Quaternion.identity) * Quaternion.Euler(definition.rotationOffset);
        foreach (PropBundleItem item in bundle.items)
        {
            if (item == null || item.prefab != ladderPrefab)
                continue;

            position += baseRotation * item.localPosition;
            break;
        }
        return true;
    }

    GameObject GetLadderPrefab(PropSocketRole role)
    {
        return role switch
        {
            PropSocketRole.Start => ladderTopPrefab,
            PropSocketRole.Continue => ladderMiddlePrefab,
            PropSocketRole.End => ladderBottomPrefab,
            _ => null
        };
    }

    GeneratedStructureRun BuildGeneratedRun(List<LadderPiece> run)
    {
        var generated = new GeneratedStructureRun
        {
            structureId = ladderStructureId,
            generationVersion = StructureVersion
        };

        for (int i = 0; i < run.Count; i++)
        {
            LadderPiece piece = run[i];
            if (!TryGetLadderAnchorWorldPosition(
                piece.x, piece.y, piece.socket, out Vector3 position))
                return null;

            var cell = new Vector2Int(piece.x, piece.y);
            generated.pieces.Add(new GeneratedStructurePiece
            {
                cell = cell,
                tileProfileId = gridGenerator.GetCellProfileId(piece.x, piece.y),
                role = piece.socket.role,
                laneId = piece.socket.laneId,
                bundleId = piece.socket.bundleId,
                worldPosition = position,
                socket = piece.socket
            });

            bool isEndpoint = i == 0 || i == run.Count - 1 ||
                (piece.socket.role == PropSocketRole.Continue &&
                    piece.socket.allowsTraversalExit);
            if (isEndpoint)
            {
                generated.traversalEndpoints.Add(new GeneratedTraversalEndpoint
                {
                    cell = cell,
                    worldPosition = position,
                    sourceRole = piece.socket.role,
                    isIntermediate = i > 0 && i < run.Count - 1
                });
            }
        }

        return generated;
    }

    bool IsSocket(BakedPropSocket socket, PropSocketRole role)
    {
        return socket != null &&
            socket.role == role &&
            string.Equals(socket.structureId, ladderStructureId, System.StringComparison.OrdinalIgnoreCase);
    }

    bool IsCompatibleDirection(PropSocketDirection direction, PropSocketRole role)
    {
        return role switch
        {
            PropSocketRole.Start => direction == PropSocketDirection.South,
            PropSocketRole.Continue => direction == PropSocketDirection.North ||
                direction == PropSocketDirection.South,
            PropSocketRole.End => direction == PropSocketDirection.North,
            _ => false
        };
    }

    void PlaceLadderPiece(GameObject prefab, LadderPiece piece)
    {
        var cell = new Vector2Int(piece.x, piece.y);
        if (prefab == null || occupiedPropCells.Contains(cell) ||
            !gridGenerator.TryGetPropSocketWorldPose(
                piece.x, piece.y, piece.socket, out Vector3 position, out Quaternion socketRotation))
            return;

        PropDefinition definition = propCatalog?.Find(ladderStructureId);
        int spawned = SpawnResolvedBundle(
            definition, prefab, piece.socket, position, socketRotation,
            piece.x, piece.y, ladderWorldEulerAngles);
        if (spawned > 0)
            occupiedPropCells.Add(cell);
    }

    int SpawnResolvedBundle(
        PropDefinition definition,
        GameObject fallbackPrefab,
        BakedPropSocket socket,
        Vector3 position,
        Quaternion socketRotation,
        int x,
        int y,
        Vector3? fallbackRotation = null)
    {
        Quaternion baseRotation = definition != null
            ? (definition.useSocketRotation ? socketRotation : Quaternion.identity)
                * Quaternion.Euler(definition.rotationOffset)
            : Quaternion.Euler(fallbackRotation ?? Vector3.zero);

        PropPieceBundle bundle = definition?.GetBundle(
            socket.laneId, socket.role, socket.bundleId);
        int count = 0;
        if (bundle != null)
        {
            foreach (PropBundleItem item in bundle.items)
            {
                if (item == null || item.prefab == null)
                    continue;

                Vector3 itemPosition = position + baseRotation * item.localPosition;
                Quaternion itemRotation = baseRotation * Quaternion.Euler(item.localRotation);
                SpawnProp(item.prefab, itemPosition, itemRotation, x, y, socket);
                count++;
            }
            return count;
        }

        if (fallbackPrefab != null)
        {
            SpawnProp(fallbackPrefab, position, baseRotation, x, y, socket);
            count++;
        }
        return count;
    }

    void SpawnProp(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        int x,
        int y,
        BakedPropSocket socket)
    {
        GameObject prop = Instantiate(prefab, position, rotation, transform);
        prop.name = $"{prefab.name} [{x},{y}] {socket.laneId}/{socket.bundleId} on {gridGenerator.GetCellProfileId(x, y)}";
        if (prop.GetComponentInChildren<DungeonLightReceiver>(true) == null)
            prop.AddComponent<DungeonLightReceiver>();
        spawnedProps.Add(prop);
    }
}
