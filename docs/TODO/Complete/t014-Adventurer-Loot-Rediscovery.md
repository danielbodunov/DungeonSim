# t014 — Adventurer Loot Rediscovery

## Tracking
- **ID:** t014
- **Status:** Complete
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t013

## Goal
Allow later adventurers to discover and acquire physical loot left by previous failed expeditions, creating persistent consequences and emergent expedition chains.

## Requirements
- Physical loot drops participate in the existing POI/discovery model.
- Adventurers must physically discover a drop; no remote knowledge.
- Investigation should take place at the location of the drop.
- Investigation/acquisition transfers the drop's contents into the new adventurer's authoritative carried-loot state.
- Claimed drops resolve exactly once and disappear/transition appropriately.
- Preserve original provenance where useful so later outcomes can distinguish dungeon bait from resources brought/dropped by adventurers.
- Do not make NPCs omniscient about old drops.

## Acceptance Criteria
- Adventurer B can discover loot dropped by Adventurer A.
- B can acquire it through normal investigation/POI flow.
- The original world drop cannot be claimed twice.
- If B later dies, the acquired contents can enter the normal death-drop lifecycle again.
- If B escapes, the acquired contents follow the normal escape/loss lifecycle.

## Out of Scope
- Sophisticated loot desirability scoring
- Contested pickup/combat
- Player recovery interaction (t015)

## Git
Suggested branch: `feature/t014-loot-rediscovery`

## Manual Validation

1. Have Adventurer A take known dungeon treasure and die away from the entrance. Record the physical drop ID, item ID, value, origin, and source cell.
2. Start Adventurer B. Before B enters the drop's cell, verify B has no investigation target or carried knowledge of the drop.
3. Let B enter the drop's cell. Verify B walks to the bag's actual world position, then changes to `Investigating` for the configured duration.
4. When investigation completes, verify the world bag and its t013 recovery record disappear exactly once and B's **Carried Loot** gains matching item identity, value, origin, and source-cell data.
5. Exercise any repeated completion/claim path against the old drop reference and verify it cannot add the contents again.
6. Kill B before it exits. Verify exactly one new physical drop appears with a new drop ID while the recovered items retain their original provenance.
7. In a separate run, let B acquire A's drop and escape. Verify the items appear in the normal successful-escape/loss outcome and no recovery drop remains.
8. Place another physical drop at or near the entrance and start a new adventurer. Verify the starting-cell arrival also discovers and investigates it locally.
9. Kill a treasure-carrying adventurer while it is climbing a ladder and again just below an elevated platform. Verify each bag resolves to supported ground below, its debug `DropCell` matches the physical cell, and a later adventurer can route to and investigate it without stalling at the ladder exit.
10. Capture a scenario with no recoverable drops and note the completed-expedition and duplicate-attempt histories. Create drops and additional outcomes, then load/reset the scenario. Verify all later recovery records, bags, POIs, lookup entries, and outcome/attempt entries disappear while the captured counts return exactly.
11. Capture a second scenario with a recoverable drop and non-zero outcome history. Resolve or add state, reload twice, and verify both reloads recreate the same bag contents/location, histories, Dread/progression baseline, roster, gameplay speed, and next generated runtime/drop identities.

## Implementation Status

- `RecoverableLootWorldDrop` now implements the existing `IDungeonPointOfInterestInteraction` contract and delegates acquisition to the investigating `NPCTraversalAgent`.
- The agent validates active-visit state, POI availability, and current-cell ownership before claiming through t013's authoritative `TryClaimRecoverableLoot` boundary. A successful claim removes the source record/view once, then transfers every item snapshot into carried custody and refreshes the generic carried-loot visual.
- `CarriedDungeonTreasure` retains compatibility with its existing constructor while exposing explicit `RecoverableLootOrigin` and `HasSourceCell` data. Death recovery and successful escape now copy those fields rather than reconstructing provenance from dungeon-bait status alone.
- POI investigation approaches the target's grounded `InteractionPosition` before starting its timer. This applies the existing generic POI behavior to treasure and recovery drops and ensures acquisition occurs at the physical bag rather than merely somewhere within its cell.
- Starting-cell and fall-recovery arrivals now run the same current-cell POI discovery check used after normal route steps. No drop is queried before the agent physically occupies its cell.
- The debug harness labels the aggregate as **Carried Loot** and displays origin plus optional source-cell data.
- Death drops now start with the NPC's actual death position and reuse `NPCTraversal.TryGetFallRecoveryLanding`, including its deep downward support raycast. The resolved cell is accepted only when the landing position maps back to that cell and the cell is reachable from the authoritative entrance. A deterministic nearest supported/reachable graph cell, then the production entrance spawn pose, provide bounded fallbacks when the exact vertical line cannot produce a valid POI location.
- `RecoverableLootDrop.DropCell` and `WorldPosition` now store the supported resting result while `AdventurerDeathLootOutcome` continues recording the actual death cell/position. Save/load therefore preserves both semantics without Rigidbody simulation.
- New scenario captures include authoritative recovery records, death/escape custody histories (including duplicate-processing attempts), next drop/runtime-agent IDs, Dread-harvest history, expedition outcomes (including duplicate-completion attempts), Dread total, dungeon opening count/level, gameplay speed, and the persistent adventurer roster.
- Scenario runtime state is prevalidated before layout mutation. Applying a new snapshot clears active visits and reconstructs records, world bags, POIs, and runtime lookup dictionaries from the captured baseline. Legacy scenarios without runtime snapshots explicitly clear recovery and diagnostic histories instead of retaining stale test state.
- Active visits, routes, current health/stamina, pending visit Dread, investigation progress, and the current exploration timer are intentionally not resumed. Applying a scenario returns the loop to Expansion and clears these transient states; the captured persistent roster/progression becomes the next-run baseline.

## Validation Notes

- Runtime compilation completed with 0 warnings and 0 errors.
- Editor compilation completed with 0 errors and the existing `TileSocketBakerWindow.visualizeSamples` CS0414 warning.
- Static call-path review confirmed the world drop claims through the authoritative t013 record before custody transfer, repeated claims fail after record removal, and death/escape both consume the transferred carried records through their existing production paths.
- Static scenario review confirmed validation occurs before `PrepareForScenarioApply` or `RestoreTileLayout`, stale recovery views are disabled/unregistered before destruction, and all diagnostic collections are restored together with their ID lookups and duplicate-attempt fields.
- Unity Play Mode validation completed successfully, including the loot-rediscovery flow and the follow-up ladder-grounding/scenario-reset corrections.

## Known Limitations

- Adventurers acquire any available physical drop they investigate; desirability and prioritization remain out of scope.
- Concurrent/contested pickup behavior and player recovery remain deferred.
- Scenario application restores the meaningful gameplay baseline but does not rewind Unity's global random-number state. Later random exploration choices may differ even though layout, loot, progression, identities, and outcome histories start identically.
