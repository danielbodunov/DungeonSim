using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DungeonGroundLayerBand
{
    [SerializeField] string displayName = "Ground Layer";
    [Min(0), SerializeField] int minDepth;
    [Min(0), SerializeField] int maxDepth;
    [SerializeField] bool unbounded;
    [SerializeField] DungeonWeightedSpriteVariant[] variants = Array.Empty<DungeonWeightedSpriteVariant>();

    public string DisplayName => displayName;
    public int MinDepth => minDepth;
    public int MaxDepth => maxDepth;
    public bool Unbounded => unbounded;
    public IReadOnlyList<DungeonWeightedSpriteVariant> Variants => variants;
}

[CreateAssetMenu(fileName = "DungeonGroundSurfaceFamily", menuName = "Dungeon/Ground Surface Family")]
public sealed class DungeonGroundSurfaceFamily : ScriptableObject
{
    [SerializeField] string stableId = "DefaultGround";
    [SerializeField] DungeonGroundLayerBand[] bands = Array.Empty<DungeonGroundLayerBand>();
    [SerializeField, HideInInspector] int lookupStartRow;
    [SerializeField, HideInInspector] string generatedLookupHash;

    public string StableId => stableId;
    public IReadOnlyList<DungeonGroundLayerBand> Bands => bands;
    public int LookupStartRow => lookupStartRow;
    public string GeneratedLookupHash => generatedLookupHash;

#if UNITY_EDITOR
    public void SetLookupStartRow(int value) => lookupStartRow = value;
    public void SetGeneratedLookupHash(string value) => generatedLookupHash = value;
#endif
}
