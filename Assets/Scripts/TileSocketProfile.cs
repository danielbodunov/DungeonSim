using UnityEngine;
using System.Collections.Generic;

public enum TileSide { North, South, East, West }
public enum TileCategory { Unspecified, Ground, Narrow, Wide, Transition, Starter }

[CreateAssetMenu(menuName = "Tiles/Tile Socket Profile")]
public class TileSocketProfile : ScriptableObject
{
    public GameObject sourcePrefab;

    public int resolution = 16;
    public int rotation; //0, 1, 2, 3 (90 degree steps)

    public string baseTileName;
    public TileCategory category;
    public string northHash;
    public string southHash;
    public string westHash;
    public string eastHash;

    public List<string> northMatches = new();
    public List<string> southMatches = new();
    public List<string> eastMatches = new();
    public List<string> westMatches = new();

    public string GetHash(TileSide side)
    {
        return side switch
        {
            TileSide.North => northHash,
            TileSide.South => southHash,
            TileSide.West  => westHash,
            TileSide.East  => eastHash,
            _ => ""
        };
    }
}
