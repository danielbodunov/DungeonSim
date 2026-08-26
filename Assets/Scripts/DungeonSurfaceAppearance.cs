using UnityEngine;

[DisallowMultipleComponent]
public sealed class DungeonSurfaceAppearance : MonoBehaviour
{
    static readonly int PrimaryFamilyId = Shader.PropertyToID("_PrimaryFamily");
    static readonly int SecondaryFamilyId = Shader.PropertyToID("_SecondaryFamily");
    static readonly int AccentFamilyId = Shader.PropertyToID("_AccentFamily");
    static readonly int SpecialFamilyId = Shader.PropertyToID("_SpecialFamily");
    static readonly int VisualSeedId = Shader.PropertyToID("_VisualSeed");

    [SerializeField] DungeonSurfaceFamily primaryFamily;
    [SerializeField] DungeonSurfaceFamily secondaryFamily;
    [SerializeField] DungeonSurfaceFamily accentFamily;
    [SerializeField] DungeonSurfaceFamily specialFamily;
    [SerializeField] int visualSeed;

    MaterialPropertyBlock propertyBlock;

    void Awake() => Apply();
    void OnEnable() => Apply();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (isActiveAndEnabled)
            Apply();
    }
#endif

    public void Apply()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(PrimaryFamilyId, FamilyIndex(primaryFamily));
            propertyBlock.SetFloat(SecondaryFamilyId, FamilyIndex(secondaryFamily, primaryFamily));
            propertyBlock.SetFloat(AccentFamilyId, FamilyIndex(accentFamily, primaryFamily));
            propertyBlock.SetFloat(SpecialFamilyId, FamilyIndex(specialFamily, primaryFamily));
            propertyBlock.SetFloat(VisualSeedId, visualSeed);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    static int FamilyIndex(DungeonSurfaceFamily family, DungeonSurfaceFamily fallback = null)
    {
        DungeonSurfaceFamily resolved = family != null ? family : fallback;
        return resolved != null ? resolved.LookupIndex : 0;
    }
}
