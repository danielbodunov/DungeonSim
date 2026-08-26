using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonGroundSurfaceFamily))]
public sealed class DungeonGroundSurfaceFamilyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        DrawRangeWarnings(serializedObject.FindProperty("bands"));
        serializedObject.ApplyModifiedProperties();

        DungeonGroundSurfaceFamily family = (DungeonGroundSurfaceFamily)target;
        if (family.GeneratedLookupHash != DungeonSurfaceLookupGenerator.ComputeGroundFamilyHash(family))
            EditorGUILayout.HelpBox("Generated ground lookup data is stale. Rebuild before validation or committing the asset.", MessageType.Warning);

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild Surface Family Lookup"))
            DungeonSurfaceLookupGenerator.Rebuild();
    }

    static void DrawRangeWarnings(SerializedProperty bands)
    {
        if (bands == null || bands.arraySize == 0)
        {
            EditorGUILayout.HelpBox("At least one ground layer band is required.", MessageType.Error);
            return;
        }

        int expectedDepth = 0;
        int fallbackCount = 0;
        for (int i = 0; i < bands.arraySize; i++)
        {
            SerializedProperty band = bands.GetArrayElementAtIndex(i);
            int min = band.FindPropertyRelative("minDepth").intValue;
            int max = band.FindPropertyRelative("maxDepth").intValue;
            bool unbounded = band.FindPropertyRelative("unbounded").boolValue;
            int variants = band.FindPropertyRelative("variants").arraySize;
            string label = band.FindPropertyRelative("displayName").stringValue;

            if (min != expectedDepth)
                EditorGUILayout.HelpBox($"{label}: expected Min Depth {expectedDepth}; ranges must be contiguous and non-overlapping.", MessageType.Error);
            if (!unbounded && max < min)
                EditorGUILayout.HelpBox($"{label}: Max Depth cannot be below Min Depth.", MessageType.Error);
            if (variants == 0)
                EditorGUILayout.HelpBox($"{label}: assign at least one sliced Sprite.", MessageType.Error);
            else
            {
                bool hasPositiveWeight = false;
                for (int variantIndex = 0; variantIndex < variants; variantIndex++)
                {
                    SerializedProperty variant = band.FindPropertyRelative("variants").GetArrayElementAtIndex(variantIndex);
                    float weight = variant.FindPropertyRelative("weight").floatValue;
                    if (weight < 0f)
                        EditorGUILayout.HelpBox($"{label}: variant {variantIndex + 1} has a negative weight.", MessageType.Error);
                    hasPositiveWeight |= weight > 0f;
                }
                if (!hasPositiveWeight)
                    EditorGUILayout.HelpBox($"{label}: all variant weights are zero; the first valid Sprite will be used as a safe fallback.", MessageType.Warning);
            }
            if (unbounded)
            {
                fallbackCount++;
                if (i != bands.arraySize - 1)
                    EditorGUILayout.HelpBox($"{label}: the unbounded fallback must be last.", MessageType.Error);
            }
            else
                expectedDepth = max + 1;
        }

        if (fallbackCount != 1)
            EditorGUILayout.HelpBox("Exactly one unbounded fallback band is required.", MessageType.Error);
    }
}
