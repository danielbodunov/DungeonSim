using UnityEngine;
using UnityEditor;
using System.IO;

public class ModularPrefabGeneratorWindow : EditorWindow
{
    private bool addCollider = true;
    private string outputFolder = "Assets/GeneratedPrefabs";

    [MenuItem("Tools/Modular Prefab Generator")]
    public static void ShowWindow()
    {
        GetWindow<ModularPrefabGeneratorWindow>("Modular Prefabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Modular Prefab Generator", EditorStyles.boldLabel);

        addCollider = EditorGUILayout.Toggle("Add Box Collider", addCollider);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate From Selected"))
        {
            GeneratePrefabs();
        }
    }

    private void GeneratePrefabs()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError("No GameObject selected.");
            return;
        }

        EnsureFolderExists(outputFolder);

        foreach (Transform child in selected.transform)
        {
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
                continue;

            // =========================
            // Create Root
            // =========================
            GameObject root = new GameObject(child.name);

            // Preserve pivot by NOT altering transform
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // =========================
            // Create Mesh Child
            // =========================
            GameObject meshObject = new GameObject("Mesh");
            meshObject.transform.SetParent(root.transform);

            // Preserve original local transform
            meshObject.transform.localPosition = child.localPosition;
            meshObject.transform.localRotation = child.localRotation;
            meshObject.transform.localScale = child.localScale;

            // Copy mesh + materials
            MeshFilter newMF = meshObject.AddComponent<MeshFilter>();
            newMF.sharedMesh = meshFilter.sharedMesh;

            MeshRenderer newMR = meshObject.AddComponent<MeshRenderer>();
            newMR.sharedMaterials = meshRenderer.sharedMaterials;

            // =========================
            // Optional Collider
            // =========================
            if (addCollider)
            {
                AddFittedBoxCollider(root);
            }

            // =========================
            // Save Prefab
            // =========================
            string prefabPath = Path.Combine(outputFolder, child.name + ".prefab");
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Modular prefabs generated successfully.");
    }

    private void AddFittedBoxCollider(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // Convert world bounds to root local space
        Vector3 localCenter = root.transform.InverseTransformPoint(combinedBounds.center);
        Vector3 localSize = root.transform.InverseTransformVector(combinedBounds.size);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = localCenter;
        collider.size = localSize;
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] split = folderPath.Split('/');
        string current = split[0];

        for (int i = 1; i < split.Length; i++)
        {
            string next = current + "/" + split[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, split[i]);
            }

            current = next;
        }
    }
}
