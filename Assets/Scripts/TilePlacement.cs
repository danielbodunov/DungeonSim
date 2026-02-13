using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class TilePlacement : MonoBehaviour
{
    [SerializeField]
    private GameObject mouseIndicator, cellIndicator;

    [SerializeField]
    private InputManager inputManager;


    [SerializeField]
    private Grid grid;

    [SerializeField]
    private ObjectsDatabaseSO database;
    private int selectedObjectIndex = -1;

    [SerializeField]
    private GameObject gridVisualization;

    [SerializeField]
    private GameObject tiles;

    private void Start()
    {
        StopPlacement();
        CreateGroundTiles();
    }

    public void StartPlacement(int ID)
    {
        StopPlacement(); // Ensure any existing placement is stopped before starting a new one
        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
        if (selectedObjectIndex <  0)
        {
            Debug.LogError($"Invalid object ID: {ID}");
            return;
        }
        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;

    }


    private void PlaceStructure()
    {
        if(inputManager.IsPointerOverUI())
        {
            return;
        }

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        GameObject newObject = Instantiate(database.objectsData[selectedObjectIndex].Prefab);
        newObject.transform.position = grid.CellToWorld(gridPosition);
        
    }
    
    private void StopPlacement()
    {
        selectedObjectIndex = -1;
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;
    }


    private void Update()
    {

        if (selectedObjectIndex < 0)
        { 
            return; 
        }
        
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        mouseIndicator.transform.position = mousePosition;
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);

    }

    private void CreateGroundTiles()
    {
        Debug.Log("Creating ground tiles...");
        grid.CellToWorld(new Vector3Int(0, 0, 0));
        for (int y = 0; y <= -50; y++)        {
            for (int x = 0; x <= 50; x++)
            {
                Vector3Int cellPosition = new Vector3Int(x, 0, y);
                Vector3 worldPosition = grid.CellToWorld(cellPosition);
                GameObject tile = Instantiate(database.objectsData[0].Prefab, worldPosition, Quaternion.identity);
                tile.transform.SetParent(tiles.transform);
                //tile.GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }


}
