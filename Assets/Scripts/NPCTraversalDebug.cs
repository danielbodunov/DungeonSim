using System.Collections.Generic;
using UnityEngine;

/// <summary>Add beside NPCTraversal to visualize its runtime navigation data.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class NPCTraversalDebug : MonoBehaviour
{
    public static bool SocketVisualsEnabled { get; private set; } = true;

    [SerializeField] NPCTraversal traversal;

    [Header("Tile Debug Visuals")]
    [SerializeField, Tooltip("Master toggle for the navigation graph and walkable-surface sample visuals.")]
    bool showTileDebugVisuals = true;
    [SerializeField] bool showNavigationGraph = true;
    [SerializeField] bool showWalkableSamples = true;
    [SerializeField] bool showRejectedSamples = true;
    [SerializeField, Tooltip("Shows authored prop and ladder socket gizmos on tile prefabs and instances.")]
    bool showSocketVisuals = true;

    [Header("NPC Debug Visuals")]
    [SerializeField] bool showActivePath = true;
    [SerializeField] bool showNextTarget = true;
    [SerializeField, Min(0.005f)] float markerRadius = 0.04f;

    readonly List<NPCDebugConnection> connections = new();

    void OnEnable()
    {
        ApplyGlobalToggles();
    }

    void OnValidate()
    {
        ApplyGlobalToggles();
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    void ApplyGlobalToggles()
    {
        SocketVisualsEnabled = showSocketVisuals;
    }

    void OnDrawGizmos()
    {
        ApplyGlobalToggles();
        if (traversal == null)
            traversal = GetComponent<NPCTraversal>();
        if (traversal == null)
            return;

        if (showTileDebugVisuals)
        {
            if (showNavigationGraph)
                DrawGraph();
            if (showWalkableSamples)
                DrawSamples(traversal.DebugWalkableSamples, new Color(0.1f, 1f, 0.25f, 0.9f));
            if (showRejectedSamples)
                DrawSamples(traversal.DebugRejectedSamples, new Color(1f, 0.1f, 0.1f, 0.9f));
        }
        if (showActivePath)
            DrawActivePath();
        if (showNextTarget)
            DrawNextTarget();
    }

    void DrawGraph()
    {
        traversal.GetDebugConnections(connections);
        foreach (NPCDebugConnection connection in connections)
        {
            Gizmos.color = connection.isLadder
                ? new Color(1f, 0.1f, 1f, 0.9f)
                : new Color(0.1f, 0.8f, 1f, 0.75f);
            Gizmos.DrawLine(connection.from, connection.to);
        }
    }

    void DrawSamples(IReadOnlyList<Vector3> samples, Color color)
    {
        Gizmos.color = color;
        for (int i = 0; i < samples.Count; i++)
            Gizmos.DrawSphere(samples[i], markerRadius);
    }

    void DrawActivePath()
    {
        NPCTraversalAgent agent = traversal.ActiveAgent;
        if (agent == null || agent.ActiveRoute == null)
            return;

        Gizmos.color = Color.yellow;
        Vector3 previous = agent.transform.position;
        for (int i = Mathf.Max(0, agent.NextWaypointIndex); i < agent.ActiveRoute.Count; i++)
        {
            Vector3 waypoint = agent.ActiveRoute[i];
            Gizmos.DrawLine(previous, waypoint);
            Gizmos.DrawWireSphere(waypoint, markerRadius * 1.5f);
            previous = waypoint;
        }
    }

    void DrawNextTarget()
    {
        NPCTraversalAgent agent = traversal.ActiveAgent;
        if (agent == null || !agent.HasNextWaypoint)
            return;

        Gizmos.color = new Color(1f, 0.45f, 0f, 1f);
        Gizmos.DrawWireSphere(agent.NextWaypoint, markerRadius * 3f);
        Gizmos.DrawLine(agent.transform.position, agent.NextWaypoint);
    }
}
