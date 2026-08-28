using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonSurfaceFamily))]
public sealed class DungeonSurfaceFamilyEditor : Editor
{
    static readonly string[] VariantProperties =
    {
        "backWallVariants", "floorVariants", "ceilingVariants", "sideWallVariants"
    };

    static readonly string[] RoleNames = { "Back Wall", "Floor", "Ceiling", "Side Wall" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        for (int roleIndex = 0; roleIndex < VariantProperties.Length; roleIndex++)
            DrawWeightWarnings(serializedObject.FindProperty(VariantProperties[roleIndex]), RoleNames[roleIndex]);
        serializedObject.ApplyModifiedProperties();

        DungeonSurfaceFamily family = (DungeonSurfaceFamily)target;
        if (family.GeneratedLookupHash != DungeonSurfaceLookupGenerator.ComputeSurfaceFamilyHash(family))
            EditorGUILayout.HelpBox("Generated surface lookup data is stale. Rebuild before validation or committing the asset.", MessageType.Warning);

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild Surface Family Lookup"))
            DungeonSurfaceLookupGenerator.Rebuild();
    }

    static void DrawWeightWarnings(SerializedProperty variants, string roleName)
    {
        if (variants == null || variants.arraySize == 0)
        {
            EditorGUILayout.HelpBox($"{roleName}: assign at least one sliced Sprite.", MessageType.Error);
            return;
        }

        bool hasPositiveWeight = false;
        for (int i = 0; i < variants.arraySize; i++)
        {
            float weight = variants.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue;
            if (weight < 0f)
                EditorGUILayout.HelpBox($"{roleName}: variant {i + 1} has a negative weight.", MessageType.Error);
            hasPositiveWeight |= weight > 0f;
        }
        if (!hasPositiveWeight)
            EditorGUILayout.HelpBox($"{roleName}: all weights are zero; the first valid Sprite will be used as a safe fallback.", MessageType.Warning);
    }
}
