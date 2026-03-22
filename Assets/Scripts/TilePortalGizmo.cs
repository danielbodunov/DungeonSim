using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

[ExecuteAlways]
public class TilePortalGizmo : MonoBehaviour
{
    public TileSide side;
    public string mask;
    public int resolution = 16;
    public TileSocketProfile profile;
    public bool allSides = true;
    public bool visualizeRays;

    private Dictionary<int, Vector3[]> samplePositions;

    private Bounds b;

    void OnDrawGizmos()
    {
        if (samplePositions == null)
            samplePositions = new Dictionary<int, Vector3[]>();

        if(visualizeRays)
            SampleRays(b, resolution);

        if (string.IsNullOrEmpty(mask))
            return;

        b = CalculateBounds(gameObject);


        if(allSides)
        {
            DrawSide(TileSide.North);
            DrawSide(TileSide.South);
            DrawSide(TileSide.East);
            DrawSide(TileSide.West);
        }
        else
        {
            DrawSide(side);
        }

        if(visualizeRays)
            SampleRays(b, resolution);

    }
    void DrawRay(Vector3 origin, Vector3 dir)
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, dir);
    }
    void DrawSide(TileSide s)
    {

        mask = s switch
        {
            TileSide.North => profile.northHash,
            TileSide.South => profile.southHash,
            TileSide.East  => profile.eastHash,
            TileSide.West  => profile.westHash,
            _ => null
        };

        Debug.Log("Mask size: " + mask.Length); 
        for (int i = 0; i < mask.Length; i++)
        {
            float t = (float)i / (resolution - 1);
            Vector3 pos = new Vector3();

            if(!visualizeRays)
            {
                pos = s switch
                {
                    TileSide.North => new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), b.max.y, b.center.z),
                    TileSide.South => new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), b.min.y, b.center.z),
                    TileSide.East  => new Vector3(b.min.x, Mathf.Lerp(b.min.y, b.max.y, t), b.center.z),
                    TileSide.West  => new Vector3(b.max.x, Mathf.Lerp(b.min.y, b.max.y, t), b.center.z),
                    _ => Vector3.zero
                };
            } else {
                pos = samplePositions[(int)s][i];
                Vector3 dir = GetInwardDirection(s);
                DrawRay(pos, dir);
            }
            Gizmos.color = mask[i] == '1' ? Color.green : Color.red;
            Gizmos.DrawSphere(pos, 0.05f);  
        }
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
    
    private void SampleRays(Bounds b, int resolution)
    {
        if (!samplePositions.ContainsKey(0))
        {
            for (int i = 0; i < 4; i++)
                samplePositions[i] = new Vector3[resolution];
        }

        //Iterate over each side
        for (int i = 0; i < 4; i++)
        {
            TileSide side = (TileSide)i;

            //SampleSide points along the edge
            for (int j = 0; j < resolution; j++)
            {
                float t = (float)j / (resolution - 1);
                Vector3 samplePoint = Vector3.zero;

                for (int v = 0; v < 3; v++)
                {
                    float vt = (float)v / (3 - 1);
                    float depth = Mathf.Lerp(b.min.z, b.max.z, vt);

                    Vector3 origin = GetEdgePoint(b, side, t, depth);
                    Vector3 direction = GetInwardDirection(side);
                    samplePoint = origin;

                    origin += direction * -0.02f;
                    
                    if (!Physics.Raycast(origin, direction, 3))
                    {
                        samplePoint = origin;
                        break;
                    }
                }
                samplePositions[i][j] = samplePoint;
            }
        }
    }
        static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;

        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds;
    }

}