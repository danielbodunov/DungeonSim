using UnityEngine;
using System;
using System.Collections.Generic;

public class TilePlacement : MonoBehaviour
{
    [SerializeField]
    private GameObject mouseIndicator, cellIndicator;

    [SerializeField]
    private InputManager inputManager;


    [SerializeField]
    private Grid grid;

    [SerializeField]
    private TileGridGenerator tileGridGenerator;

    [SerializeField]
    private ObjectsDatabaseSO database;
    private int selectedObjectIndex = -1;
    private Func<Vector2Int, bool> customPlacementHandler;

    [SerializeField]
    private GameObject gridVisualization;

    [SerializeField]
    private GameObject tiles;

    [SerializeField]
    private TileAdjacencyDatabase tileDatabase;

    private Vector3Int? lastDragCell;
    private bool buildingEnabled = true;
    private bool removingTraps;
    private bool removingEntrance;
    private bool editingEdges;
    private CellWidthIntent widthIntent = CellWidthIntent.Auto;
    private GameObject floorPropPreview;
    private GameObject trapPreview;
    private GameObject trapConstructionPresentationPreview;
    private Vector2Int? trapPresentationPreviewServiceCell;
    private Vector2Int? trapPresentationPreviewTargetCell;
    private TrapAttachmentSurface? trapPresentationPreviewSurface;
    private Renderer[] floorPropPreviewRenderers = System.Array.Empty<Renderer>();
    private Renderer[] trapPreviewRenderers = System.Array.Empty<Renderer>();
    private LineRenderer trapHazardPreview;
    private Material trapHazardPreviewMaterial;
    private GameObject trapTargetIndicator;
    private Renderer[] trapTargetIndicatorRenderers =
        System.Array.Empty<Renderer>();
    private readonly List<GameObject> trapFootprintIndicators = new();
    private int selectedTrapCandidateIndex;
    private int trapCandidateCount;
    private TrapAttachmentPlacement selectedTrapCandidate;
    private bool hasSelectedTrapCandidate;
    private string trapPlacementFailure = string.Empty;
    private Vector3Int? lastTrapPreviewCell;
    private Renderer[] cellIndicatorRenderers = System.Array.Empty<Renderer>();
    private MaterialPropertyBlock previewPropertyBlock;
    private GameplayLoopController gameplayLoop;
    static readonly BuildCost DungeonTileCost = new(
        PhysicalResourceCategory.ConstructionMaterials, 1);
    private static readonly Color ValidPreviewColor =
        new(0.25f, 0.9f, 0.4f, 0.72f);
    private static readonly Color InvalidPreviewColor =
        new(1f, 0.2f, 0.2f, 0.72f);

    public bool BuildingEnabled => buildingEnabled;
    public bool IsRemovingTraps => removingTraps;
    public bool IsRemovingEntrance => removingEntrance;
    public bool IsEditingEdges => editingEdges;
    public bool IsPlacementActive => removingTraps || removingEntrance ||
        editingEdges || selectedObjectIndex >= 0;
    public CellWidthIntent WidthIntent => widthIntent;
    public bool IsTrapPlacementActive => selectedObjectIndex >= 0 &&
        database != null && selectedObjectIndex < database.objectsData.Count &&
        database.objectsData[selectedObjectIndex].PlacementType ==
            ObjectPlacementType.Trap;
    public int TrapCandidateCount => trapCandidateCount;
    public bool HasSelectedTrapCandidate => hasSelectedTrapCandidate;
    public TrapAttachmentPlacement SelectedTrapCandidate =>
        selectedTrapCandidate;
    public string TrapPlacementFailure => trapPlacementFailure;
    public int SelectedObjectId => selectedObjectIndex >= 0 &&
        database != null && selectedObjectIndex < database.objectsData.Count
            ? database.objectsData[selectedObjectIndex].ID
            : -1;
    public IReadOnlyList<ObjectData> AvailableObjects => database != null
        ? database.objectsData
        : System.Array.Empty<ObjectData>();

