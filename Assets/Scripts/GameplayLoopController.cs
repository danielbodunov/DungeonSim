using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum DungeonPhase
{
    Expansion,
    Exploring
}

/// <summary>
/// Scenario baseline for persistent gameplay progress and auditable visit
/// results. Active visits are intentionally not resumed by scenario loading.
/// </summary>
[Serializable]
public sealed class GameplayLoopScenarioState
{
    [SerializeField, Min(0)] int dungeonOpenCount;
    [FormerlySerializedAs("adventurerAura")]
    [SerializeField, Min(0)] int dread;
    [SerializeField, Min(1)] int dungeonLevel = 1;
    [SerializeField, Range(0.1f, 10f)] float selectedSpeed = 1f;
    [SerializeField] List<NPCCharacterRecord> adventurerRoster = new();
    [FormerlySerializedAs("auraHarvests")]
    [SerializeField] List<DreadHarvestRecord> dreadHarvests = new();
    [SerializeField] List<DreadSpendRecord> dreadSpends = new();
    [SerializeField] List<ExpeditionOutcomeRecord> expeditionOutcomes = new();
    [SerializeField] List<DungeonStoredLootItem> recoveredLootInventory = new();
    [SerializeField] List<PlayerLootRecoveryRecord> playerLootRecoveries = new();

    public int DungeonOpenCount => Mathf.Max(0, dungeonOpenCount);
    public int Dread => Mathf.Max(0, dread);
    public int DungeonLevel => Mathf.Max(1, dungeonLevel);
    public float SelectedSpeed => Mathf.Clamp(selectedSpeed, 0.1f, 10f);
    public IReadOnlyList<NPCCharacterRecord> AdventurerRoster =>
        adventurerRoster ??
        (IReadOnlyList<NPCCharacterRecord>)Array.Empty<NPCCharacterRecord>();
    public IReadOnlyList<DreadHarvestRecord> DreadHarvests =>
        dreadHarvests ??
        (IReadOnlyList<DreadHarvestRecord>)Array.Empty<DreadHarvestRecord>();
    public IReadOnlyList<DreadSpendRecord> DreadSpends =>
        dreadSpends ??
        (IReadOnlyList<DreadSpendRecord>)Array.Empty<DreadSpendRecord>();
    public IReadOnlyList<ExpeditionOutcomeRecord> ExpeditionOutcomes =>
        expeditionOutcomes ??
        (IReadOnlyList<ExpeditionOutcomeRecord>)Array.Empty<ExpeditionOutcomeRecord>();
    public IReadOnlyList<DungeonStoredLootItem> RecoveredLootInventory =>
        recoveredLootInventory ??
        (IReadOnlyList<DungeonStoredLootItem>)Array.Empty<DungeonStoredLootItem>();
    public IReadOnlyList<PlayerLootRecoveryRecord> PlayerLootRecoveries =>
        playerLootRecoveries ??
        (IReadOnlyList<PlayerLootRecoveryRecord>)Array.Empty<PlayerLootRecoveryRecord>();

    public GameplayLoopScenarioState(
        int openCount,
        int savedDread,
        int level,
        float gameplaySpeed,
        IReadOnlyList<NPCCharacterRecord> roster,
        IReadOnlyList<DreadHarvestRecord> harvests,
        IReadOnlyList<ExpeditionOutcomeRecord> outcomes,
        IReadOnlyList<DungeonStoredLootItem> storedLoot = null,
        IReadOnlyList<PlayerLootRecoveryRecord> recoveries = null,
        IReadOnlyList<DreadSpendRecord> spends = null)
    {
        dungeonOpenCount = Mathf.Max(0, openCount);
        dread = Mathf.Max(0, savedDread);
        dungeonLevel = Mathf.Max(1, level);
        selectedSpeed = Mathf.Clamp(gameplaySpeed, 0.1f, 10f);
        adventurerRoster = CopyRoster(roster);
        dreadHarvests = CopyHarvests(harvests);
        dreadSpends = CopySpends(spends);
        expeditionOutcomes = CopyOutcomes(outcomes);
        recoveredLootInventory = CopyStoredLoot(storedLoot);
        playerLootRecoveries = CopyRecoveries(recoveries);
    }

    internal GameplayLoopScenarioState Copy()
    {
        return new GameplayLoopScenarioState(
            DungeonOpenCount,
            Dread,
            DungeonLevel,
            SelectedSpeed,
            AdventurerRoster,
            DreadHarvests,
            ExpeditionOutcomes,
            RecoveredLootInventory,
            PlayerLootRecoveries,
            DreadSpends);
    }

    static List<NPCCharacterRecord> CopyRoster(
        IReadOnlyList<NPCCharacterRecord> source)
    {
        var result = new List<NPCCharacterRecord>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
        {
            NPCCharacterRecord record = source[i];
            if (record == null)
                continue;
            result.Add(new NPCCharacterRecord
            {
                id = record.id,
                characterName = record.characterName,
                level = record.level,
                experience = record.experience,
                maxHealth = record.maxHealth,
                maxStamina = record.maxStamina,
                strength = record.strength,
                dexterity = record.dexterity,
                luck = record.luck,
                intelligence = record.intelligence,
                dungeonVisits = record.dungeonVisits,
                startingResources = AdventurerResourcePayload.CopyAll(
                    record.startingResources)
            });
        }
        return result;
    }

    static List<DreadHarvestRecord> CopyHarvests(
        IReadOnlyList<DreadHarvestRecord> source)
    {
        var result = new List<DreadHarvestRecord>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }

    static List<DreadSpendRecord> CopySpends(
        IReadOnlyList<DreadSpendRecord> source)
    {
        var result = new List<DreadSpendRecord>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }

