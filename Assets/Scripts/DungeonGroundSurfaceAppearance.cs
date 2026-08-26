using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class DungeonGroundSurfaceAppearance : MonoBehaviour
{
    static readonly int UseGroundLayersId = Shader.PropertyToID("_UseGroundLayers");
    static readonly int GroundLookupStartRowId = Shader.PropertyToID("_GroundLookupStartRow");
    static readonly int GroundTopYId = Shader.PropertyToID("_GroundTopY");
    static readonly int GroundCellScaleId = Shader.PropertyToID("_GroundCellScale");
    static readonly int GroundCellWorldSizeId = Shader.PropertyToID("_GroundCellWorldSize");
    static readonly int VisualSeedId = Shader.PropertyToID("_VisualSeed");

    [SerializeField] DungeonGroundSurfaceFamily family;
    [Tooltip("Optional per-region reference. Its world Y overrides Ground Top Y.")]
    [SerializeField] Transform groundTopReference;
    [SerializeField] float groundTopY;
    [Min(1), SerializeField] int logicalCellsPerTile = 3;
    [FormerlySerializedAs("groundLayerHeight")]
    [Min(0.0001f), SerializeField] float dungeonTileWorldSize = 1f;
    [SerializeField] int visualSeed;

    MaterialPropertyBlock propertyBlock;

    void Awake() => Apply();
    void OnEnable() => Apply();

#if UNITY_EDITOR
    void OnValidate()
    {
        logicalCellsPerTile = Mathf.Max(1, logicalCellsPerTile);
        dungeonTileWorldSize = Mathf.Max(0.0001f, dungeonTileWorldSize);
        if (isActiveAndEnabled)
            Apply();
    }
#endif

    public void Configure(float topWorldY, float tileWorldSize, int cellsPerTile, int seed)
    {
        groundTopReference = null;
        groundTopY = topWorldY;
        dungeonTileWorldSize = Mathf.Max(0.0001f, tileWorldSize);
        logicalCellsPerTile = Mathf.Max(1, cellsPerTile);
        visualSeed = seed;
        Apply();
    }

    public void Apply()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        float resolvedTopY = groundTopReference != null ? groundTopReference.position.y : groundTopY;
        float cellScale = logicalCellsPerTile / Mathf.Max(0.0001f, dungeonTileWorldSize);
        float cellWorldSize = 1f / cellScale;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(UseGroundLayersId, family != null ? 1f : 0f);
            propertyBlock.SetFloat(GroundLookupStartRowId, family != null ? family.LookupStartRow : 0f);
            propertyBlock.SetFloat(GroundTopYId, resolvedTopY);
            propertyBlock.SetFloat(GroundCellScaleId, cellScale);
            propertyBlock.SetFloat(GroundCellWorldSizeId, cellWorldSize);
            propertyBlock.SetFloat(VisualSeedId, visualSeed);
            target.SetPropertyBlock(propertyBlock);
        }
    }
}
