using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Tiles/Tile Adjacency Database")]
public class TileAdjacencyDatabase : ScriptableObject
{
    public List<TileSocketProfile> tiles = new();
}
