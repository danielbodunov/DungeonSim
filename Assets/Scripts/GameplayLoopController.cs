using System;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField, Min(0)] int adventurerAura;
    [SerializeField, Min(1)] int dungeonLevel = 1;
    [SerializeField, Range(0.1f, 10f)] float selectedSpeed = 1f;
    [SerializeField] List<NPCCharacterRecord> adventurerRoster = new();
    [SerializeField] List<AuraHarvestRecord> auraHarvests = new();
    [SerializeField] List<ExpeditionOutcomeRecord> expeditionOutcomes = new();
    [SerializeField] List<DungeonStoredLootItem> recoveredLootInventory = new();
    [SerializeField] List<PlayerLootRecoveryRecord> playerLootRecoveries = new();

    public int DungeonOpenCount => Mathf.Max(0, dungeonOpenCount);
    public int AdventurerAura => Mathf.Max(0, adventurerAura);
    public int DungeonLevel => Mathf.Max(1, dungeonLevel);
    public float SelectedSpeed => Mathf.Clamp(selectedSpeed, 0.1f, 10f);
    public IReadOnlyList<NPCCharacterRecord> AdventurerRoster =>
        adventurerRoster ??
        (IReadOnlyList<NPCCharacterRecord>)Array.Empty<NPCCharacterRecord>();
    public IReadOnlyList<AuraHarvestRecord> AuraHarvests =>
        auraHarvests ??
        (IReadOnlyList<AuraHarvestRecord>)Array.Empty<AuraHarvestRecord>();
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
        int aura,
        int level,
        float gameplaySpeed,
        IReadOnlyList<NPCCharacterRecord> roster,
        IReadOnlyList<AuraHarvestRecord> harvests,
        IReadOnlyList<ExpeditionOutcomeRecord> outcomes,
        IReadOnlyList<DungeonStoredLootItem> storedLoot = null,
        IReadOnlyList<PlayerLootRecoveryRecord> recoveries = null)
    {
        dungeonOpenCount = Mathf.Max(0, openCount);
        adventurerAura = Mathf.Max(0, aura);
        dungeonLevel = Mathf.Max(1, level);
        selectedSpeed = Mathf.Clamp(gameplaySpeed, 0.1f, 10f);
        adventurerRoster = CopyRoster(roster);
        auraHarvests = CopyHarvests(harvests);
        expeditionOutcomes = CopyOutcomes(outcomes);
        recoveredLootInventory = CopyStoredLoot(storedLoot);
        playerLootRecoveries = CopyRecoveries(recoveries);
    }

    internal GameplayLoopScenarioState Copy()
    {
        return new GameplayLoopScenarioState(
            DungeonOpenCount,
            AdventurerAura,
            DungeonLevel,
            SelectedSpeed,
            AdventurerRoster,
            AuraHarvests,
            ExpeditionOutcomes,
            RecoveredLootInventory,
            PlayerLootRecoveries);
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
                dungeonVisits = record.dungeonVisits
            });
        }
        return result;
    }

    static List<AuraHarvestRecord> CopyHarvests(
        IReadOnlyList<AuraHarvestRecord> source)
    {
        var result = new List<AuraHarvestRecord>(source?.Count ?? 0);
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
    readonly Dictionary<NPCCharacter, NPCTraversalAgent> activeVisitors = new();
    readonly Dictionary<NPCCharacter, int> pendingVisitAura = new();
    readonly List<AuraHarvestRecord> auraHarvests = new();
    readonly Dictionary<string, AuraHarvestRecord> auraHarvestsById = new();
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
    public int AdventurerAura => adventurerAura;
    public IReadOnlyList<AuraHarvestRecord> AuraHarvests => auraHarvests;
    public int AuraHarvestCount => auraHarvests.Count;
    public IReadOnlyList<ExpeditionOutcomeRecord> ExpeditionOutcomes =>
        expeditionOutcomes;
    public int ExpeditionOutcomeCount => expeditionOutcomes.Count;
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

    public event Action StateChanged;
    public event Action<AuraHarvestRecord> AuraHarvested;
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
            adventurerAura,
            dungeonLevel,
            selectedSpeed,
            CaptureLivingAdventurers(),
            auraHarvests,
            expeditionOutcomes,
            recoveredLootInventory,
            playerLootRecoveries);
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
            ClearAuraHarvestHistory();
            ClearExpeditionOutcomeHistory();
            ClearRecoveredLootState();
            StateChanged?.Invoke();
            return;
        }

        RestoreProgress(
            snapshot.DungeonOpenCount,
            snapshot.SelectedSpeed,
            snapshot.AdventurerAura,
            snapshot.DungeonLevel,
            new List<NPCCharacterRecord>(snapshot.AdventurerRoster),
            snapshot.RecoveredLootInventory,
            snapshot.PlayerLootRecoveries);

        IReadOnlyList<AuraHarvestRecord> restoredHarvests =
            snapshot.AuraHarvests;
        for (int i = 0; i < restoredHarvests.Count; i++)
        {
            AuraHarvestRecord harvest = restoredHarvests[i]?.Copy();
            if (harvest == null)
                continue;
            auraHarvests.Add(harvest);
            auraHarvestsById.Add(harvest.HarvestId, harvest);
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
        }

        var harvestIds = new HashSet<string>();
        IReadOnlyList<AuraHarvestRecord> harvests = snapshot.AuraHarvests;
        for (int i = 0; i < harvests.Count; i++)
        {
            AuraHarvestRecord harvest = harvests[i];
            if (harvest == null || string.IsNullOrWhiteSpace(harvest.HarvestId) ||
                !harvestIds.Add(harvest.HarvestId))
            {
                failure = $"Aura harvest record {i + 1} has a missing or duplicate harvest ID.";
                return false;
            }
            if (harvest.DungeonOpenCount > snapshot.DungeonOpenCount)
            {
                failure = $"Aura harvest '{harvest.HarvestId}' belongs to a later dungeon opening than the captured baseline.";
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
        int savedAdventurerAura,
        int savedDungeonLevel,
        List<NPCCharacterRecord> livingAdventurers,
        IReadOnlyList<DungeonStoredLootItem> storedLoot = null,
        IReadOnlyList<PlayerLootRecoveryRecord> recoveries = null)
    {
        SetPaused(false);
        SetExpansion();
        dungeonOpenCount = Mathf.Max(0, savedDungeonOpenCount);
        adventurerAura = Mathf.Max(0, savedAdventurerAura);
        dungeonLevel = Mathf.Max(1, savedDungeonLevel);
        ClearAuraHarvestHistory();
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
            activeVisitors[spawned.Character] = spawned;
            pendingVisitAura[spawned.Character] = 0;
            spawned.Character.Damaged += OnAdventurerDamaged;
            spawned.DungeonVisitCompleted += OnDungeonVisitCompleted;
            visitorsScheduled++;
            if (!spawned.BeginDungeonVisit())
            {
                spawned.Character.Damaged -= OnAdventurerDamaged;
                spawned.DungeonVisitCompleted -= OnDungeonVisitCompleted;
                activeRecords.Remove(spawned.Character);
                activeVisitors.Remove(spawned.Character);
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

        AuraHarvestRecord deathHarvest = null;
        if (visitor != null && character != null && visitor.DiedDuringDungeonVisit)
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
    /// Finalizes one visit after its outcome-specific loot and Aura consequences
    /// have run. The stable expedition ID makes every completion path idempotent.
    /// </summary>
    bool TryCompleteExpedition(
        NPCTraversalAgent visitor,
        ExpeditionOutcomeType outcome,
        AuraHarvestRecord auraHarvest)
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

        int visitAuraSettled = SettleVisitAura(character);
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
                auraHarvest != null ? auraHarvest.Amount : 0,
                visitAuraSettled,
                auraHarvest != null ? auraHarvest.HarvestId : string.Empty));

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
        bool settleAura)
    {
        if (character == null)
            return;

        if (characterRecord != null)
            character.WriteToRecord(characterRecord);
        character.Damaged -= OnAdventurerDamaged;
        if (visitor != null)
            visitor.DungeonVisitCompleted -= OnDungeonVisitCompleted;
        if (settleAura)
            SettleVisitAura(character);
        activeRecords.Remove(character);
        activeVisitors.Remove(character);
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

    int SettleVisitAura(NPCCharacter character)
    {
        if (character == null || !pendingVisitAura.TryGetValue(character, out int amount))
            return 0;
        int settledAmount = Mathf.Max(0, amount);
        adventurerAura += settledAmount;
        pendingVisitAura.Remove(character);
        return settledAmount;
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
            pendingVisitAura.Clear();
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
        pendingVisitAura.Clear();
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
