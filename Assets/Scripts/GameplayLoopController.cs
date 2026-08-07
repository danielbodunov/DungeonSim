using System;
using System.Collections.Generic;
using UnityEngine;

public enum DungeonPhase
{
    Expansion,
    Exploring
}

/// <summary>
/// Owns the prototype day/night loop, simulation speed, dungeon rating, and
/// rating-based adventurer spawning.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class GameplayLoopController : MonoBehaviour
{
    [Header("Phase Timing")]
    [SerializeField, Min(5f)] float explorationDuration = 60f;

    [Header("Dungeon Rating")]
    [SerializeField, Min(1)] int placedCellsPerRating = 5;
    [SerializeField, Range(1, 10)] int maximumRating = 5;

    [Header("Adventurers")]
    [SerializeField, Min(0.1f)] float adventurerSpawnInterval = 5f;

    TilePlacement tilePlacement;
    TileGridGenerator tileGrid;
    NPCTraversal npcTraversal;
    float selectedSpeed = 1f;
    float spawnTimer;
    int visitorsScheduled;
    [SerializeField, Min(0)] int dungeonOpenCount;
    [SerializeField] List<NPCCharacterRecord> adventurerRoster = new();
    readonly List<NPCCharacterRecord> roundVisitors = new();
    readonly Dictionary<NPCCharacter, NPCCharacterRecord> activeRecords = new();

    public static GameplayLoopController Instance { get; private set; }

    public DungeonPhase Phase { get; private set; } = DungeonPhase.Expansion;
    public float ExplorationTimeRemaining { get; private set; }
    public float SelectedSpeed => selectedSpeed;
    public bool IsPaused { get; private set; }
    public bool CanBuild => Phase == DungeonPhase.Expansion;
    public int PlacedCellCount => tileGrid != null ? tileGrid.PlacedCellCount : 0;
    public int DungeonRating => Mathf.Clamp(
        Mathf.CeilToInt(PlacedCellCount / (float)placedCellsPerRating),
        1,
        maximumRating);
    public int MaximumAdventurers => DungeonRating;
    public int ActiveAdventurers => npcTraversal != null
        ? npcTraversal.ActiveAgentCount
        : 0;
    public int DungeonOpenCount => dungeonOpenCount;
    public int DaysOpened => dungeonOpenCount;
    public IReadOnlyList<NPCCharacterRecord> AdventurerRoster => adventurerRoster;

    public event Action StateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapGameplayLoop()
    {
        if (FindAnyObjectByType<GameplayLoopController>() != null)
            return;

        if (FindAnyObjectByType<TilePlacement>() == null &&
            FindAnyObjectByType<NPCTraversal>() == null)
            return;

        new GameObject("Gameplay Loop").AddComponent<GameplayLoopController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        tilePlacement = FindAnyObjectByType<TilePlacement>();
        tileGrid = FindAnyObjectByType<TileGridGenerator>();
        npcTraversal = FindAnyObjectByType<NPCTraversal>();
        if (npcTraversal != null)
            npcTraversal.AdventurerDied += OnAdventurerDied;
        if (GetComponent<GameSaveManager>() == null)
            gameObject.AddComponent<GameSaveManager>();
    }

    void Start()
    {
        SetExpansion();
        if (FindAnyObjectByType<GameplayLoopUI>() == null)
            gameObject.AddComponent<GameplayLoopUI>();
    }

    void Update()
    {
        if (Phase != DungeonPhase.Exploring || IsPaused)
            return;

        ExplorationTimeRemaining = Mathf.Max(
            0f,
            ExplorationTimeRemaining - Time.deltaTime);

        if (visitorsScheduled < MaximumAdventurers)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnNextAdventurer();
                spawnTimer = adventurerSpawnInterval;
            }
        }

        // Do not close during the delay between staggered spawns. Once the
        // entire group has entered, the round ends as soon as the last NPC has
        // either returned through the entrance or died in the dungeon.
        if (visitorsScheduled >= MaximumAdventurers && ActiveAdventurers == 0)
        {
            SetExpansion();
            return;
        }

        if (ExplorationTimeRemaining <= 0f)
            SetExpansion();
    }

    public void OpenDungeon()
    {
        SetExploring();
    }

    public void SetExpansion()
    {
        Phase = DungeonPhase.Expansion;
        ExplorationTimeRemaining = 0f;
        visitorsScheduled = 0;
        tilePlacement?.SetBuildingEnabled(true);
        StoreActiveAdventurers();
        npcTraversal?.ClearAdventurers();
        activeRecords.Clear();
        StateChanged?.Invoke();
    }

    public void SetExploring()
    {
        bool isNewOpening = Phase != DungeonPhase.Exploring;
        Phase = DungeonPhase.Exploring;
        if (isNewOpening)
            dungeonOpenCount++;
        ExplorationTimeRemaining = explorationDuration;
        visitorsScheduled = 0;
        spawnTimer = 0f;
        tilePlacement?.SetBuildingEnabled(false);
        StoreActiveAdventurers();
        npcTraversal?.ClearAdventurers();
        activeRecords.Clear();
        PrepareRoundVisitors();
        SpawnNextAdventurer();
        spawnTimer = adventurerSpawnInterval;
        StateChanged?.Invoke();
    }

    public void ClearAdventurers()
    {
        StoreActiveAdventurers();
        npcTraversal?.ClearAdventurers();
        activeRecords.Clear();
        StateChanged?.Invoke();
    }

    public void SetGameplaySpeed(float speed)
    {
        selectedSpeed = Mathf.Clamp(speed, 0.1f, 10f);
        if (!IsPaused)
            Time.timeScale = selectedSpeed;
        StateChanged?.Invoke();
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : selectedSpeed;
        StateChanged?.Invoke();
    }

    public List<NPCCharacterRecord> CaptureLivingAdventurers()
    {
        StoreActiveAdventurers();
        var snapshot = new List<NPCCharacterRecord>(adventurerRoster.Count);
        foreach (NPCCharacterRecord record in adventurerRoster)
        {
            if (record == null)
                continue;
            snapshot.Add(CopyRecord(record));
        }
        return snapshot;
    }

    public void RestoreProgress(
        int savedDungeonOpenCount,
        float savedGameplaySpeed,
        List<NPCCharacterRecord> livingAdventurers)
    {
        SetPaused(false);
        SetExpansion();
        dungeonOpenCount = Mathf.Max(0, savedDungeonOpenCount);
        adventurerRoster.Clear();

        var ids = new HashSet<string>();
        if (livingAdventurers != null)
        {
            foreach (NPCCharacterRecord savedRecord in livingAdventurers)
            {
                if (savedRecord == null)
                    continue;

                NPCCharacterRecord restored = CopyRecord(savedRecord);
                if (string.IsNullOrWhiteSpace(restored.id) || !ids.Add(restored.id))
                {
                    restored.id = Guid.NewGuid().ToString("N");
                    ids.Add(restored.id);
                }
                adventurerRoster.Add(restored);
            }
        }

        SetGameplaySpeed(savedGameplaySpeed);
        StateChanged?.Invoke();
    }

    static NPCCharacterRecord CopyRecord(NPCCharacterRecord source)
    {
        return new NPCCharacterRecord
        {
            id = source.id,
            characterName = source.characterName,
            level = source.level,
            experience = source.experience,
            maxHealth = source.maxHealth,
            maxStamina = source.maxStamina,
            strength = source.strength,
            dexterity = source.dexterity,
            luck = source.luck,
            intelligence = source.intelligence,
            dungeonVisits = source.dungeonVisits
        };
    }

    void SpawnNextAdventurer()
    {
        if (npcTraversal == null || visitorsScheduled >= MaximumAdventurers)
            return;

        if (visitorsScheduled >= roundVisitors.Count)
            return;

        NPCCharacterRecord record = roundVisitors[visitorsScheduled];
        NPCTraversalAgent spawned = npcTraversal.SpawnAdventurer(record);
        if (spawned != null)
        {
            activeRecords[spawned.Character] = record;
            spawned.DungeonVisitCompleted += OnDungeonVisitCompleted;
            visitorsScheduled++;
        }
    }

    void PrepareRoundVisitors()
    {
        var names = new HashSet<string>();
        foreach (NPCCharacterRecord record in adventurerRoster)
            if (!string.IsNullOrWhiteSpace(record.characterName))
                names.Add(record.characterName);

        while (adventurerRoster.Count < MaximumAdventurers)
        {
            NPCCharacterRecord generated = AdventurerNameGenerator.Create(names);
            adventurerRoster.Add(generated);
            names.Add(generated.characterName);
        }

        roundVisitors.Clear();
        for (int i = 0; i < Mathf.Min(MaximumAdventurers, adventurerRoster.Count); i++)
            roundVisitors.Add(adventurerRoster[i]);
    }

    void OnDungeonVisitCompleted(NPCTraversalAgent visitor)
    {
        if (visitor != null && visitor.Character != null &&
            activeRecords.TryGetValue(visitor.Character, out NPCCharacterRecord record))
        {
            visitor.Character.WriteToRecord(record);
            activeRecords.Remove(visitor.Character);
        }
        StateChanged?.Invoke();
    }

    void OnAdventurerDied(NPCCharacter character)
    {
        if (character != null && activeRecords.TryGetValue(character, out NPCCharacterRecord record))
        {
            activeRecords.Remove(character);
            adventurerRoster.Remove(record);
        }
        StateChanged?.Invoke();
    }

    void StoreActiveAdventurers()
    {
        foreach (KeyValuePair<NPCCharacter, NPCCharacterRecord> pair in activeRecords)
            if (pair.Key != null && !pair.Key.IsDead)
                pair.Key.WriteToRecord(pair.Value);
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        Time.timeScale = 1f;
        if (npcTraversal != null)
            npcTraversal.AdventurerDied -= OnAdventurerDied;
        Instance = null;
    }
}
