using UnityEngine;
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
    private Renderer[] floorPropPreviewRenderers = System.Array.Empty<Renderer>();
    private Renderer[] cellIndicatorRenderers = System.Array.Empty<Renderer>();
    private MaterialPropertyBlock previewPropertyBlock;
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
        StopPlacement();
        //CreateGroundTiles();
    }

    public void StartPlacement(int ID)
    {
        if (!buildingEnabled)
            return;

        StopPlacement(); // Ensure any existing placement is stopped before starting a new one
        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
        if (selectedObjectIndex <  0)
        {
            Debug.LogError($"Invalid object ID: {ID}");
            return;
        }
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        CreateFloorPropPreview(database.objectsData[selectedObjectIndex]);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnRightClicked += PlaceGround;
        inputManager.OnExit += StopPlacement;

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
        tileGridGenerator.PlaceGroundWorldPosition(grid.GetCellCenterWorld(gridPosition));
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
            tileGridGenerator.PlaceTrapWorldPosition(
                cellCenter, selectedObject.Prefab, selectedObject.ID);
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
            if (tileGridGenerator.PlaceFloorPropWorldPosition(
                    cellCenter, selectedObject.Prefab, selectedObject.ID))
            {
                lastDragCell = gridPosition;
            }
        }
        else
        {
            if (tileGridGenerator.ClickWorldPosition(
                    cellCenter, widthIntent))
            {
                lastDragCell = gridPosition;
            }
        }
    }

    public void SetWidthIntent(CellWidthIntent intent)
    {
        widthIntent = intent;
    }

    public void StopPlacement()
    {
        DestroyFloorPropPreview();
        ClearPreviewTint(cellIndicatorRenderers);
        selectedObjectIndex = -1;
        removingTraps = false;
        removingEntrance = false;
        editingEdges = false;
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnRightClicked -= PlaceGround;
        inputManager.OnExit -= StopPlacement;
        lastDragCell = null;
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
            Destroy(floorPropPreview);
        }
        floorPropPreview = null;
        floorPropPreviewRenderers = System.Array.Empty<Renderer>();
    }

    // private void CreateGroundTiles()
    // {
    //     Debug.Log("Creating ground tiles...");
    //     grid.CellToWorld(new Vector3Int(0, 0, 0));
    //     for (int y = 0; y < 50; y++)        {
    //         for (int x = 0; x >= -50; x--)
    //         {
    //             Vector3Int cellPosition = new Vector3Int(x, 0, y);
    //             Vector3 worldPosition = grid.CellToWorld(cellPosition);
    //             GameObject tile = Instantiate(database.objectsData[0].Prefab, worldPosition, Quaternion.identity);
    //             tile.transform.SetParent(tiles.transform);
    //         }
    //     }
    // }


}
