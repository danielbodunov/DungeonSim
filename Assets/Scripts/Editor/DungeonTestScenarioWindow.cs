using System;
using UnityEditor;
using UnityEngine;

public sealed class DungeonTestScenarioWindow : EditorWindow
{
    DungeonTestScenario selectedScenario;
    DungeonTestScenario capturedScenario;
    string scenarioName = "New Dungeon Scenario";
    string description = string.Empty;
    string intendedTestPurpose = string.Empty;
    string lastStatus = "Capture a running dungeon to begin.";
    MessageType lastStatusType = MessageType.Info;

    [MenuItem("Tools/Dungeon Test Scenarios")]
    public static void Open()
    {
        GetWindow<DungeonTestScenarioWindow>("Dungeon Scenarios");
    }

    void OnEnable()
    {
        if (Selection.activeObject is DungeonTestScenario scenario)
            SelectScenario(scenario);
    }

    void OnDisable()
    {
        DestroyCapturedScenario();
    }

    void OnSelectionChange()
    {
        if (Selection.activeObject is DungeonTestScenario scenario &&
            scenario != selectedScenario)
        {
            SelectScenario(scenario);
            Repaint();
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Reusable Dungeon Test Scenarios", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scenario actions use the initialized dungeon in Play Mode and " +
            "reconstruct it through normal production placement APIs.",
            MessageType.Info);

        DungeonTestScenario nextScenario =
            (DungeonTestScenario)EditorGUILayout.ObjectField(
                "Selected Scenario",
                selectedScenario,
                typeof(DungeonTestScenario),
                false);
        if (nextScenario != selectedScenario)
            SelectScenario(nextScenario);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scenario Metadata", EditorStyles.boldLabel);
        scenarioName = EditorGUILayout.TextField("Name", scenarioName);
        EditorGUILayout.LabelField("Description");
        description = EditorGUILayout.TextArea(
            description, GUILayout.MinHeight(45f));
        EditorGUILayout.LabelField("Intended Test Purpose");
        intendedTestPurpose = EditorGUILayout.TextArea(
            intendedTestPurpose, GUILayout.MinHeight(45f));

        EditorGUILayout.Space();
        DrawCaptureSection();
        EditorGUILayout.Space();
        DrawPlaybackSection();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(lastStatus, lastStatusType);
    }