    private void Start()
    {
        if(tileGridGenerator == null)
        {
            tileGridGenerator = this.GetComponent<TileGridGenerator>()
                ?? throw new System.Exception("TileGridGenerator component not found on the same GameObject.");
        }
        cellIndicatorRenderers = cellIndicator != null
            ? cellIndicator.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();
        previewPropertyBlock = new MaterialPropertyBlock();
        gameplayLoop = GameplayLoopController.Instance
            ?? FindAnyObjectByType<GameplayLoopController>();
        StopPlacement();
        //CreateGroundTiles();
    }

    void OnDisable()
    {
        DestroyFloorPropPreview();
        DestroyTrapPreview();
        if (inputManager != null)
        {
            inputManager.OnClicked -= PlaceStructure;
            inputManager.OnRightClicked -= PlaceGround;
            inputManager.OnExit -= StopPlacement;
        }
    }

    public void StartPlacement(int ID)
    {
        if (!TryStartPlacement(ID, null, out string failure))
            Debug.LogWarning(failure, this);
    }

    public bool TryStartPlacement(
        int ID,
        Func<Vector2Int, bool> placementHandler,
        out string failure)
    {
        failure = string.Empty;
        if (!buildingEnabled)
        {
            failure = "Placement is disabled outside the dungeon expansion phase.";
            return false;
        }
        if (database == null || database.objectsData == null)
        {
            failure = "Placement cannot start because the object database is unavailable.";
            return false;
        }

        StopPlacement(); // Ensure any existing placement is stopped before starting a new one
        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
        if (selectedObjectIndex <  0)
        {
            failure = $"Object {ID} is not present in the placement database.";
            return false;
        }
        customPlacementHandler = placementHandler;
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        CreateFloorPropPreview(database.objectsData[selectedObjectIndex]);
        CreateTrapPreview(database.objectsData[selectedObjectIndex]);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnRightClicked += PlaceGround;
        inputManager.OnExit += StopPlacement;

        return true;
    }

