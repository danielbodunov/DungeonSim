using UnityEngine;
using System.Collections.Generic;

public class PropGenerator : MonoBehaviour
{
    [SerializeField] string ladderStructureId = "Ladder";
    [SerializeField] string ladderTopPrefabName = "Ladder_Start";
    [SerializeField] string ladderMiddlePrefabName = "Ladder_Continue";
    [SerializeField] string ladderBottomPrefabName = "Ladder_End";
    [SerializeField] Vector3 ladderWorldEulerAngles = Vector3.zero;
    [SerializeField] bool logLadderDiagnostics = true;

    TileGridGenerator gridGenerator;
    GameObject ladderTopPrefab;
    GameObject ladderMiddlePrefab;
    GameObject ladderBottomPrefab;
    HashSet<Vector2Int> occupiedPropCells = new();
    List<GameObject> spawnedProps = new();
    Coroutine pendingGeneration;

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
        if (gridGenerator == null)
            return;

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
        PlaceLadders();
        pendingGeneration = null;
    }

    void LoadPropPrefabs()
    {
        ladderTopPrefab = LoadPropPrefab(ladderTopPrefabName);
        ladderMiddlePrefab = LoadPropPrefab(ladderMiddlePrefabName);
        ladderBottomPrefab = LoadPropPrefab(ladderBottomPrefabName);
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

            for (int i = 0; i < selectedRun.Count; i++)
            {
                GameObject prefab = i == 0
                    ? ladderTopPrefab
                    : i == selectedRun.Count - 1
                        ? ladderBottomPrefab
                        : ladderMiddlePrefab;
                PlaceLadderPiece(prefab, selectedRun[i]);
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

            BakedPropSocket continuation = FindSocket(
                x, nextY, PropSocketRole.Continue, startSocket.laneId);
            BakedPropSocket end = FindSocket(
                x, nextY, PropSocketRole.End, startSocket.laneId);
            if (end != null)
            {
                run.Add(new LadderPiece(x, nextY, end));
                return run.Count >= 2;
            }

            if (continuation == null)
            {
                failure = $"{gridGenerator.GetCellProfileId(x, nextY)} has no matching Continue or End socket";
                return false;
            }

            run.Add(new LadderPiece(x, nextY, continuation));
        }

        failure = "the run reached the grid boundary without an End socket";
        return false;
    }

    BakedPropSocket FindSocket(int x, int y, PropSocketRole role, string laneId)
    {
        foreach (BakedPropSocket socket in gridGenerator.GetCellPropSockets(x, y))
            if (IsSocket(socket, role) &&
                IsCompatibleDirection(gridGenerator.GetRuntimeSocketDirection(socket), role) &&
                string.Equals(socket.laneId, laneId, System.StringComparison.OrdinalIgnoreCase))
                return socket;

        return null;
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
                piece.x, piece.y, piece.socket, out Vector3 position, out _))
            return;

        Quaternion rotation = Quaternion.Euler(ladderWorldEulerAngles);
        var prop = Instantiate(prefab, position, rotation, transform);
        prop.name = $"{prefab.name} [{piece.x},{piece.y}] on {gridGenerator.GetCellProfileId(piece.x, piece.y)}";
        spawnedProps.Add(prop);
        occupiedPropCells.Add(cell);
    }
}
