using UnityEngine;
using System.Text;
using System.Collections.Generic;

public static class PortalEdgeAnalyzer
{
    const float CellSize = 1f;
    const float EdgeStartOffset = 0.01f;

    public static string GeneratePortalMask(
        GameObject prefab,
        TileSide side,
        int resolution,
        int depthSamples,
        float checkDepth
        )
    {
        prefab.transform.position = Vector3.zero;

        // Editor-time instantiation and rotation do not necessarily update the
        // physics scene before the raycasts below. Without this, every rotated
        // profile can sample the collider at its original orientation.
        Physics.SyncTransforms();

        Debug.Log($"Instance position and rotation for {prefab.name}:{prefab.transform.position},{prefab.transform.rotation.eulerAngles}");

        // Tile adjacency is defined on the logical 1x1 cell, not on visual
        // renderer bounds. Cosmetic mesh offsets or prop-authoring gizmos must
        // not move the edge samples away from the tile's colliders.
        var bounds = new Bounds(
            prefab.transform.position,
            new Vector3(CellSize, CellSize, CellSize));
        StringBuilder mask = new();

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);

            bool blocked = true;
            Vector3 lastSamplePoint = Vector3.zero;

            // Edge topology is defined in the tile's XY plane. Sampling at
            // the logical center makes the result independent of whether the
            // source geometry faces +Z or -Z.
            Vector3 origin = GetEdgePoint(bounds, side, t, bounds.center.z);
            Vector3 direction = GetInwardDirection(side);
            lastSamplePoint = origin;

            origin -= direction * EdgeStartOffset;

            if (!Physics.Raycast(origin, direction, checkDepth))
            {
                blocked = false;
                lastSamplePoint = origin;
            }
            Debug.Log($"Sample point for {prefab.name} on side {side} at t={t}: {lastSamplePoint}, blocked: {blocked}");


            mask.Append(blocked ? "0" : "1");
        }
        return mask.ToString();
    }

    static Vector3 GetEdgePoint(Bounds b, TileSide side, float t, float depth)
    {
        float offset = 0.02f;
        return side switch
        {
            // +Y
            TileSide.North => new Vector3(
                                Mathf.Lerp(b.min.x + offset, b.max.x - offset, t),
                                b.max.y,
                                depth
                                ),
            // -Y
            TileSide.South => new Vector3(
                                Mathf.Lerp(b.min.x + offset, b.max.x - offset, t),
                                b.min.y,
                                depth
                                ),
            // -X
            TileSide.West => new Vector3(
                                b.min.x,
                                Mathf.Lerp(b.min.y + offset, b.max.y - offset, t),
                                depth
                                ),
            // +X
            TileSide.East => new Vector3(
                                b.max.x,
                                Mathf.Lerp(b.min.y + offset, b.max.y - offset, t),
                                depth
                                ),
            _ => Vector3.zero,
        };
    }

    static Vector3 GetInwardDirection(TileSide side)
    {
        return side switch
        {
            TileSide.North => Vector3.down,
            TileSide.South => Vector3.up,
            TileSide.East  => Vector3.left,
            TileSide.West  => Vector3.right,
            _ => Vector3.zero
        };
    }
}
