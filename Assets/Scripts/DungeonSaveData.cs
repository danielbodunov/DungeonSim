using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public class DungeonSaveData
{
    public const int CurrentVersion = 10;

    public int version = CurrentVersion;
    public string saveName;
    public string savedAtUtc;
    public int gridWidth;
    public int gridHeight;
    public int dungeonOpenCount;
    [FormerlySerializedAs("adventurerAura")]
    public int dread;
    public int dungeonLevel = 1;
    public float selectedGameplaySpeed = 1f;
    public int propGenerationSeed;
    public List<NPCCharacterRecord> livingAdventurers = new();
    public List<SavedTileCell> tileCells = new();
    public List<SavedConnectionEdge> connectionEdges = new();
    public List<SavedTrapCell> traps = new();
    public List<SavedFloorPropCell> floorProps = new();
    public List<RecoverableLootDrop> recoverableLootDrops = new();
    public int nextRecoverableLootDropNumber = 1;
    public List<DungeonStoredLootItem> recoveredLootInventory = new();
    public List<PlayerLootRecoveryRecord> playerLootRecoveries = new();
    public List<DreadSpendRecord> dreadSpends = new();
    public SavedEntrance entrance;
}

[Serializable]
public class SavedTileCell
{
    public int x;
    public int y;
    public bool isPlaced;
    public string profileId;
    public CellWidthIntent widthIntent;
}

[Serializable]
public class SavedTrapCell
{
    public int x;
    public int y;
    public int objectId = -1;
    public string prefabName;
}

[Serializable]
public class SavedFloorPropCell
{
    public int x;
    public int y;
    public int objectId = -1;
    public string prefabName;
    public bool isResolved;
}

[Serializable]
public class SavedEntrance
{
    public int x;
    public int y;
    public int objectId = -1;
    public string prefabName;
}

[Serializable]
public class SavedConnectionEdge
{
    public int fromX;
    public int fromY;
    public int toX;
    public int toY;
    public ConnectionIntent intent;
}
