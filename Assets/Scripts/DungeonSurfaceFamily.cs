using System;
using System.Collections.Generic;
using UnityEngine;

public enum DungeonSurfaceRole
{
    BackWall = 0,
    Floor = 1,
    Ceiling = 2,
    SideWall = 3
}

[CreateAssetMenu(fileName = "DungeonSurfaceFamily", menuName = "Dungeon/Surface Family")]
public sealed class DungeonSurfaceFamily : ScriptableObject
{
    [SerializeField] string stableId = "DungeonStone";
    [SerializeField, HideInInspector] int lookupIndex;
    [SerializeField, HideInInspector] string generatedLookupHash;
    [SerializeField] DungeonWeightedSpriteVariant[] backWallVariants = Array.Empty<DungeonWeightedSpriteVariant>();
    [SerializeField] DungeonWeightedSpriteVariant[] floorVariants = Array.Empty<DungeonWeightedSpriteVariant>();
    [SerializeField] DungeonWeightedSpriteVariant[] ceilingVariants = Array.Empty<DungeonWeightedSpriteVariant>();
    [SerializeField] DungeonWeightedSpriteVariant[] sideWallVariants = Array.Empty<DungeonWeightedSpriteVariant>();

    public string StableId => stableId;
    public int LookupIndex => lookupIndex;
    public string GeneratedLookupHash => generatedLookupHash;

    public IReadOnlyList<DungeonWeightedSpriteVariant> GetVariants(DungeonSurfaceRole role) => role switch
    {
        DungeonSurfaceRole.BackWall => backWallVariants,
        DungeonSurfaceRole.Floor => floorVariants,
        DungeonSurfaceRole.Ceiling => ceilingVariants,
        DungeonSurfaceRole.SideWall => sideWallVariants,
        _ => Array.Empty<DungeonWeightedSpriteVariant>()
    };

#if UNITY_EDITOR
    public void SetLookupIndex(int value) => lookupIndex = value;
    public void SetGeneratedLookupHash(string value) => generatedLookupHash = value;
#endif
}
