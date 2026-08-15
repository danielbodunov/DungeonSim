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

    [Header("Adventurer Aura")]
    [SerializeField, Min(0)] int adventurerAura;
    [SerializeField, Min(1)] int dungeonLevel = 1;
    [SerializeField, Min(0)] int auraPerNewCell = 1;
    [SerializeField, Min(0)] int auraPerDamage = 1;
    [SerializeField, Min(0)] int baseDefeatAura = 10;
    [SerializeField, Min(1f)] float defeatLevelExponent = 2f;

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
    readonly Dictionary<NPCCharacter, int> pendingVisitAura = new();
    readonly List<AuraHarvestRecord> auraHarvests = new();
    readonly Dictionary<string, AuraHarvestRecord> auraHarvestsById = new();

    public static GameplayLoopController Instance { get; private set; }

    public DungeonPhase Phase { get; private set; } = DungeonPhase.Expansion;
    public float ExplorationTimeRemaining { get; private set; }
    public float SelectedSpeed => selectedSpeed;
    public bool IsPaused => DungeonSimulationState.IsPaused;
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
    public int AdventurerAura => adventurerAura;
    public IReadOnlyList<AuraHarvestRecord> AuraHarvests => auraHarvests;
    public int AuraHarvestCount => auraHarvests.Count;
    public int TotalHarvestedAura
    {
        get
        {
            int total = 0;
            for (int i = 0; i < auraHarvests.Count; i++)
                if (auraHarvests[i] != null)
                    total += auraHarvests[i].Amount;
            return total;
        }
    }
    public int DungeonLevel => dungeonLevel;
    public int PendingAdventurerAura
    {
        get
        {
            int total = 0;
            foreach (int amount in pendingVisitAura.Values)
                total += amount;
            return total;
        }
    }
    public IReadOnlyList<NPCCharacterRecord> AdventurerRoster => adventurerRoster;

    public event Action StateChanged;
    public event Action<AuraHarvestRecord> AuraHarvested;

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
        DungeonSimulationState.PauseChanged += OnSimulationPauseChanged;
        tilePlacement = FindAnyObjectByType<TilePlacement>();
        tileGrid = FindAnyObjectByType<TileGridGenerator>();
        npcTraversal = FindAnyObjectByType<NPCTraversal>();
        if (npcTraversal != null)
        {
            npcTraversal.AdventurerDefeated += OnAdventurerDefeated;
            npcTraversal.AdventurerCellEntered += OnAdventurerCellEntered;
        }
        if (GetComponent<GameSaveManager>() == null)
            gameObject.AddComponent<GameSaveManager>();
        if (GetComponent<NPCActionFeedbackUI>() == null)
            gameObject.AddComponent<NPCActionFeedbackUI>();
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
        ReturnAllActiveAdventurersOutside();
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
        ReturnAllActiveAdventurersOutside();
        npcTraversal?.ClearAdventurers();
        activeRecords.Clear();
        PrepareRoundVisitors();
        SpawnNextAdventurer();
        spawnTimer = adventurerSpawnInterval;
        StateChanged?.Invoke();
    }

    public void ClearAdventurers()
    {
        ReturnAllActiveAdventurersOutside();
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
        if (!DungeonSimulationState.SetPaused(paused))
            StateChanged?.Invoke();
    }

    void OnSimulationPauseChanged(bool paused)
    {
        if (!paused)
            Time.timeScale = selectedSpeed;
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
        int savedAdventurerAura,
        int savedDungeonLevel,
        List<NPCCharacterRecord> livingAdventurers)
    {
        SetPaused(false);
        SetExpansion();
        dungeonOpenCount = Mathf.Max(0, savedDungeonOpenCount);
        adventurerAura = Mathf.Max(0, savedAdventurerAura);
        dungeonLevel = Mathf.Max(1, savedDungeonLevel);
        ClearAuraHarvestHistory();
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
        NPCTraversalAgent spawned = npcTraversal.SpawnAdventurer(record, false);
        if (spawned != null)
        {
            activeRecords[spawned.Character] = record;
            pendingVisitAura[spawned.Character] = 0;
            spawned.Character.Damaged += OnAdventurerDamaged;
            spawned.DungeonVisitCompleted += OnDungeonVisitCompleted;
            visitorsScheduled++;
            if (!spawned.BeginDungeonVisit())
            {
                spawned.Character.Damaged -= OnAdventurerDamaged;
                activeRecords.Remove(spawned.Character);
                pendingVisitAura.Remove(spawned.Character);
                npcTraversal.DespawnAdventurer(spawned);
            }
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
            visitor.DungeonVisitCompleted -= OnDungeonVisitCompleted;
            visitor.Character.WriteToRecord(record);
            visitor.Character.Damaged -= OnAdventurerDamaged;
            SettleVisitAura(visitor.Character);
            activeRecords.Remove(visitor.Character);
        }
        StateChanged?.Invoke();
    }

    void OnAdventurerDefeated(NPCTraversalAgent visitor)
    {
        NPCCharacter character = visitor != null ? visitor.Character : null;
        NPCCharacterRecord record = null;
        bool hasActiveRecord = character != null &&
            activeRecords.TryGetValue(character, out record);

        if (visitor != null && character != null &&
            visitor.DiedDuringDungeonVisit)
        {
            string harvestId =
                $"aura:death:opening-{dungeonOpenCount}:agent-{visitor.RuntimeAgentId}";
            TryHarvestAura(
                new AuraHarvestRequest(
                    harvestId,
                    AuraHarvestSource.AdventurerDeath,
                    GetDeathAuraHarvestAmount(character.Level),
                    hasActiveRecord ? record.id : string.Empty,
                    character.CharacterName,
                    visitor.RuntimeAgentId,
                    character.Level,
                    dungeonOpenCount,
                    visitor.CurrentCell,
                    visitor.transform.position),
                out _);
        }

        if (hasActiveRecord)
        {
            character.RecordDungeonVisitCompleted();
            character.WriteToRecord(record);
            character.Damaged -= OnAdventurerDamaged;
            SettleVisitAura(character);
            activeRecords.Remove(character);
        }
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Credits an Aura harvest exactly once. Callers provide a stable harvest
    /// ID plus source context; the currency mutation remains owned here.
    /// </summary>
    public bool TryHarvestAura(
        AuraHarvestRequest request,
        out AuraHarvestRecord harvest)
    {
        harvest = null;
        if (string.IsNullOrWhiteSpace(request.HarvestId) || request.Amount <= 0)
            return false;

        if (auraHarvestsById.TryGetValue(request.HarvestId, out harvest))
        {
            harvest.RecordDuplicateAttempt();
            StateChanged?.Invoke();
            return false;
        }

        harvest = new AuraHarvestRecord(request);
        auraHarvests.Add(harvest);
        auraHarvestsById.Add(harvest.HarvestId, harvest);
        adventurerAura += harvest.Amount;
        AuraHarvested?.Invoke(harvest);
        StateChanged?.Invoke();
        return true;
    }

    void ClearAuraHarvestHistory()
    {
        auraHarvests.Clear();
        auraHarvestsById.Clear();
    }

    void OnAdventurerCellEntered(
        NPCTraversalAgent visitor,
        Vector2Int cell,
        bool firstVisit)
    {
        if (visitor == null || !firstVisit)
            return;
        AddPendingAura(visitor.Character, auraPerNewCell);
    }

    void OnAdventurerDamaged(NPCCharacter character, int appliedDamage)
    {
        AddPendingAura(character, appliedDamage * auraPerDamage);
    }

    void AddPendingAura(NPCCharacter character, int amount)
    {
        if (character == null || amount <= 0 || !pendingVisitAura.ContainsKey(character))
            return;
        pendingVisitAura[character] += amount;
    }

    void SettleVisitAura(NPCCharacter character)
    {
        if (character == null || !pendingVisitAura.TryGetValue(character, out int amount))
            return;
        adventurerAura += Mathf.Max(0, amount);
        pendingVisitAura.Remove(character);
    }

    public int GetDeathAuraHarvestAmount(int adventurerLevel)
    {
        return Mathf.Max(0, Mathf.RoundToInt(
            baseDefeatAura * Mathf.Pow(Mathf.Max(1, adventurerLevel), defeatLevelExponent)));
    }

    public bool TrySpendAura(int amount)
    {
        if (amount < 0 || adventurerAura < amount)
            return false;
        adventurerAura -= amount;
        StateChanged?.Invoke();
        return true;
    }

    public bool TryPurchaseDungeonLevel(int auraCost)
    {
        if (auraCost <= 0 || adventurerAura < auraCost)
            return false;
        adventurerAura -= auraCost;
        dungeonLevel++;
        StateChanged?.Invoke();
        return true;
    }

    void StoreActiveAdventurers()
    {
        foreach (KeyValuePair<NPCCharacter, NPCCharacterRecord> pair in activeRecords)
            if (pair.Key != null && !pair.Key.IsDead)
                pair.Key.WriteToRecord(pair.Value);
    }

    void ReturnAllActiveAdventurersOutside()
    {
        if (activeRecords.Count == 0)
        {
            pendingVisitAura.Clear();
            return;
        }

        var activeCharacters = new List<NPCCharacter>(activeRecords.Keys);
        foreach (NPCCharacter character in activeCharacters)
        {
            if (character == null ||
                !activeRecords.TryGetValue(character, out NPCCharacterRecord record))
            {
                continue;
            }

            if (!character.IsDead)
                character.RecordDungeonVisitCompleted();
            character.WriteToRecord(record);
            character.Damaged -= OnAdventurerDamaged;
            SettleVisitAura(character);
        }
        activeRecords.Clear();
        pendingVisitAura.Clear();
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        DungeonSimulationState.PauseChanged -= OnSimulationPauseChanged;
        DungeonSimulationState.SetPaused(false);
        Time.timeScale = 1f;
        if (npcTraversal != null)
        {
            npcTraversal.AdventurerDefeated -= OnAdventurerDefeated;
            npcTraversal.AdventurerCellEntered -= OnAdventurerCellEntered;
        }
        Instance = null;
    }
}
