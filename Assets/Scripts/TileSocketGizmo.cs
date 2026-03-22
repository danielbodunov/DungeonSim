using UnityEngine;

public class TileSocketGizmo : MonoBehaviour
{
    public int resolution = 16;

    void OnDrawGizmos()
    {
        var mesh = GetComponentInChildren<MeshFilter>()?.sharedMesh;
        if (!mesh) return;

        var bounds = mesh.bounds;

        Gizmos.color = Color.red;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 pos = new(
                Mathf.Lerp(bounds.min.x, bounds.max.x, t),
                bounds.max.y,
                0
            );

            Gizmos.DrawSphere(transform.TransformPoint(pos), 0.05f);
        }
    }
}
