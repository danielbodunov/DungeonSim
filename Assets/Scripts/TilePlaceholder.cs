using UnityEngine;

public class TilePlaceholder : MonoBehaviour
{
    public int x, y;
    public TileGridGenerator generator;

    void OnMouseDown()
    {
        generator.ClickCell(x, y);
    }
}