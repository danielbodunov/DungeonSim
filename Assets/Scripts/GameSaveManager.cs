using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SaveSlotInfo
{
    public string SaveName { get; set; }
    public string FilePath { get; set; }
    public string SavedAtUtc { get; set; }
    public DateTime SortTimeUtc { get; set; }
    public int DungeonOpenCount { get; set; }
    public int AdventurerCount { get; set; }
    public int TileCellCount { get; set; }
    public int AdventurerAura { get; set; }
    public int DungeonLevel { get; set; }
}

/// <summary>Writes, discovers, and restores named local dungeon checkpoints.</summary>
[DisallowMultipleComponent]
public class GameSaveManager : MonoBehaviour
{
    const string LegacySaveFileName = "dungeon-save.json";
    const string SavesFolderName = "Saves";
    const string DefaultSaveName = "Quick Save";

    GameplayLoopController gameplayLoop;
    TileGridGenerator tileGrid;
    TilePlacement tilePlacement;

    public string SavesDirectory => Path.Combine(
        Application.persistentDataPath, SavesFolderName);
    public string LegacySavePath => Path.Combine(
        Application.persistentDataPath, LegacySaveFileName);
    public bool HasSave => GetSaveSlots().Count > 0;
    public string LastStatus { get; private set; } = string.Empty;

    public event Action StatusChanged;

    void Awake()
    {
        ResolveReferences();
    }

    public bool SaveGame()
    {
        return SaveGame(DefaultSaveName);
    }

    public bool SaveGame(string saveName)
    {
        ResolveReferences();
        if (gameplayLoop == null || tileGrid == null || !tileGrid.IsInitialized)
            return ReportFailure("Save unavailable: the dungeon is still initializing.");
        if (gameplayLoop.Phase != DungeonPhase.Expansion)
            return ReportFailure("Finish the current dungeon visit before saving.");

        saveName = NormalizeSaveName(saveName);
        if (string.IsNullOrEmpty(saveName))
            return ReportFailure("Enter a name for this save.");

        var save = new DungeonSaveData
        {
            saveName = saveName,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            gridWidth = tileGrid.GridWidth,
            gridHeight = tileGrid.GridHeight,
            dungeonOpenCount = gameplayLoop.DungeonOpenCount,
            adventurerAura = gameplayLoop.AdventurerAura,
            dungeonLevel = gameplayLoop.DungeonLevel,
            selectedGameplaySpeed = gameplayLoop.SelectedSpeed,
            propGenerationSeed = tileGrid.PropGenerationSeed,
            livingAdventurers = gameplayLoop.CaptureLivingAdventurers(),
            tileCells = tileGrid.CaptureTileLayout(),
            connectionEdges = tileGrid.CaptureConnectionIntents(),
            traps = tileGrid.CaptureTrapLayout(),
            floorProps = tileGrid.CaptureFloorPropLayout(),
            entrance = tileGrid.CaptureEntranceLayout()
        };

        string savePath = GetNamedSavePath(saveName);
        string temporaryPath = savePath + ".tmp";
        try
        {
            Directory.CreateDirectory(SavesDirectory);
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(save, true));
            File.Copy(temporaryPath, savePath, true);
            File.Delete(temporaryPath);
        }
        catch (Exception exception)
        {
            TryDeleteTemporaryFile(temporaryPath);
            Debug.LogException(exception, this);
            return ReportFailure("Save failed. See the Console for details.");
        }