    static List<ExpeditionOutcomeRecord> CopyOutcomes(
        IReadOnlyList<ExpeditionOutcomeRecord> source)
    {
        var result = new List<ExpeditionOutcomeRecord>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }

    static List<DungeonStoredLootItem> CopyStoredLoot(
        IReadOnlyList<DungeonStoredLootItem> source)
    {
        var result = new List<DungeonStoredLootItem>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }

    static List<PlayerLootRecoveryRecord> CopyRecoveries(
        IReadOnlyList<PlayerLootRecoveryRecord> source)
    {
        var result = new List<PlayerLootRecoveryRecord>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }
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

    [Header("Dread")]
    [FormerlySerializedAs("adventurerAura")]
    [SerializeField, Min(0)] int dread;
    [SerializeField, Min(1)] int dungeonLevel = 1;
    [FormerlySerializedAs("auraPerNewCell")]
    [SerializeField, Min(0)] int dreadPerNewCell = 1;
    [FormerlySerializedAs("auraPerDamage")]
    [SerializeField, Min(0)] int dreadPerDamage = 1;
    [FormerlySerializedAs("baseDefeatAura")]
    [SerializeField, Min(0)] int baseDefeatDread = 10;
    [SerializeField, Min(1f)] float defeatLevelExponent = 2f;
    [SerializeField, Min(1)] int treasureManifestationDreadCost = 10;

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
    readonly Dictionary<NPCCharacter, NPCTraversalAgent> activeVisitors = new();
    readonly Dictionary<NPCCharacter, int> pendingVisitDread = new();
    readonly List<DreadHarvestRecord> dreadHarvests = new();
    readonly Dictionary<string, DreadHarvestRecord> dreadHarvestsById = new();
    readonly List<DreadSpendRecord> dreadSpends = new();
    readonly Dictionary<string, DreadSpendRecord> dreadSpendsById = new();
    readonly List<ExpeditionOutcomeRecord> expeditionOutcomes = new();
    readonly Dictionary<string, ExpeditionOutcomeRecord> expeditionOutcomesById = new();
    [SerializeField] List<DungeonStoredLootItem> recoveredLootInventory = new();
    [SerializeField] List<PlayerLootRecoveryRecord> playerLootRecoveries = new();
    readonly Dictionary<string, PlayerLootRecoveryRecord> playerLootRecoveriesByDropId =
        new();
    readonly RaycastHit[] recoveryClickHits = new RaycastHit[32];
    InputManager inputManager;

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
    public TileGridGenerator DungeonGrid => tileGrid;
    public int DungeonOpenCount => dungeonOpenCount;
    public int DaysOpened => dungeonOpenCount;
    public int Dread => dread;
    public IReadOnlyList<DreadHarvestRecord> DreadHarvests => dreadHarvests;
    public int DreadHarvestCount => dreadHarvests.Count;
    public IReadOnlyList<DreadSpendRecord> DreadSpends => dreadSpends;
    public int DreadSpendCount => dreadSpends.Count;
    public int TreasureManifestationDreadCost =>
        Mathf.Max(1, treasureManifestationDreadCost);
    public string LastDreadActionMessage { get; private set; } = string.Empty;
    public IReadOnlyList<ExpeditionOutcomeRecord> ExpeditionOutcomes =>
        expeditionOutcomes;
    public int ExpeditionOutcomeCount => expeditionOutcomes.Count;
    public int TotalHarvestedDread
    {
        get
        {
            int total = 0;
            for (int i = 0; i < dreadHarvests.Count; i++)
                if (dreadHarvests[i] != null)
                    total += dreadHarvests[i].Amount;
            return total;
        }
    }
    public int TotalSpentDread
    {
        get
        {
            int total = 0;
            for (int i = 0; i < dreadSpends.Count; i++)
                if (dreadSpends[i] != null)
                    total += dreadSpends[i].Amount;
            return total;
        }
    }
    public int DungeonLevel => dungeonLevel;
    public int PendingVisitDread
    {
        get
        {
            int total = 0;
            foreach (int amount in pendingVisitDread.Values)
                total += amount;
            return total;
        }
    }
    public IReadOnlyList<NPCCharacterRecord> AdventurerRoster => adventurerRoster;
    public IReadOnlyList<DungeonStoredLootItem> RecoveredLootInventory =>
        recoveredLootInventory;
    public IReadOnlyList<PlayerLootRecoveryRecord> PlayerLootRecoveries =>
        playerLootRecoveries;
    public int RecoveredLootItemCount => recoveredLootInventory.Count;
    public int RecoveredLootValue => SumRecoveredLootValue(null);
    public int RecoveredDungeonTreasureValue =>
        SumRecoveredLootValue(RecoverableLootOrigin.DungeonTreasure);
    public int RecoveredAdventurerLootValue =>
        SumRecoveredLootValue(RecoverableLootOrigin.AdventurerPossession);
    public int RecoveredPhysicalResourceQuantity =>
        SumRecoveredPhysicalResourceQuantity(null);
    public int GetRecoveredPhysicalResourceQuantity(
        PhysicalResourceCategory category) =>
        SumRecoveredPhysicalResourceQuantity(category);

