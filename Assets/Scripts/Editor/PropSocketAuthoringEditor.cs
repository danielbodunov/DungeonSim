using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(PropSocketAuthoring))]
public class PropSocketAuthoringEditor : Editor
{
    static Material previewMaterial;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DrawCompatibleLaneButtons();
        DrawBundleIdPicker();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "The socketed prefab or all items in its matching bundle are previewed in the Scene view. Create a Props/Prop Catalog asset to override the Resources/Props naming convention.",
            MessageType.Info);

        if (GUILayout.Button("Open Prop Socket Authoring"))
            PropSocketAuthoringWindow.Open();
    }

    void DrawCompatibleLaneButtons()
    {
        serializedObject.Update();
        SerializedProperty roleProperty = serializedObject.FindProperty("role");
        if ((PropSocketRole)roleProperty.enumValueIndex != PropSocketRole.Continue)
            return;

        SerializedProperty lanesProperty =
            serializedObject.FindProperty("compatibleLaneIds");
        EditorGUILayout.LabelField("Continue Lane Shortcuts", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Accept Left"))
                AddCompatibleLane(lanesProperty, "Left");
            if (GUILayout.Button("Accept Right"))
                AddCompatibleLane(lanesProperty, "Right");
            if (GUILayout.Button("Accept Both"))
            {
                AddCompatibleLane(lanesProperty, "Left");
                AddCompatibleLane(lanesProperty, "Right");
            }
            if (GUILayout.Button("Clear"))
                lanesProperty.ClearArray();
        }
        serializedObject.ApplyModifiedProperties();
    }

    static void AddCompatibleLane(SerializedProperty lanesProperty, string laneId)
    {
        for (int i = 0; i < lanesProperty.arraySize; i++)
            if (string.Equals(
                    lanesProperty.GetArrayElementAtIndex(i).stringValue,
                    laneId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

        int index = lanesProperty.arraySize;
        lanesProperty.InsertArrayElementAtIndex(index);
        lanesProperty.GetArrayElementAtIndex(index).stringValue = laneId;
    }

    void DrawBundleIdPicker()
    {
        serializedObject.Update();
        SerializedProperty structureProperty = serializedObject.FindProperty("structureId");
        SerializedProperty bundleProperty = serializedObject.FindProperty("bundleId");
        PropDefinition definition = FindCatalog()?.Find(structureProperty.stringValue);
        if (definition == null)
            return;

        var suggestions = new List<string>();
        AddBundleSuggestions(definition, suggestions);
        if (suggestions.Count == 0)
            return;

        suggestions.Sort(StringComparer.OrdinalIgnoreCase);
        var options = new string[suggestions.Count + 1];
        options[0] = "Choose existing...";
        for (int i = 0; i < suggestions.Count; i++)
            options[i + 1] = suggestions[i];

        int selected = EditorGUILayout.Popup("Existing Bundle IDs", 0, options);
        if (selected > 0)
        {
            bundleProperty.stringValue = suggestions[selected - 1];
            serializedObject.ApplyModifiedProperties();
        }
    }

    static void AddBundleSuggestions(
        PropDefinition definition, List<string> suggestions)
    {
        foreach (PropLaneVariant lane in definition.laneVariants)
        {
            if (lane == null)
                continue;

            foreach (PropPieceBundle bundle in lane.bundles)
            {
                if (bundle == null || string.IsNullOrWhiteSpace(bundle.bundleId))
                    continue;
                if (!suggestions.Exists(id => string.Equals(
                    id, bundle.bundleId, StringComparison.OrdinalIgnoreCase)))
                    suggestions.Add(bundle.bundleId);
            }
        }
    }

    void OnSceneGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        PropSocketAuthoring socket = (PropSocketAuthoring)target;
        PropDefinition definition = FindCatalog()?.Find(socket.structureId);

        // Authoring previews always follow the socket transform so rotated
        // tile setups can be aligned visually. Runtime rotation policy remains
        // controlled independently by the catalog definition.
        Quaternion rotation = socket.transform.rotation;
        if (definition != null)
            rotation *= Quaternion.Euler(definition.rotationOffset);

        Matrix4x4 previewRoot = Matrix4x4.TRS(
            socket.transform.position, rotation, Vector3.one);
        Material material = GetPreviewMaterial();
        material.SetPass(0);

        PropPieceBundle bundle = definition?.GetBundle(
            socket.laneId, socket.role, socket.bundleId);
        if (bundle != null && bundle.items.Count > 0)
        {
            foreach (PropBundleItem item in bundle.items)
            {
                if (item == null || item.prefab == null)
                    continue;
                Matrix4x4 itemMatrix = Matrix4x4.TRS(
                    item.localPosition,
                    Quaternion.Euler(item.localRotation),
                    Vector3.one);
                DrawPrefab(item.prefab, previewRoot * itemMatrix);
            }
        }
        else
        {
            GameObject prefab = FindPreviewPrefab(socket, definition);
            if (prefab != null)
                DrawPrefab(prefab, previewRoot);
        }

        SceneView.RepaintAll();
    }

    static void DrawPrefab(GameObject prefab, Matrix4x4 previewRoot)
    {
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;

            Matrix4x4 relative = prefab.transform.worldToLocalMatrix
                * filter.transform.localToWorldMatrix;
            Matrix4x4 matrix = previewRoot * relative;
            for (int submesh = 0; submesh < filter.sharedMesh.subMeshCount; submesh++)
                Graphics.DrawMeshNow(filter.sharedMesh, matrix, submesh);
        }
    }

    internal static GameObject FindPreviewPrefab(
        PropSocketAuthoring socket, out PropDefinition definition)
    {
        definition = FindCatalog()?.Find(socket.structureId);
        return FindPreviewPrefab(socket, definition);
    }

    static GameObject FindPreviewPrefab(
        PropSocketAuthoring socket, PropDefinition definition)
    {
        GameObject configured = definition?.GetPrefab(socket.role);
        if (configured != null)
            return configured;

        string roleName = socket.role == PropSocketRole.Single
            ? socket.structureId
            : $"{socket.structureId}_{socket.role}";
        return Resources.Load<GameObject>($"Props/{roleName}");
    }

    internal static PropCatalog FindCatalog()
    {
        string[] guids = AssetDatabase.FindAssets("t:PropCatalog");
        if (guids.Length == 0)
            return null;
        return AssetDatabase.LoadAssetAtPath<PropCatalog>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static Material GetPreviewMaterial()
    {
        if (previewMaterial != null)
            return previewMaterial;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        previewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        previewMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_Cull", (int)CullMode.Off);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.SetColor("_Color", new Color(0.1f, 0.9f, 1f, 0.28f));
        return previewMaterial;
    }

}