    void DrawCaptureSection()
    {
        EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!CanUseCurrentDungeon()))
        {
            if (GUILayout.Button("Capture Current Dungeon"))
                CaptureCurrentDungeon();
        }

        if (capturedScenario == null)
        {
            EditorGUILayout.HelpBox(
                "No pending capture. Capture does not modify an asset until " +
                "Save New or Update Existing is chosen.",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.LabelField(
                $"Pending: {capturedScenario.TileCells.Count} cells, " +
                $"{capturedScenario.Traps.Count} traps, " +
                $"{capturedScenario.FloorProps.Count} floor props, " +
                (capturedScenario.Entrance != null ? "entrance" : "no entrance"),
                EditorStyles.miniBoldLabel);
        }

        using (new EditorGUI.DisabledScope(capturedScenario == null))
        {
            if (GUILayout.Button("Save Captured Scenario As New..."))
                SaveCapturedAsNew();
        }

        using (new EditorGUI.DisabledScope(
            capturedScenario == null || selectedScenario == null))
        {
            if (GUILayout.Button("Update Selected Scenario From Capture..."))
                UpdateSelectedFromCapture();
        }
    }

    void DrawPlaybackSection()
    {
        EditorGUILayout.LabelField("Load / Reset", EditorStyles.boldLabel);
        bool canApply = selectedScenario != null && CanUseCurrentDungeon();
        using (new EditorGUI.DisabledScope(!canApply))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Selected Scenario"))
                    ApplySelectedScenario("Loaded");
                if (GUILayout.Button("Reset Selected Scenario"))
                    ApplySelectedScenario("Reset");
            }
        }
        EditorGUILayout.HelpBox(
            "Reset reapplies the selected asset's authored initial state, " +
            "discarding runtime changes made since it was loaded.",
            MessageType.None);
    }

    void CaptureCurrentDungeon()
    {
        if (!TryFindDungeon(
                out TileGridGenerator grid,
                out TilePlacement placement,
                out string failure))
        {
            SetStatus(failure, MessageType.Error);
            return;
        }

        DestroyCapturedScenario();
        capturedScenario = CreateInstance<DungeonTestScenario>();
        capturedScenario.hideFlags = HideFlags.HideAndDontSave;
        capturedScenario.SetMetadata(
            scenarioName, description, intendedTestPurpose);
        if (!capturedScenario.CaptureFrom(
                grid, placement.AvailableObjects, out string report))
        {
            DestroyCapturedScenario();
            SetStatus(report, MessageType.Error);
            return;
        }

        SetStatus(report, MessageType.Info);
    }

    void SaveCapturedAsNew()
    {
        capturedScenario.SetMetadata(
            scenarioName, description, intendedTestPurpose);
        string defaultName = MakeSafeAssetName(scenarioName);
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Dungeon Test Scenario",
            defaultName,
            "asset",
            "Choose where to save the reusable dungeon scenario.");
        if (string.IsNullOrEmpty(path))
            return;

        path = AssetDatabase.GenerateUniqueAssetPath(path);
        var asset = CreateInstance<DungeonTestScenario>();
        asset.CopyFrom(capturedScenario);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        SelectScenario(asset);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        SetStatus($"Saved new scenario at '{path}'.", MessageType.Info);
    }

    void UpdateSelectedFromCapture()
    {
        if (!EditorUtility.DisplayDialog(
                "Update Dungeon Test Scenario?",
                $"Replace the authored state in '{selectedScenario.name}' " +
                "with the pending capture? This intentionally changes the " +
                "existing scenario asset.",
                "Update Scenario",
                "Cancel"))
        {
            return;
        }

        capturedScenario.SetMetadata(
            scenarioName, description, intendedTestPurpose);
        Undo.RecordObject(selectedScenario, "Update Dungeon Test Scenario");
        selectedScenario.CopyFrom(capturedScenario);
        EditorUtility.SetDirty(selectedScenario);
        AssetDatabase.SaveAssets();
        SetStatus(
            $"Updated scenario '{selectedScenario.name}' from the pending capture.",
            MessageType.Info);
    }

    void ApplySelectedScenario(string verb)
    {
        if (!TryFindDungeon(
                out TileGridGenerator grid,
                out TilePlacement placement,
                out string failure))
        {
            SetStatus(failure, MessageType.Error);
            return;
        }

        placement.StopPlacement();
        if (!selectedScenario.TryApplyTo(grid, out string report))
        {
            SetStatus(report, MessageType.Error);
            return;
        }

        SetStatus($"{verb}: {report}", MessageType.Info);
    }

    void SelectScenario(DungeonTestScenario scenario)
    {
        selectedScenario = scenario;
        if (scenario == null)
            return;

        scenarioName = scenario.ScenarioName;
        description = scenario.Description;
        intendedTestPurpose = scenario.IntendedTestPurpose;
    }

    static bool CanUseCurrentDungeon()
    {
        if (!Application.isPlaying)
            return false;
        TileGridGenerator grid = FindAnyObjectByType<TileGridGenerator>();
        return grid != null && grid.IsInitialized &&
            FindAnyObjectByType<TilePlacement>() != null;
    }

    static bool TryFindDungeon(
        out TileGridGenerator grid,
        out TilePlacement placement,
        out string failure)
    {
        grid = FindAnyObjectByType<TileGridGenerator>();
        placement = FindAnyObjectByType<TilePlacement>();
        if (!Application.isPlaying)
        {
            failure = "Enter Play Mode before capturing, loading, or resetting a scenario.";
            return false;
        }
        if (grid == null || !grid.IsInitialized)
        {
            failure = "No initialized TileGridGenerator was found in the running scene.";
            return false;
        }
        if (placement == null)
        {
            failure = "No TilePlacement object catalog was found in the running scene.";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    static string MakeSafeAssetName(string value)
    {
        string result = string.IsNullOrWhiteSpace(value)
            ? "DungeonTestScenario"
            : value.Trim();
        foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            result = result.Replace(invalid, '_');
        return result;
    }

    void SetStatus(string message, MessageType type)
    {
        lastStatus = message;
        lastStatusType = type;
        Repaint();
    }

    void DestroyCapturedScenario()
    {
        if (capturedScenario != null)
            DestroyImmediate(capturedScenario);
        capturedScenario = null;
    }
}