    public event Action StateChanged;
    public event Action<DreadHarvestRecord> DreadHarvested;
    public event Action<DreadSpendRecord> DreadSpent;
    public event Action<ExpeditionOutcomeRecord> ExpeditionCompleted;
    public event Action<PlayerLootRecoveryRecord> LootRecovered;

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
        recoveredLootInventory ??= new List<DungeonStoredLootItem>();
        playerLootRecoveries ??= new List<PlayerLootRecoveryRecord>();
        RebuildPlayerLootRecoveryLookup();
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
        inputManager = InputManager.Instance ?? FindAnyObjectByType<InputManager>();
        if (inputManager != null)
            inputManager.OnClicked += TryRecoverClickedWorldObject;
        SetExpansion();
        if (FindAnyObjectByType<GameplayLoopUI>() == null)
            gameObject.AddComponent<GameplayLoopUI>();
    }

    void TryRecoverClickedWorldObject()
    {
        if (Phase != DungeonPhase.Expansion || inputManager == null ||
            inputManager.IsPointerOverUI() ||
            (tilePlacement != null && tilePlacement.IsPlacementActive) ||
            !inputManager.TryGetPointerRay(out Ray pointerRay))
        {
            return;
        }

        int hitCount = Physics.RaycastNonAlloc(
            pointerRay,
            recoveryClickHits,
            500f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);
        IPlayerRecoverableWorldObject nearestTarget = null;
        float nearestDistance = float.PositiveInfinity;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            RaycastHit hit = recoveryClickHits[hitIndex];
            if (hit.collider == null || hit.distance >= nearestDistance)
                continue;

            MonoBehaviour[] candidates =
                hit.collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int candidateIndex = 0;
                 candidateIndex < candidates.Length;
                 candidateIndex++)
            {
                if (candidates[candidateIndex] is not
                    IPlayerRecoverableWorldObject candidate)
                {
                    continue;
                }

                nearestTarget = candidate;
                nearestDistance = hit.distance;
                break;
            }
        }

        if (nearestTarget != null &&
            !nearestTarget.TryRecoverByPlayer(this, out string failure) &&
            !string.IsNullOrWhiteSpace(failure))
        {
            Debug.LogWarning($"Player recovery failed: {failure}", this);
        }
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

    /// <summary>
    /// Transfers one physical recovery drop into dungeon storage during the
    /// between-expedition phase. The traversal claim and inventory credit are
    /// one main-thread transaction, keyed by the source drop ID.
    /// </summary>
    public bool TryRecoverLootDrop(
        string dropId,
        out PlayerLootRecoveryRecord recovery,
        out string failure)
    {
        recovery = null;
        failure = string.Empty;
        if (Phase != DungeonPhase.Expansion)
        {
            failure = "Physical loot can only be recovered between expeditions.";
            return false;
        }
        if (npcTraversal == null)
        {
            failure = "No dungeon recovery service is available.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(dropId))
        {
            failure = "Select a recoverable loot drop first.";
            return false;
        }
        if (playerLootRecoveriesByDropId.TryGetValue(dropId, out recovery))
        {
            failure = $"Loot drop '{dropId}' was already recovered.";
            return false;
        }
        if (!npcTraversal.TryGetRecoverableLootDrop(
                dropId, out RecoverableLootDrop availableDrop))
        {
            failure = $"Loot drop '{dropId}' is no longer available.";
            return false;
        }

        var storedItems = new List<DungeonStoredLootItem>(availableDrop.ItemCount);
        int dungeonOriginValue = 0;
        int adventurerOriginValue = 0;
        for (int i = 0; i < availableDrop.Items.Count; i++)
        {
            RecoverableLootItem item = availableDrop.Items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                continue;
            storedItems.Add(new DungeonStoredLootItem(item, dropId));
            if (item.Origin == RecoverableLootOrigin.DungeonTreasure)
                dungeonOriginValue += item.Value;
            else
                adventurerOriginValue += item.Value;
        }
        if (storedItems.Count == 0)
        {
            failure = $"Loot drop '{dropId}' contains no recoverable items.";
            return false;
        }
        if (!npcTraversal.TryClaimRecoverableLoot(
                dropId, out RecoverableLootDrop claimedDrop))
        {
            failure = $"Loot drop '{dropId}' was claimed before recovery completed.";
            return false;
        }

        recoveredLootInventory.AddRange(storedItems);
        recovery = new PlayerLootRecoveryRecord(
            claimedDrop,
            storedItems.Count,
            dungeonOriginValue + adventurerOriginValue,
            dungeonOriginValue,
            adventurerOriginValue);
        playerLootRecoveries.Add(recovery);
        playerLootRecoveriesByDropId.Add(dropId, recovery);
        LootRecovered?.Invoke(recovery);
        StateChanged?.Invoke();
        return true;
    }

    public bool HasRecoveredLootDrop(string dropId)
    {
        return !string.IsNullOrWhiteSpace(dropId) &&
            playerLootRecoveriesByDropId.ContainsKey(dropId);
    }

    public List<DungeonStoredLootItem> CaptureRecoveredLootInventory()
    {
        var result = new List<DungeonStoredLootItem>(recoveredLootInventory.Count);
        for (int i = 0; i < recoveredLootInventory.Count; i++)
            if (recoveredLootInventory[i] != null)
                result.Add(recoveredLootInventory[i].Copy());
        return result;
    }

    public List<PlayerLootRecoveryRecord> CapturePlayerLootRecoveries()
    {
        var result = new List<PlayerLootRecoveryRecord>(playerLootRecoveries.Count);
        for (int i = 0; i < playerLootRecoveries.Count; i++)
            if (playerLootRecoveries[i] != null)
                result.Add(playerLootRecoveries[i].Copy());
        return result;
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

    public GameplayLoopScenarioState CaptureScenarioState()
    {
        return new GameplayLoopScenarioState(
            dungeonOpenCount,
            dread,
            dungeonLevel,
            selectedSpeed,
            CaptureLivingAdventurers(),
            dreadHarvests,
            expeditionOutcomes,
            recoveredLootInventory,
            playerLootRecoveries,
            dreadSpends);
    }

    /// <summary>
    /// Ends any active visit before a validated scenario begins changing the
    /// dungeon. The resulting retreat records are replaced by the captured
    /// scenario baseline after the authored layout has been restored.
    /// </summary>
    public void PrepareForScenarioApply()
    {
        SetPaused(false);
        SetExpansion();
    }

    public void RestoreScenarioState(GameplayLoopScenarioState snapshot)
    {
        if (snapshot == null)
        {
            PrepareForScenarioApply();
            ClearDreadHarvestHistory();
            ClearDreadSpendHistory();
            ClearExpeditionOutcomeHistory();
            ClearRecoveredLootState();
            StateChanged?.Invoke();
            return;
        }

        RestoreProgress(
            snapshot.DungeonOpenCount,
            snapshot.SelectedSpeed,
            snapshot.Dread,
            snapshot.DungeonLevel,
            new List<NPCCharacterRecord>(snapshot.AdventurerRoster),
            snapshot.RecoveredLootInventory,
            snapshot.PlayerLootRecoveries,
            snapshot.DreadSpends);

        IReadOnlyList<DreadHarvestRecord> restoredHarvests =
            snapshot.DreadHarvests;
        for (int i = 0; i < restoredHarvests.Count; i++)
        {
            DreadHarvestRecord harvest = restoredHarvests[i]?.Copy();
            if (harvest == null)
                continue;
            dreadHarvests.Add(harvest);
            dreadHarvestsById.Add(harvest.HarvestId, harvest);
        }

        IReadOnlyList<ExpeditionOutcomeRecord> restoredOutcomes =
            snapshot.ExpeditionOutcomes;
        for (int i = 0; i < restoredOutcomes.Count; i++)
        {
            ExpeditionOutcomeRecord outcome = restoredOutcomes[i]?.Copy();
            if (outcome == null)
                continue;
            expeditionOutcomes.Add(outcome);
            expeditionOutcomesById.Add(outcome.ExpeditionId, outcome);
        }
        StateChanged?.Invoke();
    }

    public bool TryValidateScenarioState(
        GameplayLoopScenarioState snapshot,
        out string failure)
    {
        failure = string.Empty;
        if (snapshot == null)
            return true;

        var rosterIds = new HashSet<string>();
        IReadOnlyList<NPCCharacterRecord> roster = snapshot.AdventurerRoster;
        for (int i = 0; i < roster.Count; i++)
        {
            NPCCharacterRecord record = roster[i];
            if (record == null || string.IsNullOrWhiteSpace(record.id) ||
                !rosterIds.Add(record.id))
            {
                failure = $"Adventurer roster record {i + 1} has a missing or duplicate ID.";
                return false;
            }

            IReadOnlyList<AdventurerResourcePayload> resources =
                record.startingResources;
            if (resources == null)
                continue;
            for (int resourceIndex = 0;
                 resourceIndex < resources.Count;
                 resourceIndex++)
            {
                if (resources[resourceIndex] == null ||
                    !resources[resourceIndex].IsValid)
                {
                    failure = $"Adventurer roster record {i + 1} has an invalid starting resource payload.";
                    return false;
                }
            }
        }

        var harvestIds = new HashSet<string>();
        IReadOnlyList<DreadHarvestRecord> harvests = snapshot.DreadHarvests;
        for (int i = 0; i < harvests.Count; i++)
        {
            DreadHarvestRecord harvest = harvests[i];
            if (harvest == null || string.IsNullOrWhiteSpace(harvest.HarvestId) ||
                !harvestIds.Add(harvest.HarvestId))
            {
                failure = $"Dread harvest record {i + 1} has a missing or duplicate harvest ID.";
                return false;
            }
            if (harvest.DungeonOpenCount > snapshot.DungeonOpenCount)
            {
                failure = $"Dread harvest '{harvest.HarvestId}' belongs to a later dungeon opening than the captured baseline.";
                return false;
            }
        }

        var spendIds = new HashSet<string>();
        IReadOnlyList<DreadSpendRecord> spends = snapshot.DreadSpends;
        for (int i = 0; i < spends.Count; i++)
        {
            DreadSpendRecord spend = spends[i];
            if (spend == null || string.IsNullOrWhiteSpace(spend.SpendId) ||
                spend.Amount <= 0 || !spendIds.Add(spend.SpendId))
            {
                failure = $"Dread spend record {i + 1} has invalid contents or a missing/duplicate spend ID.";
                return false;
            }
            if (spend.DungeonOpenCount > snapshot.DungeonOpenCount)
            {
                failure = $"Dread spend '{spend.SpendId}' belongs to a later dungeon opening than the captured baseline.";
                return false;
            }
        }

        var expeditionIds = new HashSet<string>();
        IReadOnlyList<ExpeditionOutcomeRecord> outcomes =
            snapshot.ExpeditionOutcomes;
        for (int i = 0; i < outcomes.Count; i++)
        {
            ExpeditionOutcomeRecord outcome = outcomes[i];
            if (outcome == null ||
                string.IsNullOrWhiteSpace(outcome.ExpeditionId) ||
                !expeditionIds.Add(outcome.ExpeditionId))
            {
                failure = $"Expedition outcome record {i + 1} has a missing or duplicate expedition ID.";
                return false;
            }
            if (outcome.DungeonOpenCount > snapshot.DungeonOpenCount)
            {
                failure = $"Expedition outcome '{outcome.ExpeditionId}' belongs to a later dungeon opening than the captured baseline.";
                return false;
            }
        }

        var recoveryDropIds = new HashSet<string>();
        IReadOnlyList<PlayerLootRecoveryRecord> recoveries =
            snapshot.PlayerLootRecoveries;
        for (int i = 0; i < recoveries.Count; i++)
        {
            PlayerLootRecoveryRecord recovery = recoveries[i];
            if (recovery == null ||
                string.IsNullOrWhiteSpace(recovery.SourceDropId) ||
                recovery.RecoveredItemCount <= 0 ||
                !recoveryDropIds.Add(recovery.SourceDropId))
            {
                failure = $"Player recovery record {i + 1} has invalid contents or a missing/duplicate drop ID.";
                return false;
            }
        }

        IReadOnlyList<DungeonStoredLootItem> storedLoot =
            snapshot.RecoveredLootInventory;
        for (int i = 0; i < storedLoot.Count; i++)
        {
            DungeonStoredLootItem item = storedLoot[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId) ||
                string.IsNullOrWhiteSpace(item.RecoveryDropId) ||
                !recoveryDropIds.Contains(item.RecoveryDropId))
            {
                failure = $"Recovered inventory item {i + 1} has invalid identity or recovery provenance.";
                return false;
            }
        }

        for (int recoveryIndex = 0;
             recoveryIndex < recoveries.Count;
             recoveryIndex++)
        {
            PlayerLootRecoveryRecord recovery = recoveries[recoveryIndex];
            int itemCount = 0;
            int totalValue = 0;
            int dungeonValue = 0;
            int adventurerValue = 0;
            for (int itemIndex = 0; itemIndex < storedLoot.Count; itemIndex++)
            {
                DungeonStoredLootItem item = storedLoot[itemIndex];
                if (item.RecoveryDropId != recovery.SourceDropId)
                    continue;
                itemCount++;
                totalValue += item.Value;
                if (item.Origin == RecoverableLootOrigin.DungeonTreasure)
                    dungeonValue += item.Value;
                else
                    adventurerValue += item.Value;
            }
            if (itemCount != recovery.RecoveredItemCount ||
                totalValue != recovery.RecoveredValue ||
                dungeonValue != recovery.DungeonTreasureValue ||
                adventurerValue != recovery.AdventurerLootValue)
            {
                failure = $"Player recovery '{recovery.SourceDropId}' does not match the captured inventory contents.";
                return false;
            }
        }
        return true;
    }

    public void RestoreProgress(
        int savedDungeonOpenCount,
        float savedGameplaySpeed,
        int savedDread,
        int savedDungeonLevel,
        List<NPCCharacterRecord> livingAdventurers,
        IReadOnlyList<DungeonStoredLootItem> storedLoot = null,
        IReadOnlyList<PlayerLootRecoveryRecord> recoveries = null,
        IReadOnlyList<DreadSpendRecord> spends = null)
    {
        SetPaused(false);
        SetExpansion();
        dungeonOpenCount = Mathf.Max(0, savedDungeonOpenCount);
        dread = Mathf.Max(0, savedDread);
        dungeonLevel = Mathf.Max(1, savedDungeonLevel);
        ClearDreadHarvestHistory();
        RestoreDreadSpendHistory(spends);
        ClearExpeditionOutcomeHistory();
        RestoreRecoveredLootState(storedLoot, recoveries);
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
            dungeonVisits = source.dungeonVisits,
            startingResources = AdventurerResourcePayload.CopyAll(
                source.startingResources)
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
            activeVisitors[spawned.Character] = spawned;
            pendingVisitDread[spawned.Character] = 0;
            spawned.Character.Damaged += OnAdventurerDamaged;
            spawned.DungeonVisitCompleted += OnDungeonVisitCompleted;
            visitorsScheduled++;
            if (!spawned.BeginDungeonVisit())
            {
                spawned.Character.Damaged -= OnAdventurerDamaged;
                spawned.DungeonVisitCompleted -= OnDungeonVisitCompleted;
                activeRecords.Remove(spawned.Character);
                activeVisitors.Remove(spawned.Character);
                pendingVisitDread.Remove(spawned.Character);
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
            NPCCharacterRecord generated = AdventurerNameGenerator.Create(
                names,
                adventurerRoster.Count);
            adventurerRoster.Add(generated);
            names.Add(generated.characterName);
        }

        roundVisitors.Clear();
        for (int i = 0; i < Mathf.Min(MaximumAdventurers, adventurerRoster.Count); i++)
            roundVisitors.Add(adventurerRoster[i]);
    }

    void OnDungeonVisitCompleted(NPCTraversalAgent visitor)
    {
        TryCompleteExpedition(
            visitor,
            ExpeditionOutcomeType.SuccessfulEscape,
            null);
    }

    void OnAdventurerDefeated(NPCTraversalAgent visitor)
    {
        NPCCharacter character = visitor != null ? visitor.Character : null;
        NPCCharacterRecord record = null;
        bool hasActiveRecord = character != null &&
            activeRecords.TryGetValue(character, out record);

        DreadHarvestRecord deathHarvest = null;
        if (visitor != null && character != null && visitor.DiedDuringDungeonVisit)
        {
            string harvestId =
                $"dread:death:opening-{dungeonOpenCount}:agent-{visitor.RuntimeAgentId}";
            TryHarvestDread(
                new DreadHarvestRequest(
                    harvestId,
                    DreadHarvestSource.AdventurerDeath,
                    GetDeathDreadHarvestAmount(character.Level),
                    hasActiveRecord ? record.id : string.Empty,
                    character.CharacterName,
                    visitor.RuntimeAgentId,
                    character.Level,
                    dungeonOpenCount,
                    visitor.CurrentCell,
                    visitor.transform.position),
                out deathHarvest);
        }

        if (visitor != null && character != null && visitor.DiedDuringDungeonVisit)
        {
            TryCompleteExpedition(
                visitor,
                ExpeditionOutcomeType.Defeated,
                deathHarvest);
            return;
        }

        if (hasActiveRecord)
            CleanupActiveVisitor(visitor, character, record, false);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Finalizes one visit after its outcome-specific loot and Dread consequences
    /// have run. The stable expedition ID makes every completion path idempotent.
    /// </summary>
    bool TryCompleteExpedition(
        NPCTraversalAgent visitor,
        ExpeditionOutcomeType outcome,
        DreadHarvestRecord dreadHarvest)
    {
        NPCCharacter character = visitor != null ? visitor.Character : null;
        if (visitor == null || character == null)
            return false;

        string expeditionId =
            $"expedition:opening-{dungeonOpenCount}:agent-{visitor.RuntimeAgentId}";
        if (expeditionOutcomesById.TryGetValue(
            expeditionId,
            out ExpeditionOutcomeRecord existing))
        {
            existing.RecordDuplicateCompletionAttempt();
            StateChanged?.Invoke();
            return false;
        }

        activeRecords.TryGetValue(character, out NPCCharacterRecord characterRecord);
        int carriedItemCount = visitor.CarriedDungeonTreasureCount;
        int carriedValue = visitor.CarriedDungeonTreasureValue;
        int lostItemCount = 0;
        int lostValue = 0;
        int recoveredItemCount = 0;
        int recoveredValue = 0;
        string recoveryDropId = string.Empty;

        if (outcome == ExpeditionOutcomeType.SuccessfulEscape)
        {
            AdventurerEscapeLootOutcome escape =
                FindSuccessfulEscapeLootOutcome(visitor.RuntimeAgentId);
            if (escape != null)
            {
                carriedItemCount = escape.CarriedItemCountBefore;
                carriedValue = escape.CarriedValueBefore;
                lostItemCount = escape.EscapedItemCount;
                lostValue = escape.EscapedValue;
            }
        }
        else if (outcome == ExpeditionOutcomeType.Defeated)
        {
            AdventurerDeathLootOutcome defeat =
                FindDeathLootOutcome(visitor.RuntimeAgentId);
            if (defeat != null)
            {
                carriedItemCount = defeat.CarriedItemCountBefore;
                carriedValue = defeat.CarriedValueBefore;
                recoveredItemCount = defeat.RecoveredItemCount;
                recoveredValue = defeat.RecoveredValue;
                recoveryDropId = defeat.RecoveryDropId;
            }
        }
        else
        {
            // Phase/session cleanup removes this visitor from the dungeon. Any
            // treasure still in its custody remains resolved and leaves with it.
            if (!visitor.TryFinalizeForcedRetreat())
                return false;
            lostItemCount = carriedItemCount;
            lostValue = carriedValue;
        }

        if (outcome != ExpeditionOutcomeType.SuccessfulEscape)
            character.RecordDungeonVisitCompleted();

        int visitDreadSettled = SettleVisitDread(character);
        if (characterRecord != null)
            character.WriteToRecord(characterRecord);

        var completed = new ExpeditionOutcomeRecord(
            new ExpeditionOutcomeRequest(
                expeditionId,
                outcome,
                characterRecord != null ? characterRecord.id : string.Empty,
                character.CharacterName,
                visitor.RuntimeAgentId,
                character.Level,
                dungeonOpenCount,
                visitor.StartCell,
                visitor.CurrentCell,
                visitor.transform.position,
                visitor.VisitedCells.Count,
                carriedItemCount,
                carriedValue,
                lostItemCount,
                lostValue,
                recoveredItemCount,
                recoveredValue,
                recoveryDropId,
                dreadHarvest != null ? dreadHarvest.Amount : 0,
                visitDreadSettled,
                dreadHarvest != null ? dreadHarvest.HarvestId : string.Empty));

        expeditionOutcomes.Add(completed);
        expeditionOutcomesById.Add(completed.ExpeditionId, completed);
        CleanupActiveVisitor(visitor, character, characterRecord, false);
        ExpeditionCompleted?.Invoke(completed);
        StateChanged?.Invoke();
        return true;
    }

    AdventurerDeathLootOutcome FindDeathLootOutcome(int runtimeAgentId)
    {
        if (npcTraversal == null)
            return null;

        IReadOnlyList<AdventurerDeathLootOutcome> outcomes =
            npcTraversal.DeathLootOutcomes;
        for (int i = outcomes.Count - 1; i >= 0; i--)
        {
            AdventurerDeathLootOutcome candidate = outcomes[i];
            if (candidate != null && candidate.SourceRuntimeAgentId == runtimeAgentId)
                return candidate;
        }
        return null;
    }

    AdventurerEscapeLootOutcome FindSuccessfulEscapeLootOutcome(int runtimeAgentId)
    {
        if (npcTraversal == null)
            return null;

        IReadOnlyList<AdventurerEscapeLootOutcome> outcomes =
            npcTraversal.SuccessfulEscapeLootOutcomes;
        for (int i = outcomes.Count - 1; i >= 0; i--)
        {
            AdventurerEscapeLootOutcome candidate = outcomes[i];
            if (candidate != null && candidate.SourceRuntimeAgentId == runtimeAgentId)
                return candidate;
        }
        return null;
    }

    void CleanupActiveVisitor(
        NPCTraversalAgent visitor,
        NPCCharacter character,
        NPCCharacterRecord characterRecord,
        bool settleDread)
    {
        if (character == null)
            return;

        if (characterRecord != null)
            character.WriteToRecord(characterRecord);
        character.Damaged -= OnAdventurerDamaged;
        if (visitor != null)
            visitor.DungeonVisitCompleted -= OnDungeonVisitCompleted;
        if (settleDread)
            SettleVisitDread(character);
        activeRecords.Remove(character);
        activeVisitors.Remove(character);
    }

    /// <summary>
    /// Credits a Dread harvest exactly once. Callers provide a stable harvest
    /// ID plus source context; the currency mutation remains owned here.
    /// </summary>
    public bool TryHarvestDread(
        DreadHarvestRequest request,
        out DreadHarvestRecord harvest)
    {
        harvest = null;
        if (string.IsNullOrWhiteSpace(request.HarvestId) || request.Amount <= 0)
            return false;

        if (dreadHarvestsById.TryGetValue(request.HarvestId, out harvest))
        {
            harvest.RecordDuplicateAttempt();
            StateChanged?.Invoke();
            return false;
        }

        harvest = new DreadHarvestRecord(request);
        dreadHarvests.Add(harvest);
        dreadHarvestsById.Add(harvest.HarvestId, harvest);
        dread += harvest.Amount;
        DreadHarvested?.Invoke(harvest);
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Applies one validated growth effect and debits Dread only if that
    /// production operation succeeds. Duplicate IDs never reapply the effect.
    /// </summary>
    public bool TrySpendDread(
        DreadSpendRequest request,
        Func<bool> applyGrowth,
        out DreadSpendRecord spend,
        out string failure)
    {
        spend = null;
        failure = string.Empty;
        if (string.IsNullOrWhiteSpace(request.SpendId) || request.Amount <= 0)
        {
            failure = "A Dread purchase needs a stable ID and positive cost.";
            return false;
        }
        if (dreadSpendsById.TryGetValue(request.SpendId, out spend))
        {
            spend.RecordDuplicateAttempt();
            failure = $"Dread spend '{request.SpendId}' was already completed.";
            StateChanged?.Invoke();
            return false;
        }
        if (dread < request.Amount)
        {
            failure = $"The purchase needs {request.Amount} Dread; only {dread} is available.";
            return false;
        }
        if (applyGrowth == null || !applyGrowth.Invoke())
        {
            failure = "The dungeon growth effect could not be applied; no Dread was spent.";
            return false;
        }

        dread -= request.Amount;
        spend = new DreadSpendRecord(request);
        dreadSpends.Add(spend);
        dreadSpendsById.Add(spend.SpendId, spend);
        DreadSpent?.Invoke(spend);
        StateChanged?.Invoke();
        return true;
    }

    public List<DreadSpendRecord> CaptureDreadSpends()
    {
        var result = new List<DreadSpendRecord>(dreadSpends.Count);
        for (int i = 0; i < dreadSpends.Count; i++)
            if (dreadSpends[i] != null)
                result.Add(dreadSpends[i].Copy());
        return result;
    }

    void ClearDreadHarvestHistory()
    {
        dreadHarvests.Clear();
        dreadHarvestsById.Clear();
    }

    void RestoreDreadSpendHistory(IReadOnlyList<DreadSpendRecord> spends)
    {
        ClearDreadSpendHistory();
        if (spends == null)
            return;
        for (int i = 0; i < spends.Count; i++)
        {
            DreadSpendRecord spend = spends[i]?.Copy();
            if (spend == null || string.IsNullOrWhiteSpace(spend.SpendId) ||
                dreadSpendsById.ContainsKey(spend.SpendId))
            {
                continue;
            }
            dreadSpends.Add(spend);
            dreadSpendsById.Add(spend.SpendId, spend);
        }
    }

    void ClearDreadSpendHistory()
    {
        dreadSpends.Clear();
        dreadSpendsById.Clear();
    }

    void ClearExpeditionOutcomeHistory()
    {
        expeditionOutcomes.Clear();
        expeditionOutcomesById.Clear();
    }

    void RestoreRecoveredLootState(
        IReadOnlyList<DungeonStoredLootItem> storedLoot,
        IReadOnlyList<PlayerLootRecoveryRecord> recoveries)
    {
        ClearRecoveredLootState();
        if (recoveries != null)
        {
            for (int i = 0; i < recoveries.Count; i++)
                if (recoveries[i] != null &&
                    !string.IsNullOrWhiteSpace(recoveries[i].SourceDropId) &&
                    !playerLootRecoveriesByDropId.ContainsKey(
                        recoveries[i].SourceDropId))
                {
                    PlayerLootRecoveryRecord copy = recoveries[i].Copy();
                    playerLootRecoveries.Add(copy);
                    playerLootRecoveriesByDropId.Add(copy.SourceDropId, copy);
                }
        }

        if (storedLoot == null)
            return;
        for (int i = 0; i < storedLoot.Count; i++)
            if (storedLoot[i] != null &&
                playerLootRecoveriesByDropId.ContainsKey(
                    storedLoot[i].RecoveryDropId))
            {
                recoveredLootInventory.Add(storedLoot[i].Copy());
            }
    }

    void ClearRecoveredLootState()
    {
        recoveredLootInventory.Clear();
        playerLootRecoveries.Clear();
        playerLootRecoveriesByDropId.Clear();
    }

    void RebuildPlayerLootRecoveryLookup()
    {
        playerLootRecoveriesByDropId.Clear();
        for (int i = 0; i < playerLootRecoveries.Count; i++)
        {
            PlayerLootRecoveryRecord recovery = playerLootRecoveries[i];
            if (recovery != null &&
                !string.IsNullOrWhiteSpace(recovery.SourceDropId) &&
                !playerLootRecoveriesByDropId.ContainsKey(recovery.SourceDropId))
            {
                playerLootRecoveriesByDropId.Add(
                    recovery.SourceDropId, recovery);
            }
        }
    }

    int SumRecoveredLootValue(RecoverableLootOrigin? origin)
    {
        int total = 0;
        for (int i = 0; i < recoveredLootInventory.Count; i++)
        {
            DungeonStoredLootItem item = recoveredLootInventory[i];
            if (item != null && (!origin.HasValue || item.Origin == origin.Value))
                total += item.Value;
        }
        return total;
    }

    int SumRecoveredPhysicalResourceQuantity(
        PhysicalResourceCategory? category)
    {
        int total = 0;
        for (int i = 0; i < recoveredLootInventory.Count; i++)
        {
            DungeonStoredLootItem item = recoveredLootInventory[i];
            if (item != null && item.IsPhysicalResource &&
                (!category.HasValue || item.ResourceCategory == category.Value))
            {
                total += item.ResourceQuantity;
            }
        }
        return total;
    }

    void OnAdventurerCellEntered(
        NPCTraversalAgent visitor,
        Vector2Int cell,
        bool firstVisit)
    {
        if (visitor == null || !firstVisit)
            return;
        AddPendingDread(visitor.Character, dreadPerNewCell);
    }

    void OnAdventurerDamaged(NPCCharacter character, int appliedDamage)
    {
        AddPendingDread(character, appliedDamage * dreadPerDamage);
    }

    void AddPendingDread(NPCCharacter character, int amount)
    {
        if (character == null || amount <= 0 || !pendingVisitDread.ContainsKey(character))
            return;
        pendingVisitDread[character] += amount;
    }

    int SettleVisitDread(NPCCharacter character)
    {
        if (character == null || !pendingVisitDread.TryGetValue(character, out int amount))
            return 0;
        int settledAmount = Mathf.Max(0, amount);
        dread += settledAmount;
        pendingVisitDread.Remove(character);
        return settledAmount;
    }

    public int GetDeathDreadHarvestAmount(int adventurerLevel)
    {
        return Mathf.Max(0, Mathf.RoundToInt(
            baseDefeatDread * Mathf.Pow(Mathf.Max(1, adventurerLevel), defeatLevelExponent)));
    }

    public bool BeginTreasureManifestation(int objectId)
    {
        if (Phase != DungeonPhase.Expansion)
            return ReportDreadFailure("Treasure can only be manifested between expeditions.");
        int cost = TreasureManifestationDreadCost;
        if (dread < cost)
            return ReportDreadFailure(
                $"Treasure manifestation needs {cost} Dread; " +
                $"only {dread} is available.");
        if (!TryResolveTreasureDefinition(
                objectId, out ObjectData treasure, out string failure))
            return ReportDreadFailure(failure);

        if (!tilePlacement.TryStartPlacement(
                treasure.ID,
                cell => TryManifestTreasureAtCell(cell, treasure),
                out failure))
        {
            return ReportDreadFailure(failure);
        }
        LastDreadActionMessage =
            $"Choose a valid cell to manifest treasure for " +
            $"{cost} Dread.";
        StateChanged?.Invoke();
        return true;
    }

    bool TryManifestTreasureAtCell(Vector2Int cell, ObjectData treasure)
    {
        string failure;
        if (!tileGrid.TryValidateFloorPropPlacement(
                cell, treasure.Prefab, out failure))
        {
            return ReportDreadFailure(failure);
        }

        var request = new DreadSpendRequest(
            $"dread:treasure:{Guid.NewGuid():N}",
            DreadSpendPurpose.TreasureManifestation,
            TreasureManifestationDreadCost,
            dungeonOpenCount,
            cell,
            treasure.ID,
            treasure.Prefab.name);
        bool accepted = TrySpendDread(
            request,
            () => tileGrid.PlaceFloorPropCell(
                cell.x,
                cell.y,
                treasure.Prefab,
                treasure.ID),
            out _,
            out failure);
        if (!accepted)
            return ReportDreadFailure(failure);

        LastDreadActionMessage =
            $"Manifested treasure at {cell} for " +
            $"{TreasureManifestationDreadCost} Dread.";
        tilePlacement.StopPlacement();
        StateChanged?.Invoke();
        return true;
    }

    bool TryResolveTreasureDefinition(
        int objectId,
        out ObjectData treasure,
        out string failure)
    {
        treasure = null;
        failure = string.Empty;
        if (tilePlacement == null || tileGrid == null)
        {
            failure = "Treasure manifestation is unavailable in this dungeon.";
            return false;
        }

        IReadOnlyList<ObjectData> objects = tilePlacement.AvailableObjects;
        for (int i = 0; i < objects.Count; i++)
        {
            ObjectData candidate = objects[i];
            if (candidate != null &&
                candidate.ID == objectId &&
                candidate.PlacementType == ObjectPlacementType.FloorProp &&
                candidate.Prefab != null &&
                candidate.Prefab.GetComponent<TreasureProp>() != null)
            {
                treasure = candidate;
                return true;
            }
        }

        failure = $"Object {objectId} is not an available TreasureProp floor prop.";
        return false;
    }

    bool ReportDreadFailure(string failure)
    {
        LastDreadActionMessage = string.IsNullOrWhiteSpace(failure)
            ? "The Dread purchase could not be completed."
            : failure;
        StateChanged?.Invoke();
        return false;
    }

    void StoreActiveAdventurers()
    {
        foreach (KeyValuePair<NPCCharacter, NPCCharacterRecord> pair in activeRecords)
            if (pair.Key != null && !pair.Key.IsDead)
                pair.Key.WriteToRecord(pair.Value);
    }

    void ReturnAllActiveAdventurersOutside()
    {
        if (activeVisitors.Count > 0)
        {
            var visitors = new List<NPCTraversalAgent>(activeVisitors.Values);
            for (int i = 0; i < visitors.Count; i++)
                if (visitors[i] != null)
                    TryCompleteExpedition(
                        visitors[i],
                        ExpeditionOutcomeType.Retreated,
                        null);
        }

        if (activeRecords.Count == 0)
        {
            pendingVisitDread.Clear();
            activeVisitors.Clear();
            return;
        }

        // Defensive cleanup for a character whose traversal agent was removed
        // externally before the gameplay loop could observe a visit outcome.
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
            CleanupActiveVisitor(null, character, record, true);
        }
        activeRecords.Clear();
        activeVisitors.Clear();
        pendingVisitDread.Clear();
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        DungeonSimulationState.PauseChanged -= OnSimulationPauseChanged;
        if (inputManager != null)
            inputManager.OnClicked -= TryRecoverClickedWorldObject;
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
