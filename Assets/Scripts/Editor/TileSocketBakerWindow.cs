using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TileSocketBakerWindow : EditorWindow
{
    string resourcesFolder = "Dungeon";
    int resolution = 8;
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

        foreach (int r in allowedRotations)
        {
            string profileName = $"{prefab.name}_Rot{r}";
            var profile = ScriptableObject.CreateInstance<TileSocketProfile>();
            profile.name = profileName;
            profile.baseTileName = prefab.name;
            profile.category = GetCategory(prefab.name);
            profile.rotation = r;
            profile.sourcePrefab = prefab;
            profile.resolution = resolution;
            profile.propSockets = BakePropSockets(prefab, r);

            // Sample the exact physical rotation used at runtime instead of
            // trying to permute and reverse the unrotated edge masks.
            var rotatedInstance = InstantiateRotated(prefab, r);
            profile.westHash = PortalEdgeAnalyzer.GeneratePortalMask(rotatedInstance, TileSide.West, resolution, 6, 0.02f);
            profile.northHash = PortalEdgeAnalyzer.GeneratePortalMask(rotatedInstance, TileSide.North, resolution, 6, 0.02f);
            profile.southHash = PortalEdgeAnalyzer.GeneratePortalMask(rotatedInstance, TileSide.South, resolution, 6, 0.02f);
            profile.eastHash = PortalEdgeAnalyzer.GeneratePortalMask(rotatedInstance, TileSide.East, resolution, 6, 0.02f);
            GameObject.DestroyImmediate(rotatedInstance);

            string path = $"{folder}/{profileName}.asset";
            var existingProfile = AssetDatabase.LoadAssetAtPath<TileSocketProfile>(path);
            if (existingProfile != null)
            {
                EditorUtility.CopySerialized(profile, existingProfile);
                DestroyImmediate(profile);
                profile = existingProfile;
                profile.name = profileName;
                EditorUtility.SetDirty(profile);
            }
            else
            {
                AssetDatabase.CreateAsset(profile, path);
            }

            db.tiles.Add(profile);
        }
    }

    BuildAdjacency(db);

    const string databasePath = "Assets/TileAdjacencyDatabase.asset";
    var existingDatabase = AssetDatabase.LoadAssetAtPath<TileAdjacencyDatabase>(databasePath);
    if (existingDatabase != null)
    {
        EditorUtility.CopySerialized(db, existingDatabase);
        DestroyImmediate(db);
        EditorUtility.SetDirty(existingDatabase);
    }
    else
    {
        AssetDatabase.CreateAsset(db, databasePath);
    }
    AssetDatabase.SaveAssets();
    Debug.Log("Baking complete!");
}

List<BakedPropSocket> BakePropSockets(GameObject prefab, int rotationIndex)
{
    var bakedSockets = new List<BakedPropSocket>();
    foreach (var authoredSocket in prefab.GetComponentsInChildren<PropSocketAuthoring>(true))
    {
        bakedSockets.Add(new BakedPropSocket
        {
            structureId = authoredSocket.structureId.Trim(),
            laneId = string.IsNullOrWhiteSpace(authoredSocket.laneId)
                ? "Default"
                : authoredSocket.laneId.Trim(),
            compatibleLaneIds = BakeCompatibleLaneIds(authoredSocket),
            bundleId = string.IsNullOrWhiteSpace(authoredSocket.bundleId)
                ? "Default"
                : authoredSocket.bundleId.Trim(),
            selectionWeight = Mathf.Max(0f, authoredSocket.selectionWeight),
            role = authoredSocket.role,
            direction = RotateDirection(authoredSocket.direction, rotationIndex),
            allowsTraversalExit = authoredSocket.allowsTraversalExit,
            platformPolicy = authoredSocket.platformPolicy,
            localPosition = prefab.transform.InverseTransformPoint(authoredSocket.transform.position),
            localRotation = Quaternion.Inverse(prefab.transform.rotation) * authoredSocket.transform.rotation
        });
    }

    return bakedSockets;
}

List<string> BakeCompatibleLaneIds(PropSocketAuthoring authoredSocket)
{
    var result = new List<string>();
    var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    string primaryLane = string.IsNullOrWhiteSpace(authoredSocket.laneId)
        ? "Default"
        : authoredSocket.laneId.Trim();
    seen.Add(primaryLane);

    if (authoredSocket.compatibleLaneIds == null)
        return result;

    foreach (string lane in authoredSocket.compatibleLaneIds)
    {
        if (string.IsNullOrWhiteSpace(lane))
            continue;
        string normalized = lane.Trim();
        if (seen.Add(normalized))
            result.Add(normalized);
    }
    return result;
}

PropSocketDirection RotateDirection(PropSocketDirection direction, int clockwiseRotations)
{
    int value = ((int)direction + clockwiseRotations) % 4;
    return (PropSocketDirection)value;
}
    
static GameObject InstantiateRotated(GameObject prefab, int rotationIndex)
{
    var instance = GameObject.Instantiate(prefab);
    // Rotation indices progress clockwise when viewed from +Z.
    instance.transform.rotation = Quaternion.Euler(0, 0, rotationIndex * -90f);
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
            Debug.Log($"Instantiated {prefab.name} with clockwise rotation {r * 90} degrees. Position: {instance.transform.position}, Rotation: {instance.transform.rotation.eulerAngles}");
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
            // Opposing edges are sampled in the same world-axis direction:
            // north/south left-to-right and east/west bottom-to-top.
            if (a.northHash == b.southHash)
                a.northMatches.Add(ProfileID(b));

            if (a.southHash == b.northHash)
                a.southMatches.Add(ProfileID(b));

            if (a.eastHash == b.westHash)
                a.eastMatches.Add(ProfileID(b));    

            if (a.westHash == b.eastHash)
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
    if (prefabName.EndsWith("_U")) return 'U';
    if (prefabName.EndsWith("_D")) return 'D';
    return 'L'; // Default to L (no symmetry, all rotations)
}

TileCategory GetCategory(string prefabName)
{
    if (prefabName.StartsWith("Wide_")) return TileCategory.Wide;
    if (prefabName.StartsWith("Narrow_")) return TileCategory.Narrow;
    if (prefabName.StartsWith("Transition_")) return TileCategory.Transition;
    if (prefabName.StartsWith("Starter_")) return TileCategory.Starter;
    if (prefabName.StartsWith("Ground_")) return TileCategory.Ground;
    return TileCategory.Unspecified;
}

List<int> GetAllowedRotations(char symmetry)
{
    switch (symmetry)
    {
        case 'X': return new List<int> { 0 }; // Only one orientation
        case 'T': return new List<int> { 0, 1, 2, 3 };
        case 'I': return new List<int> { 0, 1 }; // 0° and 90°
        case 'D': return new List<int> { 0, 2 }; // 0° and 180°
        case 'L': return new List<int> { 0, 1, 2, 3 }; // All rotations
        case 'U': return new List<int> { 0, 3 }; // 0° and 270° clockwise
        default: return new List<int> { 0, 1, 2, 3 };
    }
}

}

