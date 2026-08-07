using System;
using System.Collections.Generic;

[Serializable]
public class DungeonSaveData
{
    public const int CurrentVersion = 2;

    public int version = CurrentVersion;
    public string saveName;
    public string savedAtUtc;
    public int gridWidth;
    public int gridHeight;
    public int dungeonOpenCount;
    public float selectedGameplaySpeed = 1f;
    public int propGenerationSeed;
    public List<NPCCharacterRecord> livingAdventurers = new();
    public List<SavedTileCell> tileCells = new();
    public List<SavedTrapCell> traps = new();
}

[Serializable]
public class SavedTileCell
{
    public int x;
    public int y;
    public bool isPlaced;
    public string profileId;
}

[Serializable]
public class SavedTrapCell
{
    public int x;
    public int y;
    public int objectId = -1;
    public string prefabName;
}
