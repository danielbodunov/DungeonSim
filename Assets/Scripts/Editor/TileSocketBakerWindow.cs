using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TileSocketBakerWindow : EditorWindow
{
    string resourcesFolder = "Dungeon";
    int resolution = 16;
    bool allowRotation = true;
    bool visualizeSamples = true;

    [MenuItem("Tools/Tile Socket Baker")]
    static void Open() => GetWindow<TileSocketBakerWindow>();

    void OnGUI()
    {
        resourcesFolder = EditorGUILayout.TextField("Resources Folder", resourcesFolder);
        resolution = EditorGUILayout.IntSlider("Edge Resolution", resolution, 4, 64);
        allowRotation = EditorGUILayout.Toggle("Allow Rotation", allowRotation);

        if (GUILayout.Button("Bake Tile Sockets"))
            Bake();


        if (GUILayout.Button("Test Rotation Instantiated Prefabs"))
            TestRotationInstantiatedPrefabs();
    }

void Bake()
{
    Debug.Log("Baking tile sockets...");
    var prefabs = Resources.LoadAll<GameObject>(resourcesFolder);

    var db = ScriptableObject.CreateInstance<TileAdjacencyDatabase>();
    db.tiles = new();

    string folder = "Assets/Resources/TileProfiles";

    if (!AssetDatabase.IsValidFolder(folder))
        AssetDatabase.CreateFolder("Assets", "Resources/TileProfiles");

    foreach (var prefab in prefabs)
    {
        // Determine symmetry from prefab name suffix
        var symmetry = GetSymmetry(prefab.name);
        var allowedRotations = GetAllowedRotations(symmetry);

        // Generate base hashes for rotation 0
        var baseInstance = InstantiateRotated(prefab, 0);
        string baseWest = PortalEdgeAnalyzer.GeneratePortalMask(baseInstance, TileSide.West, resolution, 6, 0.02f);
        string baseNorth = PortalEdgeAnalyzer.GeneratePortalMask(baseInstance, TileSide.North, resolution, 6, 0.02f);
        string baseSouth = PortalEdgeAnalyzer.GeneratePortalMask(baseInstance, TileSide.South, resolution, 6, 0.02f);
        string baseEast = PortalEdgeAnalyzer.GeneratePortalMask(baseInstance, TileSide.East, resolution, 6, 0.02f);
        GameObject.DestroyImmediate(baseInstance);

        foreach (int r in allowedRotations)
        {
            var profile = ScriptableObject.CreateInstance<TileSocketProfile>();
            profile.baseTileName = prefab.name;
            profile.rotation = r;
            profile.sourcePrefab = prefab;
            profile.resolution = resolution;

            // Rotate hashes based on r
            var rotatedHashes = RotateHashes(baseWest, baseNorth, baseSouth, baseEast, r);
            profile.westHash = rotatedHashes.west;
            profile.northHash = rotatedHashes.north;
            profile.southHash = rotatedHashes.south;
            profile.eastHash = rotatedHashes.east;

            string path = $"{folder}/{prefab.name}_Rot{r}.asset";
            AssetDatabase.CreateAsset(profile, path);

            db.tiles.Add(profile);
        }
    }

    BuildAdjacency(db);

    AssetDatabase.CreateAsset(db, "Assets/TileAdjacencyDatabase.asset");
    AssetDatabase.SaveAssets();
    Debug.Log("Baking complete!");
}
    
static GameObject InstantiateRotated(GameObject prefab, int rotationIndex)
{
    var instance = GameObject.Instantiate(prefab);
    instance.transform.rotation = Quaternion.Euler(0, 0, rotationIndex * 90f);
    return instance;
}

void TestRotationInstantiatedPrefabs()
{
    var prefabs = Resources.LoadAll<GameObject>(resourcesFolder);
    foreach (var prefab in prefabs)
    {
        for (int r = 0; r < 4; r++)
        {
            var instance = InstantiateRotated(prefab, r);
            Debug.Log($"Instantiated {prefab.name} with rotation {r * 90} degrees. Position: {instance.transform.position}, Rotation: {instance.transform.rotation.eulerAngles}");
        }
    }
}

void BuildAdjacency(TileAdjacencyDatabase db)
{
    Debug.Log("Building adjacency database...");
    foreach (var a in db.tiles)
    {
        foreach (var b in db.tiles)
        {
            if (a.northHash == Reverse(b.southHash))
                a.northMatches.Add(ProfileID(b));

            if (a.southHash == Reverse(b.northHash))
                a.southMatches.Add(ProfileID(b));

            if (a.eastHash == Reverse(b.westHash))
                a.eastMatches.Add(ProfileID(b));    

            if (a.westHash == Reverse(b.eastHash))
                a.westMatches.Add(ProfileID(b)); 
        }
        EditorUtility.SetDirty(a);
    }
}

string Reverse(string s)
{
    char[] arr = s.ToCharArray();
    System.Array.Reverse(arr);
    return new string(arr);
}

string ProfileID(TileSocketProfile p)
{
    return $"{p.baseTileName}_R{p.rotation}";
}

(string west, string north, string south, string east) RotateHashes(string baseWest, string baseNorth, string baseSouth, string baseEast, int r)
{
    switch (r)
    {
        case 0:
            return (baseWest, baseNorth, baseSouth, baseEast);
        case 1: // 90° clockwise
            return (Reverse(baseNorth), Reverse(baseEast), Reverse(baseSouth), Reverse(baseWest));
        case 2: // 180°
            return (baseEast, baseSouth, baseNorth, baseWest);
        case 3: // 270° clockwise
            return (Reverse(baseSouth), Reverse(baseWest), Reverse(baseNorth), Reverse(baseEast));
        default:
            return (baseWest, baseNorth, baseSouth, baseEast);
    }
}

char GetSymmetry(string prefabName)
{
    if (prefabName.EndsWith("_X")) return 'X';
    if (prefabName.EndsWith("_T")) return 'T';
    if (prefabName.EndsWith("_I")) return 'I';
    if (prefabName.EndsWith("_L")) return 'L';
    if (prefabName.EndsWith("_D")) return 'D';
    return 'L'; // Default to L (no symmetry, all rotations)
}

List<int> GetAllowedRotations(char symmetry)
{
    switch (symmetry)
    {
        case 'X': return new List<int> { 0 }; // Only one orientation
        case 'T':
        case 'I':
        case 'D': return new List<int> { 0, 2 }; // 0° and 180°
        case 'L': return new List<int> { 0, 1, 2, 3 }; // All rotations
        default: return new List<int> { 0, 1, 2, 3 };
    }
}

}