        return ReportSuccess(
            $"Saved '{saveName}' with {save.tileCells.Count} cells, " +
            $"{save.floorProps.Count} floor props, and " +
            $"{save.livingAdventurers.Count} adventurers.");
    }

    public bool LoadGame()
    {
        return LoadLastSave();
    }

    public bool LoadLastSave()
    {
        List<SaveSlotInfo> saves = GetSaveSlots();
        if (saves.Count == 0)
            return ReportFailure("No saved game exists yet.");
        return LoadGame(saves[0]);
    }

    public bool LoadGame(SaveSlotInfo slot)
    {
        if (slot == null || string.IsNullOrWhiteSpace(slot.FilePath))
            return ReportFailure("Select a saved game to load.");
        return LoadGameFromPath(slot.FilePath);
    }

    public List<SaveSlotInfo> GetSaveSlots()
    {
        var saves = new List<SaveSlotInfo>();
        if (Directory.Exists(SavesDirectory))
        {
            foreach (string path in Directory.GetFiles(SavesDirectory, "*.json"))
            {
                SaveSlotInfo info = ReadSaveSlot(path);
                if (info != null)
                    saves.Add(info);
            }
        }

        if (File.Exists(LegacySavePath))
        {
            SaveSlotInfo legacy = ReadSaveSlot(LegacySavePath, "Legacy Save");
            if (legacy != null)
                saves.Add(legacy);
        }

        saves.Sort((left, right) => right.SortTimeUtc.CompareTo(left.SortTimeUtc));
        return saves;
    }

    bool LoadGameFromPath(string savePath)
    {
        ResolveReferences();
        if (gameplayLoop == null || tileGrid == null || !tileGrid.IsInitialized)
            return ReportFailure("Load unavailable: the dungeon is still initializing.");
        if (!File.Exists(savePath))
            return ReportFailure("That saved game no longer exists.");

        DungeonSaveData save;
        try
        {
            save = JsonUtility.FromJson<DungeonSaveData>(File.ReadAllText(savePath));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return ReportFailure("The saved game could not be read.");
        }

        if (save == null || save.version <= 0 ||
            save.version > DungeonSaveData.CurrentVersion)
        {
            return ReportFailure("The saved game uses an unsupported format.");
        }
        if (save.gridWidth != tileGrid.GridWidth || save.gridHeight != tileGrid.GridHeight)
        {
            return ReportFailure(
                $"Save grid is {save.gridWidth}x{save.gridHeight}, but this scene uses " +
                $"{tileGrid.GridWidth}x{tileGrid.GridHeight}.");
        }

        List<SavedTileCell> previousTiles = tileGrid.CaptureTileLayout();
        List<SavedConnectionEdge> previousConnections =
            tileGrid.CaptureConnectionIntents();
        List<SavedTrapCell> previousTraps = tileGrid.CaptureTrapLayout();
        List<SavedFloorPropCell> previousFloorProps =
            tileGrid.CaptureFloorPropLayout();
        SavedEntrance previousEntrance = tileGrid.CaptureEntranceLayout();
        int previousPropSeed = tileGrid.PropGenerationSeed;
        if (!tileGrid.RestoreTileLayout(save.tileCells, save.connectionEdges))
        {
            if (tileGrid.RestoreTileLayout(previousTiles, previousConnections))
            {
                RestoreTraps(previousTraps);
                RestoreFloorProps(previousFloorProps);
                RestoreEntrance(previousEntrance);
                tileGrid.RegenerateProps(previousPropSeed);
            }
            return ReportFailure(
                "The saved tile layout is incompatible with the current tile database.");
        }

        tilePlacement?.StopPlacement();
        gameplayLoop.RestoreProgress(
            save.dungeonOpenCount,
            save.selectedGameplaySpeed,
            save.adventurerAura,
            save.dungeonLevel,
            save.livingAdventurers);

        int restoredTraps = RestoreTraps(save.traps);
        bool restoredEntrance = RestoreEntrance(save.entrance);
        int restoredFloorProps = RestoreFloorProps(save.floorProps);
        tileGrid.RegenerateProps(save.propGenerationSeed);
        string displayName = string.IsNullOrWhiteSpace(save.saveName)
            ? Path.GetFileNameWithoutExtension(savePath)
            : save.saveName;
        return ReportSuccess(
            $"Loaded '{displayName}': {save.tileCells.Count} cells, " +
            $"{restoredTraps} traps, " +
            $"{restoredFloorProps} floor props, " +
            $"{(restoredEntrance ? "an entrance" : "no entrance")}, and " +
            $"{gameplayLoop.AdventurerRoster.Count} adventurers.");
    }

    SaveSlotInfo ReadSaveSlot(string path, string fallbackName = null)
    {
        try
        {
            DungeonSaveData save = JsonUtility.FromJson<DungeonSaveData>(
                File.ReadAllText(path));
            if (save == null || save.version <= 0 ||
                save.version > DungeonSaveData.CurrentVersion)
            {
                return null;
            }

            DateTime fileTime = File.GetLastWriteTimeUtc(path);
            DateTime sortTime = DateTime.TryParse(
                save.savedAtUtc,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out DateTime parsedTime)
                    ? parsedTime.ToUniversalTime()
                    : fileTime;
            string displayName = string.IsNullOrWhiteSpace(save.saveName)
                ? fallbackName ?? Path.GetFileNameWithoutExtension(path)
                : save.saveName;
            return new SaveSlotInfo
            {
                SaveName = displayName,
                FilePath = path,
                SavedAtUtc = save.savedAtUtc,
                SortTimeUtc = sortTime,
                DungeonOpenCount = save.dungeonOpenCount,
                AdventurerCount = save.livingAdventurers?.Count ?? 0,
                TileCellCount = save.tileCells?.Count ?? 0,
                AdventurerAura = Mathf.Max(0, save.adventurerAura),
                DungeonLevel = Mathf.Max(1, save.dungeonLevel)
            };
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Ignoring unreadable save file '{Path.GetFileName(path)}': " +
                exception.Message,
                this);
            return null;
        }
    }

    string GetNamedSavePath(string saveName)
    {
        string slug = BuildSafeSlug(saveName);
        uint hash = 2166136261;
        foreach (char value in saveName.ToLowerInvariant())
        {
            hash ^= value;
            hash *= 16777619;
        }
        return Path.Combine(SavesDirectory, $"{slug}-{hash:x8}.json");
    }

    static string NormalizeSaveName(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            return string.Empty;
        string normalized = saveName.Trim();
        return normalized.Length <= 64 ? normalized : normalized.Substring(0, 64);
    }

    static string BuildSafeSlug(string saveName)
    {
        var characters = new List<char>(saveName.Length);
        foreach (char value in saveName)
        {
            if (char.IsLetterOrDigit(value))
                characters.Add(char.ToLowerInvariant(value));
            else if ((value == ' ' || value == '-' || value == '_') &&
                characters.Count > 0 && characters[characters.Count - 1] != '-')
                characters.Add('-');
        }

        string slug = new string(characters.ToArray()).Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = "save";
        return slug.Length <= 40 ? slug : slug.Substring(0, 40).TrimEnd('-');
    }

    static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Preserve the original save error; a stale .tmp file is ignored.
        }
    }

    int RestoreTraps(List<SavedTrapCell> traps)
    {
        tileGrid.ClearTraps();
        if (traps == null)
            return 0;

        int restored = 0;
        foreach (SavedTrapCell savedTrap in traps)
        {
            GameObject prefab = FindObjectPrefab(savedTrap.objectId);
            if (prefab == null && !string.IsNullOrWhiteSpace(savedTrap.prefabName))
            {
                prefab = Resources.Load<GameObject>($"Traps/{savedTrap.prefabName}")
                    ?? Resources.Load<GameObject>(savedTrap.prefabName);
            }

            if (prefab != null && tileGrid.PlaceTrapCell(
                savedTrap.x, savedTrap.y, prefab, savedTrap.objectId))
            {
                restored++;
            }
            else
            {
                Debug.LogWarning(
                    $"Could not restore trap '{savedTrap.prefabName}' at " +
                    $"({savedTrap.x},{savedTrap.y}).", this);
            }
        }
        return restored;
    }

    int RestoreFloorProps(List<SavedFloorPropCell> floorProps)
    {
        tileGrid.ClearFloorProps();
        if (floorProps == null)
            return 0;

        int restored = 0;
        foreach (SavedFloorPropCell savedProp in floorProps)
        {
            if (savedProp == null)
                continue;

            GameObject prefab = FindObjectPrefab(savedProp.objectId);
            if (prefab == null && !string.IsNullOrWhiteSpace(savedProp.prefabName))
            {
                prefab = Resources.Load<GameObject>($"Props/{savedProp.prefabName}")
                    ?? Resources.Load<GameObject>(savedProp.prefabName);
            }

            if (prefab != null && tileGrid.PlaceFloorPropCell(
                    savedProp.x,
                    savedProp.y,
                    prefab,
                    savedProp.objectId,
                    savedProp.isResolved))
            {
                restored++;
            }
            else
            {
                Debug.LogWarning(
                    $"Could not restore floor prop '{savedProp.prefabName}' at " +
                    $"({savedProp.x},{savedProp.y}).", this);
            }
        }
        return restored;
    }

    bool RestoreEntrance(SavedEntrance entrance)
    {
        if (entrance == null)
        {
            tileGrid.UseDefaultEntrance();
            return tileGrid.TryGetDungeonEntrance(out _);
        }

        GameObject prefab = FindObjectPrefab(entrance.objectId);
        if (prefab == null && !string.IsNullOrWhiteSpace(entrance.prefabName))
        {
            prefab = Resources.Load<GameObject>($"Props/{entrance.prefabName}")
                ?? Resources.Load<GameObject>(entrance.prefabName);
        }

        if (prefab != null && tileGrid.PlaceEntranceCell(
                entrance.x, entrance.y, prefab, entrance.objectId))
        {
            return true;
        }

        Debug.LogWarning(
            $"Could not restore entrance '{entrance.prefabName}' at " +
            $"({entrance.x},{entrance.y}).", this);
        return false;
    }

    GameObject FindObjectPrefab(int objectId)
    {
        if (tilePlacement == null || objectId < 0)
            return null;

        IReadOnlyList<ObjectData> objects = tilePlacement.AvailableObjects;
        for (int i = 0; i < objects.Count; i++)
            if (objects[i].ID == objectId)
                return objects[i].Prefab;
        return null;
    }

    void ResolveReferences()
    {
        if (gameplayLoop == null)
            gameplayLoop = GetComponent<GameplayLoopController>()
                ?? FindAnyObjectByType<GameplayLoopController>();
        if (tileGrid == null)
            tileGrid = FindAnyObjectByType<TileGridGenerator>();
        if (tilePlacement == null)
            tilePlacement = FindAnyObjectByType<TilePlacement>();
    }

    bool ReportSuccess(string message)
    {
        LastStatus = message;
        Debug.Log(message, this);
        StatusChanged?.Invoke();
        return true;
    }

    bool ReportFailure(string message)
    {
        LastStatus = message;
        Debug.LogWarning(message, this);
        StatusChanged?.Invoke();
        return false;
    }
}
