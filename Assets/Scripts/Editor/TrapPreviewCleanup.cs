#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TrapPreviewCleanup
{
    static readonly string[] PreviewNames =
    {
        "Trap Target Corridor Preview",
        "Trap Footprint Preview",
        "Trap Hazard Direction Preview",
        "Trap Construction Presentation Preview"
    };

    static TrapPreviewCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null || !candidate.scene.IsValid() ||
                !IsPreviewName(candidate.name))
                continue;
            Object.DestroyImmediate(candidate);
        }
    }

    static bool IsPreviewName(string objectName)
    {
        for (int i = 0; i < PreviewNames.Length; i++)
            if (objectName == PreviewNames[i])
                return true;
        return false;
    }
}
#endif
