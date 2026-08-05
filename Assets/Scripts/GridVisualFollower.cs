using UnityEngine;

public class GridVisualFollower : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] float gridPlaneZ;
    [SerializeField] float cellSize = 1f;

    void LateUpdate()
    {
        if (targetCamera == null)
            return;

        Ray centerRay = targetCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f));

        Plane gridPlane = new Plane(Vector3.forward,
            new Vector3(0f, 0f, gridPlaneZ));

        if (!gridPlane.Raycast(centerRay, out float distance))
            return;

        Vector3 center = centerRay.GetPoint(distance);

        transform.position = new Vector3(
            Mathf.Floor(center.x / cellSize) * cellSize,
            Mathf.Floor(center.y / cellSize) * cellSize,
            gridPlaneZ);
    }
}