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
    public enum ColorAnimationMode
    {
        Noise,
        Loop
    }

    static readonly HashSet<DungeonLightSource> activeSources = new();

    [Header("Light")]
    [SerializeField, ColorUsage(false, true)] Color color = new(1f, 0.55f, 0.2f, 1f);
    [SerializeField, Min(0f)] float intensity = 1f;
    [SerializeField, Min(0.25f)] float radiusInCells = 6f;
    [SerializeField, Tooltip("Enable for NPC-carried or otherwise moving lights. Dynamic lights refresh at the manager's update interval.")]
    bool dynamicSource;

    [Header("Falloff")]
    [SerializeField, Range(0.1f, 8f)] float falloffPower = 2f;
    [SerializeField, Min(0f)] float innerRadiusInCells;
    [SerializeField, Range(0f, 8f)] float coreBoost;

    [Header("Animation")]
    [SerializeField] bool animateIntensity;
    [SerializeField, Range(0f, 1f)] float flickerAmount = 0.12f;
    [SerializeField, Range(0.01f, 10f)] float flickerSpeed = 2f;
    [SerializeField] bool animateColor;
    [SerializeField, GradientUsage(true)] Gradient colorGradient = new();
    [SerializeField, Range(0.01f, 10f)] float colorAnimationSpeed = 1f;
    [SerializeField, Range(0f, 1f)] float colorAnimationAmount = 0.25f;
    [SerializeField] ColorAnimationMode colorAnimationMode = ColorAnimationMode.Noise;
    [SerializeField] int animationSeed;

    public static event Action SourcesChanged;

    public Color LightColor => GetCurrentColor(Time.unscaledTime);
    public float Intensity => GetCurrentIntensity(Time.unscaledTime);
    public Color BaseColor => color;
    public float BaseIntensity => intensity;
    public float RadiusInCells => radiusInCells;
    public float InnerRadiusInCells => innerRadiusInCells;
    public float FalloffPower => falloffPower;
    public float CoreBoost => coreBoost;
    public bool IsDynamic => dynamicSource || animateIntensity || animateColor;

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

    public Color GetCurrentColor(float unscaledTime)
    {
        if (!animateColor || colorGradient == null)
            return NonNegative(color);

        float sample = colorAnimationMode == ColorAnimationMode.Loop
            ? Mathf.Repeat(
                unscaledTime * colorAnimationSpeed + SeedOffset(37), 1f)
            : Mathf.PerlinNoise(
                SeedOffset(37),
                unscaledTime * colorAnimationSpeed + SeedOffset(71));
        Color animated = colorGradient.Evaluate(sample);
        return NonNegative(Color.LerpUnclamped(
            color, animated, colorAnimationAmount));
    }

    public float GetCurrentIntensity(float unscaledTime)
    {
        if (!animateIntensity)
            return Mathf.Max(0f, intensity);

        float noise = Mathf.PerlinNoise(
            SeedOffset(11),
            unscaledTime * flickerSpeed + SeedOffset(23));
        float multiplier = Mathf.Lerp(
            1f - flickerAmount,
            1f + flickerAmount,
            noise);
        return Mathf.Max(0f, intensity * multiplier);
    }

    public float EvaluateSpatialFalloff(float distanceInCells)
    {
        float radius = Mathf.Max(0.0001f, radiusInCells);
        float normalizedDistance = Mathf.Clamp01(
            Mathf.Max(0f, distanceInCells) / radius);
        float falloff = Mathf.Pow(
            1f - normalizedDistance,
            Mathf.Max(0.1f, falloffPower));
        float innerRadius = Mathf.Clamp(innerRadiusInCells, 0f, radius);
        float innerAmount = innerRadius > 0.0001f
            ? Mathf.Clamp01(1f - distanceInCells / innerRadius)
            : 0f;
        return falloff * (1f + innerAmount * Mathf.Max(0f, coreBoost));
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
        falloffPower = Mathf.Clamp(falloffPower, 0.1f, 8f);
        innerRadiusInCells = Mathf.Clamp(innerRadiusInCells, 0f, radiusInCells);
        coreBoost = Mathf.Clamp(coreBoost, 0f, 8f);
        flickerAmount = Mathf.Clamp01(flickerAmount);
        flickerSpeed = Mathf.Clamp(flickerSpeed, 0.01f, 10f);
        colorAnimationSpeed = Mathf.Clamp(colorAnimationSpeed, 0.01f, 10f);
        colorAnimationAmount = Mathf.Clamp01(colorAnimationAmount);
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

    float SeedOffset(int salt)
    {
        uint value = unchecked((uint)(animationSeed + salt * 1103515245));
        value ^= value >> 16;
        return (value & 0x00ffffff) / 16777215f * 1024f;
    }

    static Color NonNegative(Color value)
    {
        return new Color(
            Mathf.Max(0f, value.r),
            Mathf.Max(0f, value.g),
            Mathf.Max(0f, value.b),
            1f);
    }
}
