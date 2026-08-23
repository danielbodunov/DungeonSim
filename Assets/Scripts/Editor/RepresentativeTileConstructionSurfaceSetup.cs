using UnityEditor;
using UnityEngine;

public static class RepresentativeTileConstructionSurfaceSetup
{
    const string PrefabPath =
        "Assets/Resources/Dungeon/Narrow_Straight_I.prefab";

    readonly struct SurfaceDefinition
    {
        public readonly string Id;
        public readonly TileConstructionSurfaceKind Kind;
        public readonly Vector3 Position;
        public readonly Vector3 EulerAngles;
        public readonly TileConstructionModuleImpact Impact;
        public readonly TrapAttachmentSurfaceMask TrapMask;

        public SurfaceDefinition(
            string id,
            TileConstructionSurfaceKind kind,
            Vector3 position,
            Vector3 eulerAngles,
            TileConstructionModuleImpact impact,
            TrapAttachmentSurfaceMask trapMask)
        {
            Id = id;
            Kind = kind;
            Position = position;
            EulerAngles = eulerAngles;
            Impact = impact;
            TrapMask = trapMask;
        }
    }

    static readonly SurfaceDefinition[] Definitions =
    {
        new("Floor", TileConstructionSurfaceKind.Floor,
            new Vector3(0f, -0.5f, 0f), Vector3.zero,
            TileConstructionModuleImpact.VisualOnly,
            TrapAttachmentSurfaceMask.Floor),
        new("Ceiling", TileConstructionSurfaceKind.Ceiling,
            new Vector3(0f, 0.5f, 0f), Vector3.zero,
            TileConstructionModuleImpact.VisualOnly,
            TrapAttachmentSurfaceMask.Ceiling),
        new("NorthWall", TileConstructionSurfaceKind.NorthWall,
            new Vector3(0f, 0f, 0.5f), Vector3.zero,
            TileConstructionModuleImpact.RequiresTopologyResolution,
            TrapAttachmentSurfaceMask.None),
        new("SouthWall", TileConstructionSurfaceKind.SouthWall,
            new Vector3(0f, 0f, -0.5f), new Vector3(0f, 180f, 0f),
            TileConstructionModuleImpact.RequiresTopologyResolution,
            TrapAttachmentSurfaceMask.None),
        new("EastWall", TileConstructionSurfaceKind.EastWall,
            new Vector3(0.5f, 0f, 0f), new Vector3(0f, 90f, 0f),
            TileConstructionModuleImpact.RequiresTopologyResolution,
            TrapAttachmentSurfaceMask.RightWall),
        new("WestWall", TileConstructionSurfaceKind.WestWall,
            new Vector3(-0.5f, 0f, 0f), new Vector3(0f, -90f, 0f),
            TileConstructionModuleImpact.RequiresTopologyResolution,
            TrapAttachmentSurfaceMask.LeftWall),
        new("TrapServiceRegion", TileConstructionSurfaceKind.TrapServiceRegion,
            Vector3.zero, Vector3.zero,
            TileConstructionModuleImpact.VisualOnly,
            TrapAttachmentSurfaceMask.All)
    };

    [MenuItem("Tools/Dungeon/Configure Narrow Straight Construction Surfaces")]
    public static void Configure()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            TileConstructionSurfaces contract =
                root.GetComponent<TileConstructionSurfaces>() ??
                root.AddComponent<TileConstructionSurfaces>();
            var anchors = new Transform[Definitions.Length];
            for (int i = 0; i < Definitions.Length; i++)
            {
                SurfaceDefinition definition = Definitions[i];
                string objectName = $"Construction Surface - {definition.Id}";
                Transform anchor = root.transform.Find(objectName);
                if (anchor == null)
                {
                    var anchorObject = new GameObject(objectName);
                    anchor = anchorObject.transform;
                    anchor.SetParent(root.transform, false);
                }
                anchor.localPosition = definition.Position;
                anchor.localEulerAngles = definition.EulerAngles;
                anchor.localScale = Vector3.one;
                anchors[i] = anchor;
            }

            var serialized = new SerializedObject(contract);
            SerializedProperty surfaces = serialized.FindProperty("surfaces");
            surfaces.arraySize = Definitions.Length;
            for (int i = 0; i < Definitions.Length; i++)
            {
                SurfaceDefinition definition = Definitions[i];
                SerializedProperty surface = surfaces.GetArrayElementAtIndex(i);
                surface.FindPropertyRelative("id").stringValue = definition.Id;
                surface.FindPropertyRelative("kind").enumValueIndex =
                    (int)definition.Kind;
                surface.FindPropertyRelative("anchor").objectReferenceValue =
                    anchors[i];
                surface.FindPropertyRelative("moduleImpact").enumValueIndex =
                    (int)definition.Impact;
                surface.FindPropertyRelative("trapAttachmentSurfaces").intValue =
                    (int)definition.TrapMask;
                surface.FindPropertyRelative("variants").arraySize = 0;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Configured representative construction surfaces on " +
                $"'{PrefabPath}'.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
