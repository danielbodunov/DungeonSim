using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public sealed class NPCRuntimeDebugHarnessWindow : EditorWindow
{
    const string WindowTitle = "NPC Runtime Debug";
    const int HighlightRingSegments = 32;

    NPCTraversalAgent selectedAgent;
    bool selectionMode;
    bool showKnownCells;
    bool showKnownConnections;
    bool showCarriedTreasure = true;
    bool showRecoverableLoot = true;
    bool showDeathLootOutcomes = true;
    bool showSuccessfulEscapeLootOutcomes = true;
    bool showDreadHarvests = true;
    bool showDreadSpends = true;
    bool showExpeditionOutcomes = true;
    int damageAmount = 1;
    int healAmount = 1;
    float staminaAmount = 1f;
    int exactHealth;
    float exactStamina;
    Vector2 scroll;
    string lastActionMessage;
    double nextRepaintTime;
    InputManager subscribedInputManager;
    CameraFollow focusedCamera;

    GameObject highlightRoot;
    Material highlightMaterial;
    readonly List<LineRenderer> highlightLines = new();

    [MenuItem("Tools/NPC Runtime Debug Harness")]
    public static void ShowWindow()
    {
        GetWindow<NPCRuntimeDebugHarnessWindow>(WindowTitle);
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        selectionMode = false;
        ReleaseInputSubscription();
        ReleaseCameraFocus();
        DestroyHighlight();
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode &&
            state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        selectionMode = false;
        selectedAgent = null;
        lastActionMessage = null;
        ReleaseInputSubscription();
        ReleaseCameraFocus();
        DestroyHighlight();
        Repaint();
    }

    void OnEditorUpdate()
    {
        if (!EditorApplication.isPlaying)
        {
            ReleaseInputSubscription();
            DestroyHighlight();
            return;
        }

        UpdateInputSubscription();

        if (selectedAgent == null && focusedCamera != null)
            ReleaseCameraFocus();

        if (selectionMode && selectedAgent != null)
            UpdateHighlight();
        else
            DestroyHighlight();

        if (EditorApplication.timeSinceStartup >= nextRepaintTime)
        {
            nextRepaintTime = EditorApplication.timeSinceStartup + 0.1d;
            Repaint();
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("NPC Runtime Debug Harness", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Play Mode only. Enable selection mode, then left-click an NPC in Game View. " +
            "Selection focuses the gameplay camera. Wheel zoom remains available while " +
            "following; keyboard or middle-mouse pan releases camera follow without clearing " +
            "the selected NPC. When disabled, the harness does not listen " +
            "for gameplay clicks or draw a highlight.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            bool enabled = EditorGUILayout.ToggleLeft(
                "Enable Game View NPC Selection", selectionMode);
            if (enabled != selectionMode)
                SetSelectionMode(enabled);
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Hierarchy Selection",
                Selection.activeGameObject,
                typeof(GameObject),
                true);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Use Hierarchy Selection"))
                    TryUseHierarchySelection();
            }

            using (new EditorGUI.DisabledScope(selectedAgent == null))
            {
                if (GUILayout.Button("Select In Hierarchy"))
                {
                    Selection.activeGameObject = selectedAgent.gameObject;
                    EditorGUIUtility.PingObject(selectedAgent.gameObject);
                }

                if (GUILayout.Button("Clear"))
                    SelectAgent(null);
            }
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Selected NPC", selectedAgent, typeof(NPCTraversalAgent), true);
        }

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to inspect a running NPC.", MessageType.None);
            return;
        }

        DrawSimulationControls();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawExpeditionOutcomeState();
        DrawDreadEconomyState();
        DrawRecoverableLootState();
        if (selectedAgent == null)
        {
            if (!string.IsNullOrEmpty(lastActionMessage))
                EditorGUILayout.HelpBox(lastActionMessage, MessageType.None);
            EditorGUILayout.HelpBox("No running NPC is selected.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawRuntimeState();
        DrawGameplayActions();
        DrawRawDebugActions();
        EditorGUILayout.EndScrollView();
    }

    void DrawExpeditionOutcomeState()
    {
        GameplayLoopController loop = GameplayLoopController.Instance ??
            FindAnyObjectByType<GameplayLoopController>();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Expedition Outcomes", EditorStyles.boldLabel);
        if (loop == null)
        {
            EditorGUILayout.HelpBox(
                "No running GameplayLoopController was found.",
                MessageType.None);
            return;
        }

        DrawReadOnlyText("Completed Visits", loop.ExpeditionOutcomeCount.ToString());
        showExpeditionOutcomes = EditorGUILayout.Foldout(
            showExpeditionOutcomes,
            "Outcome Details",
            true);
        if (!showExpeditionOutcomes)
            return;

        IReadOnlyList<ExpeditionOutcomeRecord> outcomes = loop.ExpeditionOutcomes;
        if (outcomes.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int i = 0; i < outcomes.Count; i++)
        {
            ExpeditionOutcomeRecord outcome = outcomes[i];
            if (outcome == null)
            {
                EditorGUILayout.LabelField($"  {i + 1}. Missing outcome record");
                continue;
            }

            EditorGUILayout.LabelField(
                $"  {outcome.Outcome} | {outcome.AdventurerName} " +
                $"[agent {outcome.RuntimeAgentId}, level {outcome.AdventurerLevel}] | " +
                $"opening {outcome.DungeonOpenCount}");
            EditorGUILayout.LabelField(
                $"      cells {outcome.StartCell} -> {outcome.CompletionCell} | " +
                $"visited {outcome.VisitedCellCount}");
            EditorGUILayout.LabelField(
                $"      carried {outcome.CarriedTreasureItemCount} item(s), " +
                $"value {outcome.CarriedTreasureValue} | " +
                $"lost {outcome.LostTreasureItemCount}, value {outcome.LostTreasureValue} | " +
                $"recovered {outcome.RecoveredTreasureItemCount}, " +
                $"value {outcome.RecoveredTreasureValue}");

            string drop = string.IsNullOrEmpty(outcome.RecoveryDropId)
                ? "none"
                : outcome.RecoveryDropId;
            EditorGUILayout.LabelField(
                $"      Dread harvest {outcome.DreadHarvested} + visit {outcome.VisitDreadSettled} " +
                $"= {outcome.TotalDreadAwarded} | recovery drop {drop}");
            EditorGUILayout.LabelField(
                $"      {outcome.ExpeditionId} | duplicate completions rejected " +
                $"{outcome.DuplicateCompletionAttempts}");
        }
    }

    void DrawDreadEconomyState()
    {
        GameplayLoopController loop = GameplayLoopController.Instance ??
            FindAnyObjectByType<GameplayLoopController>();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dread Economy", EditorStyles.boldLabel);
        if (loop == null)
        {
            EditorGUILayout.HelpBox(
                "No running GameplayLoopController was found.",
                MessageType.None);
            return;
        }

        DrawReadOnlyText("Current Dread", loop.Dread.ToString());
        DrawReadOnlyText("Pending Visit Dread", loop.PendingVisitDread.ToString());
        DrawReadOnlyText("Recorded Harvests", loop.DreadHarvestCount.ToString());
        DrawReadOnlyText("Recorded Harvest Value", loop.TotalHarvestedDread.ToString());
        DrawReadOnlyText("Recorded Spends", loop.DreadSpendCount.ToString());
        DrawReadOnlyText("Recorded Spend Value", loop.TotalSpentDread.ToString());
        if (selectedAgent != null && selectedAgent.Character != null)
        {
            DrawReadOnlyText(
                "Selected NPC Death Harvest",
                loop.GetDeathDreadHarvestAmount(
                    selectedAgent.Character.Level).ToString());
        }

        DrawDreadSpendState(loop);

        showDreadHarvests = EditorGUILayout.Foldout(
            showDreadHarvests,
            "Harvest Details",
            true);
        if (!showDreadHarvests)
            return;

        IReadOnlyList<DreadHarvestRecord> harvests = loop.DreadHarvests;
        if (harvests.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        DreadHarvestRecord latest = harvests[harvests.Count - 1];
        using (new EditorGUI.DisabledScope(latest == null))
        {
            if (GUILayout.Button("Retry Latest Harvest (Duplicate Test)"))
                RetryHarvestForDuplicateTest(loop, latest);
        }

        for (int i = 0; i < harvests.Count; i++)
        {
            DreadHarvestRecord harvest = harvests[i];
            if (harvest == null)
            {
                EditorGUILayout.LabelField($"  {i + 1}. Missing harvest record");
                continue;
            }

            EditorGUILayout.LabelField(
                $"  +{harvest.Amount} | {harvest.Source} | {harvest.SourceName} " +
                $"[agent {harvest.SourceRuntimeAgentId}, level {harvest.SourceLevel}]");
            EditorGUILayout.LabelField(
                $"      opening {harvest.DungeonOpenCount} | cell {harvest.Cell} | " +
                $"duplicates rejected {harvest.DuplicateAttempts}");
            EditorGUILayout.LabelField($"      {harvest.HarvestId}");
        }
    }

    void DrawDreadSpendState(GameplayLoopController loop)
    {
        showDreadSpends = EditorGUILayout.Foldout(
            showDreadSpends,
            "Spend Details",
            true);
        if (!showDreadSpends)
            return;

        IReadOnlyList<DreadSpendRecord> spends = loop.DreadSpends;
        if (spends.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        DreadSpendRecord latest = spends[spends.Count - 1];
        using (new EditorGUI.DisabledScope(latest == null))
        {
            if (GUILayout.Button("Retry Latest Spend (Duplicate Test)"))
                RetrySpendForDuplicateTest(loop, latest);
        }

        for (int i = 0; i < spends.Count; i++)
        {
            DreadSpendRecord spend = spends[i];
            if (spend == null)
            {
                EditorGUILayout.LabelField($"  {i + 1}. Missing spend record");
                continue;
            }

            EditorGUILayout.LabelField(
                $"  -{spend.Amount} | {spend.Purpose} | cell {spend.Cell} | " +
                $"opening {spend.DungeonOpenCount}");
            EditorGUILayout.LabelField(
                $"      object {spend.ObjectId} ({spend.PrefabName}) | " +
                $"duplicates rejected {spend.DuplicateAttempts}");
            EditorGUILayout.LabelField($"      {spend.SpendId}");
        }
    }

    void RetrySpendForDuplicateTest(
        GameplayLoopController loop,
        DreadSpendRecord spend)
    {
        int dreadBefore = loop.Dread;
        bool effectApplied = false;
        bool accepted = loop.TrySpendDread(
            new DreadSpendRequest(
                spend.SpendId,
                spend.Purpose,
                spend.Amount,
                spend.DungeonOpenCount,
                spend.Cell,
                spend.ObjectId,
                spend.PrefabName),
            () =>
            {
                effectApplied = true;
                return true;
            },
            out DreadSpendRecord result,
            out _);

        lastActionMessage = !accepted && !effectApplied && loop.Dread == dreadBefore
            ? $"Duplicate spend rejected; Dread remains {dreadBefore}. " +
              $"Rejected attempts: {result?.DuplicateAttempts ?? 0}."
            : "Unexpected duplicate-spend result; inspect Dread and the spend record.";
    }

    void RetryHarvestForDuplicateTest(
        GameplayLoopController loop,
        DreadHarvestRecord harvest)
    {
        int dreadBefore = loop.Dread;
        bool accepted = loop.TryHarvestDread(
            new DreadHarvestRequest(
                harvest.HarvestId,
                harvest.Source,
                harvest.Amount,
                harvest.SourceId,
                harvest.SourceName,
                harvest.SourceRuntimeAgentId,
                harvest.SourceLevel,
                harvest.DungeonOpenCount,
                harvest.Cell,
                harvest.WorldPosition),
            out DreadHarvestRecord result);

        lastActionMessage = !accepted && loop.Dread == dreadBefore
            ? $"Duplicate rejected; Dread remains {dreadBefore}. " +
              $"Rejected attempts: {result?.DuplicateAttempts ?? 0}."
            : "Unexpected duplicate-test result; inspect the harvest record and Dread total.";
    }

    void DrawSimulationControls()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dungeon Simulation", EditorStyles.boldLabel);
        DrawReadOnlyText(
            "State",
            DungeonSimulationState.IsPaused ? "Paused" : "Running");

        string buttonLabel = DungeonSimulationState.IsPaused
            ? "Resume Dungeon Simulation"
            : "Pause Dungeon Simulation";
        if (!GUILayout.Button(buttonLabel))
            return;

        GameplayLoopController loop = GameplayLoopController.Instance ??
            FindAnyObjectByType<GameplayLoopController>();
        if (loop != null)
            loop.TogglePause();
        else
            DungeonSimulationState.TogglePause();

        lastActionMessage = DungeonSimulationState.IsPaused
            ? "Dungeon simulation paused; camera, selection, and debug actions remain active."
            : "Dungeon simulation resumed from its existing state.";
    }

    void DrawRecoverableLootState()
    {
        NPCTraversal traversal = selectedAgent != null
            ? selectedAgent.Navigation
            : FindAnyObjectByType<NPCTraversal>();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dungeon Recovery", EditorStyles.boldLabel);
        if (traversal == null)
        {
            EditorGUILayout.HelpBox(
                "No running NPCTraversal was found.", MessageType.None);
            return;
        }

        DrawReadOnlyText("Recoverable Drops", traversal.RecoverableLootDropCount.ToString());
        DrawReadOnlyText(
            "Physical Drop Views",
            traversal.PhysicalRecoverableLootDropCount.ToString());
        DrawReadOnlyText("Recoverable Items", traversal.RecoverableLootItemCount.ToString());
        DrawReadOnlyText("Recoverable Value", traversal.RecoverableLootValue.ToString());
        showRecoverableLoot = EditorGUILayout.Foldout(
            showRecoverableLoot,
            "Recovery Details",
            true);
        if (showRecoverableLoot)
            DrawRecoverableDrops(traversal);

        showDeathLootOutcomes = EditorGUILayout.Foldout(
            showDeathLootOutcomes,
            $"Death/Custody Outcomes ({traversal.DeathLootOutcomeCount})",
            true);
        if (showDeathLootOutcomes)
            DrawDeathLootOutcomes(traversal.DeathLootOutcomes);

        DrawReadOnlyText(
            "Escaped Items",
            traversal.EscapedDungeonLootItemCount.ToString());
        DrawReadOnlyText(
            "Escaped Value",
            traversal.EscapedDungeonLootValue.ToString());
        showSuccessfulEscapeLootOutcomes = EditorGUILayout.Foldout(
            showSuccessfulEscapeLootOutcomes,
            $"Successful Escape Outcomes ({traversal.SuccessfulEscapeLootOutcomeCount})",
            true);
        if (showSuccessfulEscapeLootOutcomes)
        {
            DrawSuccessfulEscapeLootOutcomes(
                traversal.SuccessfulEscapeLootOutcomes);
        }
    }

    static void DrawRecoverableDrops(NPCTraversal traversal)
    {
        IReadOnlyList<RecoverableLootDrop> drops = traversal.RecoverableLootDrops;
        if (drops.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int dropIndex = 0; dropIndex < drops.Count; dropIndex++)
        {
            RecoverableLootDrop drop = drops[dropIndex];
            if (drop == null)
            {
                EditorGUILayout.LabelField($"  {dropIndex + 1}. Missing drop record");
                continue;
            }

            bool hasWorldDrop = traversal.TryGetRecoverableLootWorldDrop(
                drop.DropId,
                out RecoverableLootWorldDrop worldDrop);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"  {drop.DropId} | {drop.SourceAdventurerName} | cell {drop.DropCell} | " +
                $"{drop.ItemCount} item(s), value {drop.TotalValue} | " +
                $"world {(hasWorldDrop ? "present" : "missing")}");
            if (hasWorldDrop && GUILayout.Button("Select Drop", GUILayout.Width(90f)))
                Selection.activeGameObject = worldDrop.gameObject;
            EditorGUILayout.EndHorizontal();
            IReadOnlyList<RecoverableLootItem> items = drop.Items;
            for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                RecoverableLootItem item = items[itemIndex];
                if (item == null)
                {
                    EditorGUILayout.LabelField("      Missing item record");
                    continue;
                }

                string source = item.HasSourceCell
                    ? $" | source {item.SourceCell}"
                    : string.Empty;
                EditorGUILayout.LabelField(
                    $"      {item.ItemId} | {item.Origin} | value {item.Value}{source}");
            }
        }
    }

    static void DrawDeathLootOutcomes(
        IReadOnlyList<AdventurerDeathLootOutcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int i = 0; i < outcomes.Count; i++)
        {
            AdventurerDeathLootOutcome outcome = outcomes[i];
            if (outcome == null)
            {
                EditorGUILayout.LabelField($"  {i + 1}. Missing outcome record");
                continue;
            }

            string drop = outcome.ProducedDrop
                ? outcome.RecoveryDropId
                : "no drop";
            EditorGUILayout.LabelField(
                $"  {outcome.SourceAdventurerName} [agent {outcome.SourceRuntimeAgentId}] " +
                $"| cell {outcome.DeathCell} | {drop}");
            EditorGUILayout.LabelField(
                $"      custody {outcome.CarriedItemCountBefore} item(s), " +
                $"value {outcome.CarriedValueBefore} -> " +
                $"{outcome.CarriedItemCountAfter} item(s), " +
                $"value {outcome.CarriedValueAfter}");
            EditorGUILayout.LabelField(
                $"      recovered {outcome.RecoveredItemCount} item(s), " +
                $"value {outcome.RecoveredValue} | cleared {outcome.CustodyCleared} | " +
                $"processed {outcome.RecoveryProcessed} | " +
                $"duplicate attempts {outcome.DuplicateProcessingAttempts}");
        }
    }

    static void DrawSuccessfulEscapeLootOutcomes(
        IReadOnlyList<AdventurerEscapeLootOutcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int i = 0; i < outcomes.Count; i++)
        {
            AdventurerEscapeLootOutcome outcome = outcomes[i];
            if (outcome == null)
            {
                EditorGUILayout.LabelField($"  {i + 1}. Missing outcome record");
                continue;
            }

            EditorGUILayout.LabelField(
                $"  {outcome.SourceAdventurerName} [agent {outcome.SourceRuntimeAgentId}] " +
                $"| entrance cell {outcome.ExitCell}");
            EditorGUILayout.LabelField(
                $"      custody {outcome.CarriedItemCountBefore} item(s), " +
                $"value {outcome.CarriedValueBefore} -> " +
                $"{outcome.CarriedItemCountAfter} item(s), " +
                $"value {outcome.CarriedValueAfter}");
            EditorGUILayout.LabelField(
                $"      escaped {outcome.EscapedItemCount} item(s), " +
                $"value {outcome.EscapedValue} | loss {outcome.ProducedLoss} | " +
                $"cleared {outcome.CustodyCleared} | processed {outcome.EscapeProcessed} | " +
                $"duplicate attempts {outcome.DuplicateProcessingAttempts}");

            IReadOnlyList<EscapedLootItem> items = outcome.EscapedItems;
            for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                EscapedLootItem item = items[itemIndex];
                if (item == null)
                {
                    EditorGUILayout.LabelField("      Missing item record");
                    continue;
                }

                string source = item.HasSourceCell
                    ? $" | source {item.SourceCell}"
                    : string.Empty;
                EditorGUILayout.LabelField(
                    $"      {item.ItemId} | {item.Origin} | value {item.Value}{source}");
            }
        }
    }

    void DrawRuntimeState()
    {
        NPCCharacter character = selectedAgent.Character;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
        DrawReadOnlyText("Name", character != null ? character.CharacterName : "Unavailable");
        DrawReadOnlyText("Behavior", selectedAgent.BehaviorState.ToString());
        DrawReadOnlyText("Visit", selectedAgent.VisitInProgress ? "In progress" : "Inactive");
        DrawReadOnlyText("Return State", GetReturnState(selectedAgent));
        DrawReadOnlyText("Current Cell", selectedAgent.CurrentCell.ToString());
        DrawReadOnlyText("Home Cell", selectedAgent.StartCell.ToString());
        DrawReadOnlyText("Home Position", selectedAgent.HomePosition.ToString("F2"));

        if (character != null)
        {
            DrawReadOnlyText(
                "Health", $"{character.CurrentHealth} / {character.MaxHealth}");
            DrawReadOnlyText(
                "Stamina", $"{character.CurrentStamina:F2} / {character.MaxStamina:F2}");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Investigation", EditorStyles.boldLabel);
        DrawReadOnlyText(
            "State", selectedAgent.IsInvestigating ? "Investigating" : "None");
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Target",
                selectedAgent.ActiveInvestigationTarget,
                typeof(DungeonPointOfInterest),
                true);
        }
        DrawReadOnlyText(
            "Time Remaining", $"{selectedAgent.InvestigationTimeRemaining:F2}s");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Traversal Memory", EditorStyles.boldLabel);
        showKnownCells = EditorGUILayout.Foldout(
            showKnownCells,
            $"Known Cells ({selectedAgent.VisitedCells.Count})",
            true);
        if (showKnownCells)
            DrawKnownCells();

        showKnownConnections = EditorGUILayout.Foldout(
            showKnownConnections,
            $"Known Connections ({selectedAgent.FamiliarConnections.Count})",
            true);
        if (showKnownConnections)
            DrawKnownConnections();

        showCarriedTreasure = EditorGUILayout.Foldout(
            showCarriedTreasure,
            $"Carried Loot ({selectedAgent.CarriedDungeonTreasureCount}, " +
            $"value {selectedAgent.CarriedDungeonTreasureValue})",
            true);
        if (showCarriedTreasure)
            DrawCarriedTreasure();
    }

    void DrawGameplayActions()
    {
        NPCCharacter character = selectedAgent.Character;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gameplay Actions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These actions use the same runtime APIs as gameplay and preserve normal death, " +
            "resource, and return behavior.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(character == null || character.IsDead))
        {
            damageAmount = Mathf.Max(1, EditorGUILayout.IntField("Damage Amount", damageAmount));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Damage"))
                {
                    NPCActionResolver.ResolveDamage(
                        character,
                        selectedAgent,
                        damageAmount,
                        selectedAgent.transform.position + Vector3.up * 0.35f);
                    lastActionMessage = $"Applied up to {damageAmount} damage.";
                }

                if (GUILayout.Button("Kill"))
                {
                    NPCActionResolver.ResolveDamage(
                        character,
                        selectedAgent,
                        Mathf.Max(1, character.CurrentHealth),
                        selectedAgent.transform.position + Vector3.up * 0.35f);
                    lastActionMessage = "Kill requested through the normal damage resolver.";
                }
            }

            healAmount = Mathf.Max(1, EditorGUILayout.IntField("Heal Amount", healAmount));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Heal"))
                {
                    character.Heal(healAmount);
                    lastActionMessage = $"Healed up to {healAmount} health.";
                }

                if (GUILayout.Button("Heal Fully"))
                {
                    character.Heal(character.MaxHealth);
                    lastActionMessage = "Health restored to maximum.";
                }
            }

            staminaAmount = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField("Stamina Amount", staminaAmount));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Drain"))
                {
                    character.SpendStamina(staminaAmount);
                    lastActionMessage = $"Drained up to {staminaAmount:F2} stamina.";
                }

                if (GUILayout.Button("Drain Fully"))
                {
                    character.SpendStamina(character.CurrentStamina);
                    lastActionMessage = "Stamina drained to zero.";
                }

                if (GUILayout.Button("Restore"))
                {
                    character.RestoreStamina(staminaAmount);
                    lastActionMessage = $"Restored up to {staminaAmount:F2} stamina.";
                }

                if (GUILayout.Button("Restore Fully"))
                {
                    character.RestoreStamina(character.MaxStamina);
                    lastActionMessage = "Stamina restored to maximum.";
                }
            }

            if (GUILayout.Button("Force Return Home"))
            {
                lastActionMessage = selectedAgent.TryForceReturnHome()
                    ? "NPC is returning through its known route."
                    : "Return could not start; the visit may be inactive or no known route is available.";
            }
        }

        if (!string.IsNullOrEmpty(lastActionMessage))
            EditorGUILayout.HelpBox(lastActionMessage, MessageType.None);
    }

    void DrawRawDebugActions()
    {
        NPCCharacter character = selectedAgent.Character;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Raw Debug Manipulation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These setters bypass incremental gameplay costs/recovery. Setting health to zero " +
            "still invokes the normal death event.",
            MessageType.Warning);

        using (new EditorGUI.DisabledScope(character == null || character.IsDead))
        {
            exactHealth = EditorGUILayout.IntField("Exact Health", exactHealth);
            if (GUILayout.Button("Set Exact Health (Raw Debug)"))
            {
                character.SetHealth(exactHealth);
                lastActionMessage = "Exact health value applied.";
            }

            exactStamina = EditorGUILayout.FloatField("Exact Stamina", exactStamina);
            if (GUILayout.Button("Set Exact Stamina (Raw Debug)"))
            {
                character.SetStamina(exactStamina);
                lastActionMessage = "Exact stamina value applied.";
            }
        }
    }

    void DrawKnownCells()
    {
        var cells = new List<Vector2Int>(selectedAgent.VisitedCells);
        cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        if (cells.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int i = 0; i < cells.Count; i++)
            EditorGUILayout.LabelField($"  {cells[i]}");
    }

    void DrawKnownConnections()
    {
        var connections = new List<NPCTraversalConnection>(
            selectedAgent.FamiliarConnections);
        connections.Sort(CompareConnections);
        if (connections.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int i = 0; i < connections.Count; i++)
        {
            NPCTraversalConnection connection = connections[i];
            EditorGUILayout.LabelField($"  {connection.first} <-> {connection.second}");
        }
    }

    void DrawCarriedTreasure()
    {
        IReadOnlyList<CarriedDungeonTreasure> treasure =
            selectedAgent.CarriedDungeonTreasure;
        if (treasure.Count == 0)
        {
            EditorGUILayout.LabelField("  None");
            return;
        }

        for (int i = 0; i < treasure.Count; i++)
        {
            CarriedDungeonTreasure item = treasure[i];
            if (item == null)
            {
                EditorGUILayout.LabelField($"  {i + 1}. Missing record");
                continue;
            }

            string source = item.HasSourceCell
                ? item.SourceCell.ToString()
                : "none";
            EditorGUILayout.LabelField(
                $"  {i + 1}. {item.TreasureId} | value {item.Value} | " +
                $"origin {item.Origin} | source {source}");
        }
    }

    static int CompareConnections(
        NPCTraversalConnection a,
        NPCTraversalConnection b)
    {
        int result = a.first.x.CompareTo(b.first.x);
        if (result != 0)
            return result;
        result = a.first.y.CompareTo(b.first.y);
        if (result != 0)
            return result;
        result = a.second.x.CompareTo(b.second.x);
        return result != 0 ? result : a.second.y.CompareTo(b.second.y);
    }

    static string GetReturnState(NPCTraversalAgent agent)
    {
        if (!agent.VisitInProgress)
            return "Visit inactive";
        if (!agent.IsReturningHome)
            return "Exploring";
        if (agent.CurrentCell == agent.StartCell)
            return "At home cell; approaching entrance";
        if (agent.ActiveRoute == null)
            return "Return requested; no active known route";
        return "Following known route home";
    }

    static void DrawReadOnlyText(string label, string value)
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField(label, value);
    }

    void SetSelectionMode(bool enabled)
    {
        selectionMode = enabled && EditorApplication.isPlaying;
        lastActionMessage = selectionMode
            ? "Selection enabled. Click an NPC in Game View."
            : null;
        UpdateInputSubscription();
        if (!selectionMode)
            SelectAgent(null);
    }

    void UpdateInputSubscription()
    {
        InputManager desired = selectionMode && EditorApplication.isPlaying
            ? InputManager.Instance
            : null;
        if (subscribedInputManager == desired)
            return;

        ReleaseInputSubscription();
        subscribedInputManager = desired;
        if (subscribedInputManager != null)
        {
            subscribedInputManager.OnClicked += OnGameViewClicked;
        }
        else if (selectionMode)
        {
            lastActionMessage = "Selection is waiting for the scene InputManager.";
        }
    }

    void ReleaseInputSubscription()
    {
        if (subscribedInputManager != null)
            subscribedInputManager.OnClicked -= OnGameViewClicked;
        subscribedInputManager = null;
    }

    void OnGameViewClicked()
    {
        if (!selectionMode || Mouse.current == null)
            return;
        if (!IsGameView(mouseOverWindow) && !IsGameView(focusedWindow))
            return;

        TrySelectAgentAt(Mouse.current.position.ReadValue());
    }

    void TryUseHierarchySelection()
    {
        GameObject hierarchyObject = Selection.activeGameObject;
        if (hierarchyObject == null)
        {
            lastActionMessage = "Select a running NPC or one of its children in Hierarchy first.";
            Repaint();
            return;
        }

        NPCTraversalAgent agent = hierarchyObject.GetComponent<NPCTraversalAgent>();
        if (agent == null)
            agent = hierarchyObject.GetComponentInParent<NPCTraversalAgent>(true);
        if (agent == null)
            agent = hierarchyObject.GetComponentInChildren<NPCTraversalAgent>(true);
        if (agent == null)
        {
            lastActionMessage =
                $"'{hierarchyObject.name}' is not part of a running NPC with an " +
                "NPCTraversalAgent component.";
            Repaint();
            return;
        }

        SelectAgent(agent);
    }

    void SelectAgent(NPCTraversalAgent agent)
    {
        selectedAgent = agent;
        lastActionMessage = null;
        if (selectedAgent != null && selectedAgent.Character != null)
        {
            exactHealth = selectedAgent.Character.CurrentHealth;
            exactStamina = selectedAgent.Character.CurrentStamina;
        }

        if (selectedAgent != null)
            RequestCameraFocus(selectedAgent.transform);
        else
            ReleaseCameraFocus();

        if (selectionMode && selectedAgent != null)
            UpdateHighlight();
        else
            DestroyHighlight();
        Repaint();
    }

    void RequestCameraFocus(Transform target)
    {
        Camera mainCamera = Camera.main;
        focusedCamera = mainCamera != null
            ? mainCamera.GetComponent<CameraFollow>()
            : null;
        if (focusedCamera == null)
            focusedCamera = FindAnyObjectByType<CameraFollow>();

        if (focusedCamera == null)
        {
            lastActionMessage =
                "The selected NPC is highlighted, but no active CameraFollow was found.";
            return;
        }

        if (!focusedCamera.FocusTarget(target))
        {
            lastActionMessage =
                "The selected NPC is highlighted, but the gameplay camera could not focus it.";
        }
    }

    void ReleaseCameraFocus()
    {
        if (focusedCamera != null)
            focusedCamera.ClearFocus();
        focusedCamera = null;
    }

    void TrySelectAgentAt(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].isActiveAndEnabled)
                {
                    camera = cameras[i];
                    break;
                }
            }
        }

        if (camera == null)
        {
            lastActionMessage = "No active Game View camera was found.";
            return;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            camera.farClipPlane,
            ~0,
            QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            NPCTraversalAgent agent = hits[i].collider
                .GetComponentInParent<NPCTraversalAgent>();
            if (agent != null)
            {
                SelectAgent(agent);
                return;
            }
        }

        SelectAgent(FindClosestScreenAgent(camera, screenPosition));
        if (selectedAgent == null)
            lastActionMessage = "No NPC was found under the Game View pointer.";
    }

    static NPCTraversalAgent FindClosestScreenAgent(
        Camera camera,
        Vector2 screenPosition)
    {
        NPCTraversalAgent[] agents = FindObjectsByType<NPCTraversalAgent>(
            FindObjectsInactive.Exclude);
        NPCTraversalAgent closest = null;
        float closestDistance = 32f * 32f;
        for (int i = 0; i < agents.Length; i++)
        {
            NPCTraversalAgent agent = agents[i];
            Vector3 projected = camera.WorldToScreenPoint(agent.transform.position);
            if (projected.z <= 0f)
                continue;
            float distance = (new Vector2(projected.x, projected.y) - screenPosition)
                .sqrMagnitude;
            if (distance >= closestDistance)
                continue;
            closestDistance = distance;
            closest = agent;
        }
        return closest;
    }

    static bool IsGameView(EditorWindow window)
    {
        return window != null && window.GetType().FullName == "UnityEditor.GameView";
    }

    void UpdateHighlight()
    {
        if (selectedAgent == null)
        {
            DestroyHighlight();
            return;
        }

        EnsureHighlight();
        if (highlightRoot == null || highlightLines.Count != 7)
            return;

        Bounds bounds = GetAgentBounds(selectedAgent);
        Vector3 extents = bounds.extents;
        float radius = Mathf.Max(0.2f, Mathf.Max(extents.x, extents.z) + 0.08f);
        float bottom = bounds.min.y - 0.04f;
        float middle = bounds.center.y;
        float top = bounds.max.y + 0.04f;
        float width = Mathf.Max(0.015f, bounds.size.magnitude * 0.012f);

        SetRing(highlightLines[0], bounds.center, radius, bottom, width);
        SetRing(highlightLines[1], bounds.center, radius, middle, width);
        SetRing(highlightLines[2], bounds.center, radius, top, width);
        SetVertical(highlightLines[3], bounds.center, radius, bottom, top, 1f, 0f, width);
        SetVertical(highlightLines[4], bounds.center, radius, bottom, top, -1f, 0f, width);
        SetVertical(highlightLines[5], bounds.center, radius, bottom, top, 0f, 1f, width);
        SetVertical(highlightLines[6], bounds.center, radius, bottom, top, 0f, -1f, width);
    }

    void EnsureHighlight()
    {
        if (highlightRoot != null &&
            highlightRoot.transform.parent == selectedAgent.transform)
        {
            return;
        }

        DestroyHighlight();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return;

        highlightMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
            color = new Color(0f, 1f, 0.9f, 1f)
        };
        highlightRoot = new GameObject("NPC Debug Selection Highlight")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        highlightRoot.transform.SetParent(selectedAgent.transform, false);

        for (int i = 0; i < 7; i++)
        {
            var lineObject = new GameObject($"Highlight Line {i + 1}")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineObject.transform.SetParent(highlightRoot.transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.hideFlags = HideFlags.HideAndDontSave;
            line.sharedMaterial = highlightMaterial;
            line.startColor = highlightMaterial.color;
            line.endColor = highlightMaterial.color;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            highlightLines.Add(line);
        }
    }

    static Bounds GetAgentBounds(NPCTraversalAgent agent)
    {
        Renderer[] renderers = agent.GetComponentsInChildren<Renderer>(false);
        Bounds bounds = new(agent.transform.position + Vector3.up * 0.5f, Vector3.zero);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is LineRenderer)
                continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
        {
            Collider[] colliders = agent.GetComponentsInChildren<Collider>(false);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!found)
                {
                    bounds = colliders[i].bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }
        }

        if (!found)
            bounds = new Bounds(agent.transform.position + Vector3.up * 0.5f,
                new Vector3(0.4f, 1f, 0.4f));
        return bounds;
    }

    static void SetRing(
        LineRenderer line,
        Vector3 center,
        float radius,
        float height,
        float width)
    {
        line.loop = true;
        line.positionCount = HighlightRingSegments;
        line.startWidth = width;
        line.endWidth = width;
        for (int i = 0; i < HighlightRingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / HighlightRingSegments;
            line.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                height,
                center.z + Mathf.Sin(angle) * radius));
        }
    }

    static void SetVertical(
        LineRenderer line,
        Vector3 center,
        float radius,
        float bottom,
        float top,
        float xDirection,
        float zDirection,
        float width)
    {
        line.loop = false;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        float x = center.x + radius * xDirection;
        float z = center.z + radius * zDirection;
        line.SetPosition(0, new Vector3(x, bottom, z));
        line.SetPosition(1, new Vector3(x, top, z));
    }

    void DestroyHighlight()
    {
        highlightLines.Clear();
        if (highlightRoot != null)
            DestroyImmediate(highlightRoot);
        if (highlightMaterial != null)
            DestroyImmediate(highlightMaterial);
        highlightRoot = null;
        highlightMaterial = null;
    }
}