    public void StartTrapRemoval()
    {
        if (!buildingEnabled)
            return;

        StopPlacement();
        removingTraps = true;
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    public void StartEntranceRemoval()
    {
        if (!buildingEnabled)
            return;

        StopPlacement();
        removingEntrance = true;
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    public void StartEdgeToggle()
    {
        if (!buildingEnabled)
            return;

        StopPlacement();
        editingEdges = true;
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }


    private void PlaceStructure()
    {
        if (!buildingEnabled)
            return;

        if(inputManager.IsPointerOverUI())
        {
            return;
        }

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        if (editingEdges)
        {
            tileGridGenerator.ToggleConnectionIntentAtWorldPosition(mousePosition);
            return;
        }

        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        PlaceAtCell(gridPosition);
    }

    private void PlaceGround()
    {
        if (!buildingEnabled)
            return;

        if (inputManager.IsPointerOverUI())
            return;

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        Vector3 cellCenter = grid.GetCellCenterWorld(gridPosition);
        if (!tileGridGenerator.TryWorldToCell(cellCenter, out Vector2Int logicalCell) ||
            !tileGridGenerator.IsPlacedCell(logicalCell.x, logicalCell.y))
            return;
        if (tileGridGenerator.PlaceGroundWorldPosition(cellCenter))
            ResolveGameplayLoop()?.RefundBuildCost(DungeonTileCost);
    }

    private void PlaceAtCell(Vector3Int gridPosition)
    {
        if (!buildingEnabled)
            return;

        if (lastDragCell.HasValue && lastDragCell.Value == gridPosition)
            return;

        Vector3 cellCenter = grid.GetCellCenterWorld(gridPosition);
        if (removingTraps)
        {
            tileGridGenerator.RemoveTrapWorldPosition(cellCenter);
            lastDragCell = gridPosition;
            return;
        }

        if (removingEntrance)
        {
            tileGridGenerator.RemoveEntranceWorldPosition(cellCenter);
            lastDragCell = gridPosition;
            return;
        }

        ObjectData selectedObject = database.objectsData[selectedObjectIndex];
        Debug.Log($"Placing {selectedObject.Name} (ID {selectedObject.ID}) at grid position ({gridPosition.x}, {gridPosition.y})");
        if (selectedObject.PlacementType == ObjectPlacementType.Trap)
        {
            if (tileGridGenerator.PlaceTrapFromServiceWorldPosition(
                    cellCenter,
                    selectedObject.Prefab,
                    selectedObject.ID,
                    selectedTrapCandidateIndex))
                lastDragCell = gridPosition;
        }
        else if (selectedObject.PlacementType == ObjectPlacementType.Entrance)
        {
            if (tileGridGenerator.PlaceEntranceWorldPosition(
                    cellCenter, selectedObject.Prefab, selectedObject.ID))
            {
                lastDragCell = gridPosition;
            }
        }
        else if (selectedObject.PlacementType == ObjectPlacementType.FloorProp)
        {
            tileGridGenerator.TryWorldToCell(
                cellCenter,
                out Vector2Int logicalCell);
            bool placed = customPlacementHandler != null
                ? customPlacementHandler.Invoke(logicalCell)
                : tileGridGenerator.PlaceFloorPropWorldPosition(
                    cellCenter, selectedObject.Prefab, selectedObject.ID);
            if (placed)
            {
                lastDragCell = gridPosition;
            }
        }
        else
        {
            tileGridGenerator.TryWorldToCell(cellCenter, out Vector2Int logicalCell);
            bool createsCell = !tileGridGenerator.IsPlacedCell(
                logicalCell.x, logicalCell.y);
            GameplayLoopController resources = ResolveGameplayLoop();
            string failure = string.Empty;
            if (createsCell && (resources == null ||
                !resources.CanAfford(DungeonTileCost, out failure)))
            {
                Debug.LogWarning(resources == null
                    ? "Construction resources are unavailable."
                    : failure, this);
                return;
            }
            if (tileGridGenerator.ClickWorldPosition(cellCenter, widthIntent))
            {
                if (createsCell)
                    resources.TrySpendBuildCost(DungeonTileCost, out _);
                lastDragCell = gridPosition;
            }
        }
    }

    GameplayLoopController ResolveGameplayLoop()
    {
        if (gameplayLoop == null)
            gameplayLoop = GameplayLoopController.Instance
                ?? FindAnyObjectByType<GameplayLoopController>();
        return gameplayLoop;
    }

    public void SetWidthIntent(CellWidthIntent intent)
    {
        widthIntent = intent;
    }

    public void CycleTrapCandidate()
    {
        if (!IsTrapPlacementActive || trapCandidateCount <= 1)
            return;
        selectedTrapCandidateIndex =
            (selectedTrapCandidateIndex + 1) % trapCandidateCount;
        lastDragCell = null;
    }

    public void StopPlacement()
    {
        DestroyFloorPropPreview();
        DestroyTrapPreview();
        ClearPreviewTint(cellIndicatorRenderers);
        selectedObjectIndex = -1;
        customPlacementHandler = null;
        removingTraps = false;
        removingEntrance = false;
        editingEdges = false;
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnRightClicked -= PlaceGround;
        inputManager.OnExit -= StopPlacement;
        lastDragCell = null;
        selectedTrapCandidateIndex = 0;
        trapCandidateCount = 0;
        hasSelectedTrapCandidate = false;
        trapPlacementFailure = string.Empty;
        lastTrapPreviewCell = null;
    }


    private void Update()
    {
        if (!buildingEnabled ||
            (!removingTraps && !removingEntrance &&
             !editingEdges && selectedObjectIndex < 0))
        { 
            return; 
        }
        
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        mouseIndicator.transform.position = mousePosition;
        cellIndicator.transform.position = grid.GetCellCenterWorld(gridPosition);

        UpdateFloorPropPreview(grid.GetCellCenterWorld(gridPosition));
        Vector3 cellCenter = grid.GetCellCenterWorld(gridPosition);
        if (IsTrapPlacementActive &&
            (!lastTrapPreviewCell.HasValue ||
             lastTrapPreviewCell.Value != gridPosition))
        {
            selectedTrapCandidateIndex = 0;
            lastTrapPreviewCell = gridPosition;
        }
        UpdateTrapPreview(cellCenter);
        if (IsTrapPlacementActive && inputManager.TrapCandidateCyclePressed &&
            trapCandidateCount > 1)
        {
            CycleTrapCandidate();
            UpdateTrapPreview(cellCenter);
        }

        // Wall toggles are discrete clicks handled by PlaceStructure. Keeping
        // them out of the held-button path prevents one click toggling twice.
        if (editingEdges)
            return;

        if (!inputManager.LeftClick)
        {
            lastDragCell = null;
            return;
        }

        if (!inputManager.IsPointerOverUI())
            PlaceAtCell(gridPosition);

    }

    public void SetBuildingEnabled(bool enabled)
    {
        buildingEnabled = enabled;
        if (!buildingEnabled)
            StopPlacement();
    }

    void CreateFloorPropPreview(ObjectData selectedObject)
    {
        DestroyFloorPropPreview();
        if (selectedObject == null ||
            selectedObject.PlacementType != ObjectPlacementType.FloorProp ||
            selectedObject.Prefab == null)
        {
            return;
        }

        floorPropPreview = Instantiate(selectedObject.Prefab);
        floorPropPreview.name = $"{selectedObject.Prefab.name} Placement Preview";
        floorPropPreview.hideFlags = HideFlags.DontSave;

        foreach (Behaviour behaviour in
            floorPropPreview.GetComponentsInChildren<Behaviour>(true))
        {
            behaviour.enabled = false;
        }
        foreach (Collider target in
            floorPropPreview.GetComponentsInChildren<Collider>(true))
        {
            target.enabled = false;
        }
        foreach (Rigidbody body in
            floorPropPreview.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        floorPropPreviewRenderers =
            floorPropPreview.GetComponentsInChildren<Renderer>(true);
    }

    void CreateTrapPreview(ObjectData selectedObject)
    {
        DestroyTrapPreview();
        if (selectedObject == null ||
            selectedObject.PlacementType != ObjectPlacementType.Trap ||
            selectedObject.Prefab == null)
            return;

        if (selectedObject.Prefab.GetComponent<TrapAttachmentDefinition>() == null)
            return;
        trapPreview = Instantiate(selectedObject.Prefab);
        trapPreview.name = $"{selectedObject.Prefab.name} Placement Preview";
        trapPreview.hideFlags = HideFlags.DontSave;
        DisablePreviewGameplay(trapPreview);
        trapPreviewRenderers =
            trapPreview.GetComponentsInChildren<Renderer>(true);

        GameObject lineObject = new("Trap Hazard Direction Preview");
        lineObject.transform.SetParent(transform, false);
        lineObject.hideFlags = HideFlags.DontSave;
        trapHazardPreview = lineObject.AddComponent<LineRenderer>();
        trapHazardPreview.useWorldSpace = true;
        trapHazardPreview.positionCount = 2;
        trapHazardPreview.startWidth = 0.08f;
        trapHazardPreview.endWidth = 0.02f;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            trapHazardPreviewMaterial = new Material(shader)
            {
                hideFlags = HideFlags.DontSave
            };
            trapHazardPreview.sharedMaterial = trapHazardPreviewMaterial;
        }

        if (cellIndicator != null)
        {
            trapTargetIndicator = Instantiate(cellIndicator, transform);
            trapTargetIndicator.name = "Trap Target Corridor Preview";
            trapTargetIndicator.hideFlags = HideFlags.DontSave;
            DisablePreviewGameplay(trapTargetIndicator);
            trapTargetIndicatorRenderers =
                trapTargetIndicator.GetComponentsInChildren<Renderer>(true);
            trapTargetIndicator.SetActive(false);
        }
    }

    static void DisablePreviewGameplay(GameObject preview)
    {
        foreach (Behaviour behaviour in
            preview.GetComponentsInChildren<Behaviour>(true))
            behaviour.enabled = false;
        foreach (Collider target in
            preview.GetComponentsInChildren<Collider>(true))
            target.enabled = false;
        foreach (Rigidbody body in
            preview.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    void UpdateFloorPropPreview(Vector3 cellCenter)
    {
        if (floorPropPreview == null || selectedObjectIndex < 0 ||
            database == null || selectedObjectIndex >= database.objectsData.Count)
        {
            return;
        }

        ObjectData selectedObject = database.objectsData[selectedObjectIndex];
        bool isValid = tileGridGenerator.TryGetFloorPropPreviewPose(
            cellCenter,
            selectedObject.Prefab,
            out Vector3 position,
            out Quaternion rotation);
        floorPropPreview.transform.SetPositionAndRotation(position, rotation);
        Color tint = isValid ? ValidPreviewColor : InvalidPreviewColor;
        ApplyPreviewTint(floorPropPreviewRenderers, tint);
        ApplyPreviewTint(cellIndicatorRenderers, tint);
    }

    void UpdateTrapPreview(Vector3 cellCenter)
    {
        if (trapPreview == null || selectedObjectIndex < 0 ||
            database == null || selectedObjectIndex >= database.objectsData.Count)
            return;
        ObjectData selectedObject = database.objectsData[selectedObjectIndex];
        bool isValid = tileGridGenerator.TryGetTrapPreviewPose(
            cellCenter,
            selectedObject.Prefab,
            selectedTrapCandidateIndex,
            out Vector3 mechanismPosition,
            out Quaternion mechanismRotation,
            out Vector3 hazardTargetPosition,
            out selectedTrapCandidate,
            out trapCandidateCount,
            out trapPlacementFailure);
        hasSelectedTrapCandidate = isValid;
        if (trapCandidateCount > 0)
            selectedTrapCandidateIndex = Mathf.Clamp(
                selectedTrapCandidateIndex, 0, trapCandidateCount - 1);
        else
            selectedTrapCandidateIndex = 0;
        trapPreview.transform.SetPositionAndRotation(
            mechanismPosition, mechanismRotation);
        Color tint = isValid ? ValidPreviewColor : InvalidPreviewColor;
        ApplyPreviewTint(trapPreviewRenderers, tint);
        ApplyPreviewTint(cellIndicatorRenderers, tint);
        if (trapHazardPreview != null)
        {
            float hazardPreviewZ = hazardTargetPosition.z;
            trapHazardPreview.SetPosition(
                0, WithZ(mechanismPosition, hazardPreviewZ));
            trapHazardPreview.SetPosition(
                1, WithZ(hazardTargetPosition, hazardPreviewZ));
            trapHazardPreview.startColor = tint;
            trapHazardPreview.endColor = tint;
            trapHazardPreview.enabled = isValid;
        }
        if (trapTargetIndicator != null)
        {
            trapTargetIndicator.SetActive(isValid);
            if (isValid)
            {
                trapTargetIndicator.transform.position = WithZ(
                    hazardTargetPosition,
                    GetCellIndicatorZ(hazardTargetPosition.z));
                ApplyPreviewTint(trapTargetIndicatorRenderers, tint);
            }
        }
        UpdateTrapFootprintIndicators(isValid, selectedTrapCandidate, tint);
        UpdateTrapConstructionPresentationPreview(
            isValid, selectedObject.Prefab, selectedTrapCandidate);
    }

    void UpdateTrapConstructionPresentationPreview(
        bool isValid,
        GameObject trapPrefab,
        TrapAttachmentPlacement attachment)
    {
        if (!isValid || trapPrefab == null)
        {
            DestroyTrapConstructionPresentationPreview();
            return;
        }

        bool unchanged = trapConstructionPresentationPreview != null &&
            trapPresentationPreviewServiceCell == attachment.ServiceCell &&
            trapPresentationPreviewTargetCell == attachment.TargetCell &&
            trapPresentationPreviewSurface == attachment.Surface;
        if (unchanged)
            return;

        DestroyTrapConstructionPresentationPreview();
        TrapAttachmentDefinition definition =
            trapPrefab.GetComponent<TrapAttachmentDefinition>();
        trapConstructionPresentationPreview =
            TrapConstructionPresentation.CreatePreview(
                tileGridGenerator, definition, attachment, transform);
        trapPresentationPreviewServiceCell = attachment.ServiceCell;
        trapPresentationPreviewTargetCell = attachment.TargetCell;
        trapPresentationPreviewSurface = attachment.Surface;
    }

    void DestroyTrapConstructionPresentationPreview()
    {
        if (trapConstructionPresentationPreview != null)
        {
            trapConstructionPresentationPreview
                .GetComponent<TrapConstructionPresentation>()?.Restore();
            trapConstructionPresentationPreview.SetActive(false);
            DestroyPreviewObject(trapConstructionPresentationPreview);
        }
        trapConstructionPresentationPreview = null;
        trapPresentationPreviewServiceCell = null;
        trapPresentationPreviewTargetCell = null;
        trapPresentationPreviewSurface = null;
    }

    void UpdateTrapFootprintIndicators(
        bool isValid,
        TrapAttachmentPlacement attachment,
        Color tint)
    {
        var previewCells = new List<Vector2Int>();
        if (isValid)
        {
            AddPreviewCells(
                previewCells, attachment.MechanismCells,
                attachment.ServiceCell);
            AddPreviewCells(
                previewCells, attachment.InfrastructureCells,
                attachment.ServiceCell);
            AddPreviewCells(
                previewCells, attachment.HazardCells,
                attachment.TargetCell);
        }

        while (trapFootprintIndicators.Count < previewCells.Count)
        {
            GameObject indicator = Instantiate(cellIndicator, transform);
            indicator.name = "Trap Footprint Preview";
            indicator.hideFlags = HideFlags.DontSave;
            DisablePreviewGameplay(indicator);
            trapFootprintIndicators.Add(indicator);
        }
        for (int i = 0; i < trapFootprintIndicators.Count; i++)
        {
            GameObject indicator = trapFootprintIndicators[i];
            bool active = i < previewCells.Count;
            indicator.SetActive(active);
            if (!active)
                continue;
            Vector2Int cell = previewCells[i];
            Vector3 position = tileGridGenerator.GetCellWorldPosition(
                cell.x, cell.y);
            indicator.transform.position = WithZ(
                position, GetCellIndicatorZ(position.z));
            ApplyPreviewTint(
                indicator.GetComponentsInChildren<Renderer>(true), tint);
        }
    }

    float GetCellIndicatorZ(float fallback) => cellIndicator != null
        ? cellIndicator.transform.position.z
        : fallback;

    static Vector3 WithZ(Vector3 position, float z)
    {
        position.z = z;
        return position;
    }

    static void AddPreviewCells(
        List<Vector2Int> destination,
        IReadOnlyList<Vector2Int> source,
        Vector2Int alreadyShown)
    {
        if (source == null)
            return;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != alreadyShown && !destination.Contains(source[i]))
                destination.Add(source[i]);
    }

    void ApplyPreviewTint(Renderer[] renderers, Color tint)
    {
        if (previewPropertyBlock == null)
            previewPropertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null)
                continue;

            target.GetPropertyBlock(previewPropertyBlock);
            previewPropertyBlock.SetColor("_BaseColor", tint);
            previewPropertyBlock.SetColor("_Color", tint);
            target.SetPropertyBlock(previewPropertyBlock);
            previewPropertyBlock.Clear();
        }
    }

    static void ClearPreviewTint(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].SetPropertyBlock(null);
    }

