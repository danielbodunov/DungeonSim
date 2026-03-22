using UnityEngine;
using System.Text;
using System.Collections.Generic;

public static class PortalEdgeAnalyzer
{

    public static string GeneratePortalMask(
        GameObject prefab,
        TileSide side,
        int resolution,
        int depthSamples,
        float checkDepth
        )
    {
        prefab.transform.position = Vector3.zero;
        Debug.Log($"Instance position and rotation for {prefab.name}:{prefab.transform.position},{prefab.transform.rotation.eulerAngles}");

        var bounds = CalculateBounds(prefab);
        StringBuilder mask = new();

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);

            bool blocked = true;
            Vector3 lastSamplePoint = Vector3.zero;

            for (int v = 0; v < 1; v++)
            {
                float vt = (float)v / (depthSamples - 1);
                float depth = Mathf.Lerp(bounds.min.z, bounds.max.z, vt);

                Vector3 origin = GetEdgePoint(bounds, side, t, depth);
                Vector3 direction = GetInwardDirection(side);
                lastSamplePoint = origin;

                origin += direction * -.01f;
                
                if (!Physics.Raycast(origin, direction, .02f))
                {
                    blocked = false;
                    lastSamplePoint = origin;
                    break;
                }
            }
            Debug.Log($"Sample point for {prefab.name} on side {side} at t={t}: {lastSamplePoint}, blocked: {blocked}");


            mask.Append(blocked ? "0" : "1");
        }
        return mask.ToString();
    }

    static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;

        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
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
                                0.5f
                                ),
            // -Y
            TileSide.South => new Vector3(
                                Mathf.Lerp(b.min.x + offset, b.max.x - offset, t),
                                b.min.y,
                                0.5f
                                ),
            // -X
            TileSide.West => new Vector3(
                                b.max.x,
                                Mathf.Lerp(b.min.y + offset, b.max.y - offset, t),
                                0.5f
                                ),
            // +X
            TileSide.East => new Vector3(
                                b.min.x,
                                Mathf.Lerp(b.min.y + offset, b.max.y - offset, t),
                                0.5f
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
            TileSide.East  => Vector3.right,
            TileSide.West  => Vector3.left,
            _ => Vector3.zero
        };
    }
}