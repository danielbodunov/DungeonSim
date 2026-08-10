using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PropSocketAuthoring : MonoBehaviour
{
    [Tooltip("Sockets with the same structure ID can be matched together.")]
    public string structureId = "Ladder";

    [Tooltip("Sockets in one generated structure must use the same lane ID.")]
    public string laneId = "Default";

    [Tooltip("Additional incoming lane IDs this socket may accept. Most useful for Continue sockets that can serve either Left or Right without duplicating the socket.")]
    public List<string> compatibleLaneIds = new();

    [Tooltip("Selects the role bundle used in this cell, such as Default or Platform. Unlike Lane ID, this may change along a structure run.")]
    public string bundleId = "Default";

    [Min(0f), Tooltip("Relative chance of choosing this Start socket. Set to zero to disable automatic starts. Ignored for other roles.")]
    public float selectionWeight = 1f;

    public PropSocketRole role = PropSocketRole.Single;

    [Tooltip("Connection direction in the unrotated tile: Start points away from the start, End points toward the incoming structure, and Continue identifies its axis.")]
    public PropSocketDirection direction = PropSocketDirection.South;

    [Tooltip("Marks a Continue socket as platform-capable and able to expose an intermediate NPC ladder entrance/exit when its platform policy permits. Start and End are always traversal endpoints.")]
    public bool allowsTraversalExit;

    [Tooltip("Controls whether a traversal platform is included during automatic ladder generation. Manual Only keeps the platform bundle available for a future placement tool.")]
    public PropSocketPlatformPolicy platformPolicy;

    void OnDrawGizmosSelected()
    {
        if (!NPCTraversalDebug.SocketVisualsEnabled)
            return;

        Gizmos.color = role switch
        {
            PropSocketRole.Start => Color.green,
            PropSocketRole.Continue => Color.yellow,
            PropSocketRole.End => Color.red,
            _ => Color.cyan
        };

        Vector3 origin = transform.position;
        Vector3 directionVector = GetWorldDirection();
        Vector3 endpoint = origin + directionVector * 0.4f;

        Gizmos.DrawWireSphere(origin, 0.08f);
        Gizmos.DrawLine(origin, endpoint);

#if UNITY_EDITOR
        float handleSize = HandleUtility.GetHandleSize(endpoint) * 0.08f;
        Handles.color = Gizmos.color;
        Handles.ConeHandleCap(
            0,
            endpoint,
            Quaternion.LookRotation(Vector3.forward, directionVector),
            handleSize,
            EventType.Repaint);

        string weightLabel = role == PropSocketRole.Start
            ? $" w:{selectionWeight:0.##}"
            : string.Empty;
        string exitLabel = role == PropSocketRole.Continue && allowsTraversalExit
            ? " Exit"
            : string.Empty;
        string platformLabel = role == PropSocketRole.Continue &&
            platformPolicy == PropSocketPlatformPolicy.ManualOnly
                ? " Manual Platform"
                : string.Empty;
        string compatibleLabel = compatibleLaneIds != null && compatibleLaneIds.Count > 0
            ? $" Accepts: {string.Join(",", compatibleLaneIds)}"
            : string.Empty;
        Handles.Label(
            origin + Vector3.up * 0.12f,
            $"{structureId} {role} {direction}{exitLabel}{platformLabel}\n" +
            $"Lane: {laneId}{compatibleLabel} Bundle: {bundleId}{weightLabel}");
#endif
    }

    Vector3 GetWorldDirection()
    {
        Transform tileRoot = FindTileRoot();
        return direction switch
        {
            PropSocketDirection.North => tileRoot.up,
            PropSocketDirection.East => tileRoot.right,
            PropSocketDirection.South => -tileRoot.up,
            PropSocketDirection.West => -tileRoot.right,
            _ => tileRoot.up
        };
    }

    Transform FindTileRoot()
    {
        Transform tileRoot = transform;
        Transform current = transform;
        while (current.parent != null)
        {
            // Runtime tile instances are direct children of the grid generator.
            // Stop before crossing from the tile prefab into that scene object.
            if (current.parent.GetComponent<TileGridGenerator>() != null)
                break;

            current = current.parent;
            tileRoot = current;
        }

        return tileRoot;
    }
}
