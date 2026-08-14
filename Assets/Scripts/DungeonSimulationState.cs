using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Authoritative pause state for dungeon gameplay simulation. Presentation,
/// input, and Editor tooling remain independent of this state.
/// </summary>
public static class DungeonSimulationState
{
    static bool isPaused;

    public static bool IsPaused => isPaused;
    public static bool CanAdvance => !isPaused;
    public static float DeltaTime => isPaused ? 0f : Time.deltaTime;

    public static event Action<bool> PauseChanged;

    public static bool SetPaused(bool paused)
    {
        if (isPaused == paused)
            return false;

        isPaused = paused;
        PauseChanged?.Invoke(isPaused);
        return true;
    }

    public static void TogglePause()
    {
        SetPaused(!isPaused);
    }

    /// <summary>
    /// Waits for scaled gameplay time while preserving the remaining duration
    /// across simulation pauses.
    /// </summary>
    public static IEnumerator WaitForSimulationSeconds(float duration)
    {
        float remaining = Mathf.Max(0f, duration);
        while (remaining > 0f)
        {
            yield return null;
            remaining = Mathf.Max(0f, remaining - DeltaTime);
        }
    }

    public static IEnumerator WaitUntilRunning()
    {
        while (isPaused)
            yield return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        isPaused = false;
        PauseChanged = null;
    }
}
