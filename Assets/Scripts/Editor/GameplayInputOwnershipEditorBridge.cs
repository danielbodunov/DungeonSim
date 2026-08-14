using System;
using UnityEditor;

/// <summary>
/// Supplies editor surface ownership to the production input layer without
/// introducing UnityEditor references into the runtime assembly.
/// </summary>
[InitializeOnLoad]
internal static class GameplayInputOwnershipEditorBridge
{
    static readonly Func<GameplayInputOwnership> ResolveOwnership =
        GetCurrentOwnership;

    static GameplayInputOwnershipEditorBridge()
    {
        Register();
        AssemblyReloadEvents.beforeAssemblyReload += Unregister;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            Register();
    }

    static void Register()
    {
        InputManager.RegisterInputOwnershipResolver(ResolveOwnership);
    }

    static void Unregister()
    {
        InputManager.UnregisterInputOwnershipResolver(ResolveOwnership);
        AssemblyReloadEvents.beforeAssemblyReload -= Unregister;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    static GameplayInputOwnership GetCurrentOwnership()
    {
        if (!EditorApplication.isPlaying)
            return GameplayInputOwnership.None;

        bool pointerOwned = IsGameView(EditorWindow.mouseOverWindow);
        bool keyboardOwned = IsGameView(EditorWindow.focusedWindow);
        return new GameplayInputOwnership(pointerOwned, keyboardOwned);
    }

    static bool IsGameView(EditorWindow window)
    {
        return window != null &&
            window.GetType().FullName == "UnityEditor.GameView";
    }
}
