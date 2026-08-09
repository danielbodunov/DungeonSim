using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A lightweight source consumed by DungeonLightingManager. Put this on a
/// torch, lamp, entrance, or a child of a moving NPC.
/// </summary>
[DisallowMultipleComponent]
public class DungeonLightSource : MonoBehaviour
{
    static readonly HashSet<DungeonLightSource> activeSources = new();

    [SerializeField, ColorUsage(false, true)] Color color = new(1f, 0.55f, 0.2f, 1f);
    [SerializeField, Min(0f)] float intensity = 1f;
    [SerializeField, Min(0.25f)] float radiusInCells = 6f;
    [SerializeField, Tooltip("Enable for NPC-carried or otherwise moving lights. Dynamic lights refresh at the manager's update interval.")]
    bool dynamicSource;

    public static event Action SourcesChanged;

    public Color LightColor => color;
    public float Intensity => intensity;
    public float RadiusInCells => radiusInCells;
    public bool IsDynamic => dynamicSource;

    public void Configure(Color lightColor, float lightIntensity, float radius, bool isDynamic)
    {
        color = lightColor;
        intensity = Mathf.Max(0f, lightIntensity);
        radiusInCells = Mathf.Max(0.25f, radius);
        dynamicSource = isDynamic;
        SourcesChanged?.Invoke();
    }

    public void RequestRefresh()
    {
        SourcesChanged?.Invoke();
    }

    public static void GetActiveSources(List<DungeonLightSource> results)
    {
        results.Clear();
        activeSources.RemoveWhere(source => source == null);
        foreach (DungeonLightSource source in activeSources)
            if (source.isActiveAndEnabled && source.intensity > 0f)
                results.Add(source);
    }

    void OnEnable()
    {
        activeSources.Add(this);
        SourcesChanged?.Invoke();
    }

    void OnDisable()
    {
        activeSources.Remove(this);
        SourcesChanged?.Invoke();
    }

    void OnValidate()
    {
        intensity = Mathf.Max(0f, intensity);
        radiusInCells = Mathf.Max(0.25f, radiusInCells);
        if (isActiveAndEnabled)
            SourcesChanged?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        Color gizmoColor = color;
        gizmoColor.a = 0.9f;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radiusInCells);
        Gizmos.DrawSphere(transform.position, 0.08f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry()
    {
        activeSources.Clear();
        SourcesChanged = null;
    }
}
