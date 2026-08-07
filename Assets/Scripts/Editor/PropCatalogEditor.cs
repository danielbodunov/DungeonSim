using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PropCatalog))]
public class PropCatalogEditor : Editor
{
    int definitionIndex;
    int laneIndex;
    int bundleIndex = -1;
    bool showLegacyPrefabs;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty definitions = serializedObject.FindProperty("definitions");

        DrawStructureTabs(definitions);
        if (definitions.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add a structure definition to begin.", MessageType.Info);
            if (GUILayout.Button("Add Structure"))
                AddDefinition(definitions);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        definitionIndex = Mathf.Clamp(definitionIndex, 0, definitions.arraySize - 1);
        SerializedProperty definition = definitions.GetArrayElementAtIndex(definitionIndex);
        if (!DrawDefinitionSettings(definitions, definition))
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }
        DrawLaneTabs(definition.FindPropertyRelative("laneVariants"));

        serializedObject.ApplyModifiedProperties();
    }

    void DrawStructureTabs(SerializedProperty definitions)
    {
        EditorGUILayout.LabelField("Structures", EditorStyles.boldLabel);
        var labels = new string[definitions.arraySize];
        for (int i = 0; i < definitions.arraySize; i++)
        {
            string id = definitions.GetArrayElementAtIndex(i)
                .FindPropertyRelative("structureId").stringValue;
            labels[i] = string.IsNullOrWhiteSpace(id) ? $"Structure {i + 1}" : id;
        }

        if (labels.Length > 0)
        {
            int next = GUILayout.Toolbar(Mathf.Clamp(definitionIndex, 0, labels.Length - 1), labels);
            if (next != definitionIndex)
            {
                definitionIndex = next;
                laneIndex = 0;
                bundleIndex = -1;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Structure", GUILayout.Width(90f)))
                AddDefinition(definitions);
        }
        EditorGUILayout.Space(4f);
    }

    bool DrawDefinitionSettings(
        SerializedProperty definitions, SerializedProperty definition)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Structure Settings", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUI.enabled = definitions.arraySize > 1;
                if (GUILayout.Button("Delete Structure", GUILayout.Width(110f)) &&
                    EditorUtility.DisplayDialog(
                        "Delete Structure",
                        "Delete this prop structure definition?",
                        "Delete", "Cancel"))
                {
                    definitions.DeleteArrayElementAtIndex(definitionIndex);
                    definitionIndex = Mathf.Max(0, definitionIndex - 1);
                    laneIndex = 0;
                    bundleIndex = -1;
                    GUI.enabled = true;
                    return false;
                }
                GUI.enabled = true;
            }

            EditorGUILayout.PropertyField(definition.FindPropertyRelative("structureId"));
            EditorGUILayout.PropertyField(definition.FindPropertyRelative("generationMode"));
            EditorGUILayout.PropertyField(definition.FindPropertyRelative("spawnChance"));
            EditorGUILayout.PropertyField(definition.FindPropertyRelative("occupiesCell"));
            EditorGUILayout.PropertyField(definition.FindPropertyRelative("useSocketRotation"));
            EditorGUILayout.PropertyField(definition.FindPropertyRelative("rotationOffset"));
        }
        return true;
    }

    void DrawLaneTabs(SerializedProperty lanes)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Lane Variants", EditorStyles.boldLabel);

        if (lanes.arraySize == 0)
        {
            EditorGUILayout.HelpBox("This structure has no lane variants.", MessageType.Info);
            if (GUILayout.Button("Add Lane"))
                AddLane(lanes);
            return;
        }

        laneIndex = Mathf.Clamp(laneIndex, 0, lanes.arraySize - 1);
        var labels = new string[lanes.arraySize];
        for (int i = 0; i < lanes.arraySize; i++)
        {
            string id = lanes.GetArrayElementAtIndex(i)
                .FindPropertyRelative("laneId").stringValue;
            labels[i] = string.IsNullOrWhiteSpace(id) ? $"Lane {i + 1}" : id;
        }

        int next = GUILayout.Toolbar(laneIndex, labels);
        if (next != laneIndex)
        {
            laneIndex = next;
            bundleIndex = -1;
        }

        SerializedProperty lane = lanes.GetArrayElementAtIndex(laneIndex);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(
                lane.FindPropertyRelative("laneId"), GUIContent.none);
            if (GUILayout.Button("+ Lane", GUILayout.Width(65f)))
                AddLane(lanes);
            GUI.enabled = lanes.arraySize > 1;
            if (GUILayout.Button("Delete", GUILayout.Width(60f)))
            {
                lanes.DeleteArrayElementAtIndex(laneIndex);
                laneIndex = Mathf.Max(0, laneIndex - 1);
                bundleIndex = -1;
                GUI.enabled = true;
                return;
            }
            GUI.enabled = true;
        }

        DrawBundleTable(lane.FindPropertyRelative("bundles"));
        DrawLegacyPrefabs();
    }

    void DrawBundleTable(SerializedProperty bundles)
    {
        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Bundles", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Bundle", GUILayout.Width(75f)))
            {
                AddBundle(bundles);
                bundleIndex = bundles.arraySize - 1;
            }
        }

        if (bundles.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No bundles are configured for this lane.", MessageType.None);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Role", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            GUILayout.Label("Bundle ID", EditorStyles.miniBoldLabel);
            GUILayout.Label("Items", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
            GUILayout.Space(48f);
        }

        for (int i = 0; i < bundles.arraySize; i++)
        {
            SerializedProperty bundle = bundles.GetArrayElementAtIndex(i);
            SerializedProperty items = bundle.FindPropertyRelative("items");
            Color oldColor = GUI.backgroundColor;
            if (i == bundleIndex)
                GUI.backgroundColor = new Color(0.72f, 0.72f, 0.72f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(
                    bundle.FindPropertyRelative("role"), GUIContent.none,
                    GUILayout.Width(90f));
                EditorGUILayout.PropertyField(
                    bundle.FindPropertyRelative("bundleId"), GUIContent.none);
                GUILayout.Label(items.arraySize.ToString(), GUILayout.Width(42f));
                if (GUILayout.Button(i == bundleIndex ? "Close" : "Edit", GUILayout.Width(48f)))
                    bundleIndex = i == bundleIndex ? -1 : i;
            }
            GUI.backgroundColor = oldColor;
        }

        if (bundleIndex >= bundles.arraySize)
            bundleIndex = bundles.arraySize - 1;
        if (bundleIndex >= 0)
            DrawBundleItems(bundles, bundleIndex);
    }

    void DrawBundleItems(SerializedProperty bundles, int selectedIndex)
    {
        SerializedProperty bundle = bundles.GetArrayElementAtIndex(selectedIndex);
        SerializedProperty items = bundle.FindPropertyRelative("items");

        EditorGUILayout.Space(3f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Bundle Items — {bundle.FindPropertyRelative("bundleId").stringValue}",
                    EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Item", GUILayout.Width(60f)))
                    AddItem(items);
                if (GUILayout.Button("Delete Bundle", GUILayout.Width(95f)))
                {
                    bundles.DeleteArrayElementAtIndex(selectedIndex);
                    bundleIndex = -1;
                    return;
                }
            }

            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(
                            item.FindPropertyRelative("prefab"), GUIContent.none);
                        if (GUILayout.Button("×", GUILayout.Width(24f)))
                        {
                            items.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("localPosition"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("localRotation"));
                }
            }
        }
    }

    void DrawLegacyPrefabs()
    {
        SerializedProperty definitions = serializedObject.FindProperty("definitions");
        if (definitions.arraySize == 0 || definitionIndex >= definitions.arraySize)
            return;
        SerializedProperty prefabs = definitions.GetArrayElementAtIndex(definitionIndex)
            .FindPropertyRelative("prefabs");
        EditorGUILayout.Space(4f);
        showLegacyPrefabs = EditorGUILayout.Foldout(
            showLegacyPrefabs, "Legacy Role Prefab Fallbacks", true);
        if (showLegacyPrefabs)
            EditorGUILayout.PropertyField(prefabs, GUIContent.none, true);
    }

    static void AddDefinition(SerializedProperty definitions)
    {
        int index = definitions.arraySize;
        definitions.InsertArrayElementAtIndex(index);
        SerializedProperty definition = definitions.GetArrayElementAtIndex(index);
        definition.FindPropertyRelative("structureId").stringValue = "NewProp";
        definition.FindPropertyRelative("generationMode").enumValueIndex =
            (int)PropGenerationMode.Single;
        definition.FindPropertyRelative("spawnChance").floatValue = 1f;
        definition.FindPropertyRelative("occupiesCell").boolValue = true;
        definition.FindPropertyRelative("useSocketRotation").boolValue = true;
        definition.FindPropertyRelative("rotationOffset").vector3Value = Vector3.zero;
        definition.FindPropertyRelative("laneVariants").ClearArray();
        definition.FindPropertyRelative("prefabs").ClearArray();
    }

    static void AddLane(SerializedProperty lanes)
    {
        int index = lanes.arraySize;
        lanes.InsertArrayElementAtIndex(index);
        SerializedProperty lane = lanes.GetArrayElementAtIndex(index);
        lane.FindPropertyRelative("laneId").stringValue = "NewLane";
        lane.FindPropertyRelative("bundles").ClearArray();
    }

    static void AddBundle(SerializedProperty bundles)
    {
        int index = bundles.arraySize;
        bundles.InsertArrayElementAtIndex(index);
        SerializedProperty bundle = bundles.GetArrayElementAtIndex(index);
        bundle.FindPropertyRelative("role").enumValueIndex = (int)PropSocketRole.Single;
        bundle.FindPropertyRelative("bundleId").stringValue = "Default";
        bundle.FindPropertyRelative("items").ClearArray();
    }

    static void AddItem(SerializedProperty items)
    {
        int index = items.arraySize;
        items.InsertArrayElementAtIndex(index);
        SerializedProperty item = items.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("prefab").objectReferenceValue = null;
        item.FindPropertyRelative("localPosition").vector3Value = Vector3.zero;
        item.FindPropertyRelative("localRotation").vector3Value = Vector3.zero;
    }
}