    void DestroyFloorPropPreview()
    {
        if (floorPropPreview != null)
        {
            floorPropPreview.SetActive(false);
            DestroyPreviewObject(floorPropPreview);
        }
        floorPropPreview = null;
        floorPropPreviewRenderers = System.Array.Empty<Renderer>();
    }

    void DestroyTrapPreview()
    {
        DestroyTrapConstructionPresentationPreview();
        if (trapPreview != null)
        {
            trapPreview.SetActive(false);
            DestroyPreviewObject(trapPreview);
        }
        if (trapHazardPreview != null)
            DestroyPreviewObject(trapHazardPreview.gameObject);
        if (trapHazardPreviewMaterial != null)
            DestroyPreviewObject(trapHazardPreviewMaterial);
        if (trapTargetIndicator != null)
            DestroyPreviewObject(trapTargetIndicator);
        for (int i = 0; i < trapFootprintIndicators.Count; i++)
            if (trapFootprintIndicators[i] != null)
                DestroyPreviewObject(trapFootprintIndicators[i]);
        trapFootprintIndicators.Clear();
        trapPreview = null;
        trapPreviewRenderers = System.Array.Empty<Renderer>();
        trapHazardPreview = null;
        trapHazardPreviewMaterial = null;
        trapTargetIndicator = null;
        trapTargetIndicatorRenderers = System.Array.Empty<Renderer>();
    }

    static void DestroyPreviewObject(UnityEngine.Object previewObject)
    {
        if (previewObject == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(previewObject);
            return;
        }
#endif
        Destroy(previewObject);
    }



}
