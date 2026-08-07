using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PropSocketAuthoringWindow : EditorWindow
{
    enum PositionSpace
    {
        SocketLocal,
        PrefabLocal
    }

    enum EditScope
    {
        CurrentPrefabStage,
        SelectedProjectPrefabs
    }

    EditScope editScope = EditScope.CurrentPrefabStage;
    string structureFilter;
    string laneFilter;
    string capturedSocketName;
    bool hasCapturedSocket;
    PropSocketRole roleFilter = PropSocketRole.Single;
    bool filterByRole;
    PositionSpace positionSpace = PositionSpace.SocketLocal;
    Vector3 offset;
    float nudgeAmount = 0.1f;
    string destinationLane = "NewLane";
    GameObject referencePrefab;
    bool matchRotation = true;

    [MenuItem("Tools/Prop Socket Authoring")]
    public static void Open() => GetWindow<PropSocketAuthoringWindow>("Prop Sockets");

    void OnEnable()
    {
        CaptureSelectedSocket();
    }

    void OnSelectionChange()
    {
        CaptureSelectedSocket();
        Repaint();
    }

    void OnGUI()
    {
        editScope = (EditScope)EditorGUILayout.EnumPopup("Edit Scope", editScope);
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool editingPrefabStage = editScope == EditScope.CurrentPrefabStage;
        bool hasSelectedPrefabs = HasSelectedPrefabPaths();
        if ((editingPrefabStage && stage == null) ||
            (!editingPrefabStage && !hasSelectedPrefabs))
        {
            EditorGUILayout.HelpBox(
                editingPrefabStage
                    ? "Open a tile prefab in Prefab Mode for the selected edit scope."
                    : "Select one or more prefab assets in the Project window for the selected edit scope.",
                MessageType.Warning);
            return;
        }

        if (editingPrefabStage)
        {
            EditorGUILayout.LabelField("Editing Current Prefab Stage", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Prefab", stage.prefabContentsRoot,
                typeof(GameObject), true);
            EditorGUILayout.HelpBox(
                "Operations affect the open prefab and support Undo. Save it normally when finished.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Editing Selected Project Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Operations affect all prefab assets selected in the Project window and save them immediately.",
                MessageType.Warning);
        }

        PropCatalog catalog = PropSocketAuthoringEditor.FindCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox(
                "No Prop Catalog was found. Existing ladder previews can use their filenames, but catalog-defined single props will not generate.",
                MessageType.Warning);
            if (GUILayout.Button("Create Default Prop Catalog"))
                CreateDefaultCatalog();
        }
        else if (GUILayout.Button("Select Prop Catalog"))
        {
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        CaptureSelectedSocket();
        if (!hasCapturedSocket)
        {
            EditorGUILayout.HelpBox(
                "Select a GameObject containing PropSocketAuthoring to choose the Structure and Source Lane used by this editor.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Socket Target", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Selected Socket", capturedSocketName);
            EditorGUILayout.TextField("Structure", structureFilter);
            EditorGUILayout.TextField("Source Lane", laneFilter);
        }
        filterByRole = EditorGUILayout.Toggle("Filter By Role", filterByRole);
        using (new EditorGUI.DisabledScope(!filterByRole))
            roleFilter = (PropSocketRole)EditorGUILayout.EnumPopup("Role", roleFilter);
        positionSpace = (PositionSpace)EditorGUILayout.EnumPopup(
            "Position Space", positionSpace);
        EditorGUILayout.HelpBox(
            positionSpace == PositionSpace.SocketLocal
                ? "Offsets rotate with each socket. Local X consistently moves left/right relative to differently oriented props."
                : "Offsets use the tile prefab's fixed X/Y/Z axes.",
            MessageType.None);
        offset = EditorGUILayout.Vector3Field("Position Offset", offset);
        nudgeAmount = Mathf.Max(0.0001f,
            EditorGUILayout.FloatField("Nudge Amount", nudgeAmount));
        destinationLane = EditorGUILayout.TextField("Destination Lane", destinationLane);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Nudge Matching Sockets", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("X -")) ModifyTargets(s => Nudge(s, Vector3.left));
            if (GUILayout.Button("X +")) ModifyTargets(s => Nudge(s, Vector3.right));
            if (GUILayout.Button("Y -")) ModifyTargets(s => Nudge(s, Vector3.down));
            if (GUILayout.Button("Y +")) ModifyTargets(s => Nudge(s, Vector3.up));
            if (GUILayout.Button("Z -")) ModifyTargets(s => Nudge(s, Vector3.back));
            if (GUILayout.Button("Z +")) ModifyTargets(s => Nudge(s, Vector3.forward));
        }

        EditorGUILayout.LabelField("Rotate Matching Sockets", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Z -90 deg")) ModifyTargets(s => RotateZ(s, -90f));
            if (GUILayout.Button("Z +90 deg")) ModifyTargets(s => RotateZ(s, 90f));
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Offset To Matching Sockets"))
            ModifyTargets(ApplyOffset);
        if (GUILayout.Button("Duplicate Lane With Offset"))
            ModifyTargets(DuplicateSocket);
        if (GUILayout.Button("Rename Matching Lane"))
            ModifyTargets(RenameLane);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Match Reference Prefab", EditorStyles.boldLabel);
        referencePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Reference Tile", referencePrefab, typeof(GameObject), false);
        matchRotation = EditorGUILayout.Toggle("Match Rotation", matchRotation);
        using (new EditorGUI.DisabledScope(referencePrefab == null))
            if (GUILayout.Button("Match Socket Offsets From Reference"))
                MatchReferenceOffsets();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Matching sockets in scope: {CountMatches()}", EditorStyles.miniBoldLabel);
    }

    bool Matches(PropSocketAuthoring socket)
    {
        return string.Equals(socket.structureId.Trim(), structureFilter.Trim(),
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(socket.laneId.Trim(), laneFilter.Trim(),
                StringComparison.OrdinalIgnoreCase)
            && (!filterByRole || socket.role == roleFilter);
    }

    void CaptureSelectedSocket()
    {
        GameObject selected = Selection.activeGameObject;
        PropSocketAuthoring socket = selected != null
            ? selected.GetComponent<PropSocketAuthoring>()
            : null;
        if (socket == null)
            return;

        structureFilter = string.IsNullOrWhiteSpace(socket.structureId)
            ? "Ladder"
            : socket.structureId.Trim();
        laneFilter = string.IsNullOrWhiteSpace(socket.laneId)
            ? "Default"
            : socket.laneId.Trim();
        capturedSocketName = socket.gameObject.name;
        hasCapturedSocket = true;
    }

    void ApplyOffset(PropSocketAuthoring socket)
    {
        socket.transform.localPosition += ConvertPositionDelta(socket, offset);
    }

    void Nudge(PropSocketAuthoring socket, Vector3 direction)
    {
        socket.transform.localPosition += ConvertPositionDelta(
            socket, direction * nudgeAmount);
    }

    Vector3 ConvertPositionDelta(PropSocketAuthoring socket, Vector3 delta)
    {
        return positionSpace == PositionSpace.SocketLocal
            ? socket.transform.localRotation * delta
            : delta;
    }

    static void RotateZ(PropSocketAuthoring socket, float degrees)
    {
        socket.transform.localRotation *= Quaternion.Euler(0f, 0f, degrees);
    }

    void RenameLane(PropSocketAuthoring socket)
    {
        socket.laneId = string.IsNullOrWhiteSpace(destinationLane)
            ? "Default"
            : destinationLane.Trim();
    }

    void DuplicateSocket(PropSocketAuthoring socket)
    {
        GameObject duplicate = Instantiate(socket.gameObject, socket.transform.parent);
        if (editScope == EditScope.CurrentPrefabStage)
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Prop Socket Lane");
        duplicate.name = $"{socket.gameObject.name} ({destinationLane})";
        duplicate.transform.localPosition = socket.transform.localPosition
            + ConvertPositionDelta(socket, offset);
        duplicate.transform.localRotation = socket.transform.localRotation;
        duplicate.transform.localScale = socket.transform.localScale;
        duplicate.GetComponent<PropSocketAuthoring>().laneId =
            string.IsNullOrWhiteSpace(destinationLane) ? "Default" : destinationLane.Trim();
    }

    void ModifyTargets(Action<PropSocketAuthoring> operation)
    {
        if (editScope == EditScope.CurrentPrefabStage)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
                return;
            int changed = ModifyRoot(stage.prefabContentsRoot, operation, true);
            SceneView.RepaintAll();
            Debug.Log($"Prop Socket Authoring changed {changed} socket(s). Rebake tile sockets when authoring is complete.");
            return;
        }

        int totalChanged = 0;
        foreach (string path in GetSelectedPrefabPaths())
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changed = ModifyRoot(root, operation, false);
                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    totalChanged += changed;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Prop Socket Authoring changed {totalChanged} socket(s) across selected prefabs. Rebake tile sockets when authoring is complete.");
    }

    int ModifyRoot(GameObject root, Action<PropSocketAuthoring> operation, bool recordUndo)
    {
        var matches = new List<PropSocketAuthoring>();
        foreach (PropSocketAuthoring socket in
            root.GetComponentsInChildren<PropSocketAuthoring>(true))
            if (Matches(socket))
                matches.Add(socket);

        if (recordUndo)
            Undo.SetCurrentGroupName("Edit Prop Sockets");
        foreach (PropSocketAuthoring socket in matches)
        {
            if (recordUndo)
            {
                Undo.RecordObject(socket, "Edit Prop Socket");
                Undo.RecordObject(socket.transform, "Edit Prop Socket Transform");
            }
            operation(socket);
            EditorUtility.SetDirty(socket);
            EditorUtility.SetDirty(socket.transform);
        }
        return matches.Count;
    }

    int CountMatches()
    {
        if (editScope == EditScope.CurrentPrefabStage)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            return CountMatches(stage.prefabContentsRoot);
        }

        int count = 0;
        foreach (string path in GetSelectedPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                count += CountMatches(prefab);
        }
        return count;
    }

    int CountMatches(GameObject root)
    {
        int count = 0;
        foreach (PropSocketAuthoring socket in
            root.GetComponentsInChildren<PropSocketAuthoring>(true))
            if (Matches(socket))
                count++;
        return count;
    }

    struct SocketPose
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    void MatchReferenceOffsets()
    {
        if (referencePrefab == null)
            return;

        var poses = new Dictionary<string, SocketPose>(StringComparer.OrdinalIgnoreCase);
        Transform referenceRoot = referencePrefab.transform;
        foreach (PropSocketAuthoring socket in
            referencePrefab.GetComponentsInChildren<PropSocketAuthoring>(true))
        {
            if (!Matches(socket))
                continue;
            string key = GetSocketKey(socket);
            if (poses.ContainsKey(key))
            {
                Debug.LogWarning($"Reference prefab has multiple sockets with key '{key}'. The first socket will be used.");
                continue;
            }
            poses[key] = new SocketPose
            {
                position = referenceRoot.InverseTransformPoint(socket.transform.position),
                rotation = Quaternion.Inverse(referenceRoot.rotation) * socket.transform.rotation
            };
        }

        int matched = 0;
        ModifyTargets(socket =>
        {
            if (!poses.TryGetValue(GetSocketKey(socket), out SocketPose pose))
                return;

            Transform destinationRoot = GetTopRoot(socket.transform);
            socket.transform.position = destinationRoot.TransformPoint(pose.position);
            if (matchRotation)
                socket.transform.rotation = destinationRoot.rotation * pose.rotation;
            matched++;
        });
        Debug.Log($"Matched {matched} socket offset(s) from reference prefab '{referencePrefab.name}'.");
    }

    static string GetSocketKey(PropSocketAuthoring socket)
    {
        return $"{socket.structureId.Trim()}|{socket.laneId.Trim()}|{socket.bundleId.Trim()}|{socket.role}|{socket.direction}";
    }

    static Transform GetTopRoot(Transform transform)
    {
        while (transform.parent != null)
            transform = transform.parent;
        return transform;
    }

    static IEnumerable<string> GetSelectedPrefabPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(
                ".prefab", StringComparison.OrdinalIgnoreCase))
                paths.Add(path);
        }
        return paths;
    }

    static bool HasSelectedPrefabPaths()
    {
        foreach (string _ in GetSelectedPrefabPaths())
            return true;
        return false;
    }

    static void CreateDefaultCatalog()
    {
        const string resourcesFolder = "Assets/Resources";
        const string path = resourcesFolder + "/PropCatalog.asset";
        PropCatalog existing = AssetDatabase.LoadAssetAtPath<PropCatalog>(path);
        if (existing != null)
        {
            Selection.activeObject = existing;
            return;
        }

        PropCatalog catalog = CreateInstance<PropCatalog>();
        var ladder = new PropDefinition
        {
            structureId = "Ladder",
            generationMode = PropGenerationMode.Chained,
            spawnChance = 1f,
            occupiesCell = true,
            useSocketRotation = false
        };
        ladder.prefabs.Add(new PropRolePrefab
        {
            role = PropSocketRole.Start,
            prefab = Resources.Load<GameObject>("Props/Ladder_Start")
        });
        ladder.prefabs.Add(new PropRolePrefab
        {
            role = PropSocketRole.Continue,
            prefab = Resources.Load<GameObject>("Props/Ladder_Continue")
        });
        ladder.prefabs.Add(new PropRolePrefab
        {
            role = PropSocketRole.End,
            prefab = Resources.Load<GameObject>("Props/Ladder_End")
        });
        catalog.definitions.Add(ladder);

        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
    }
}
