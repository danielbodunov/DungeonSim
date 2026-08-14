# DEV002 — Reusable Dungeon Test Scenarios

## Tracking
- **ID:** DEV002
- **Status:** Complete
- **Depends on:** DEV001
- **Blocks:** DEV003; t007

## Goal
Allow useful dungeon test layouts to be captured once and recreated reliably from Editor tooling instead of rebuilding them manually for every test.

## Requirements
- Introduce a reusable scenario asset containing test setup data.
- Capture tiles, rotations, entrance, traps, treasure, and other supported authored state needed to recreate the test.
- Add Editor actions to capture the current dungeon, load a scenario, reset it, save a new scenario, and intentionally update an existing scenario.
- Reconstruct scenarios through normal production placement/game APIs wherever practical.
- Store a scenario name, description, and intended test purpose.

## Acceptance Criteria
- A manually constructed dungeon can be captured as a scenario asset.
- Loading recreates its layout and supported content consistently.
- Reset restores the authored initial state.
- Treasure, traps, and entrance survive capture/reload.
- Repeated tests no longer require rebuilding the layout manually.

## Constraints
Do not create a parallel dungeon implementation or automated test runner in this ticket.

## Implementation Notes

- `DungeonTestScenario` is a reusable `ScriptableObject` containing scenario metadata, grid dimensions, resolved tile profile IDs (including rotation), width and connection intents, the procedural prop seed, traps, entrance, and floor props such as treasure.
- Authored trap, entrance, and floor-prop records retain direct prefab references while preserving object IDs and prefab names for normal placement/save compatibility.
- `Tools > Dungeon Test Scenarios` captures and replays the running dungeon in Play Mode. Capture creates a pending snapshot; saving a new asset always uses a unique path, while updating an existing asset is a separate confirmation-gated action.
- Load and reset both restore the selected asset through `TileGridGenerator.RestoreTileLayout`, `PlaceTrapCell`, `PlaceEntranceCell`, `PlaceFloorPropCell`, and `RegenerateProps`. Reset intentionally reapplies the asset after runtime mutations.
- Captured trap and floor-prop records are sorted by cell and object ID so repeated captures serialize consistently.

### Follow-Up: Non-Mutating Apply Validation

`DungeonTestScenario.TryApplyTo()` now completes a dry-run validation phase before calling any production restore or placement API. The incoming tile assignments are propagated through copied solver state, and shared production validators check resolved prefabs, built-cell requirements, entrance sockets, floor-prop compatibility, topology-sensitive/interactive content, and authored occupancy conflicts. Trap, entrance, and floor-prop reservations exist only in the validation context.

`RestoreTileLayout()` uses the same pure layout validator and applies its validated cell-option snapshot only after validation succeeds. Live trap, entrance, and floor-prop placement also use the shared validators, keeping scenario validation aligned with production placement rules. A failed scenario validation returns a placement-specific error without clearing generated props, authored content, tile instances, the active placement selection, or any other running-dungeon state.

## Validation Performed

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo` completed with 0 warnings and 0 errors after including the new runtime source in the generated project for the focused compile.
- `dotnet build Assembly-CSharp-Editor.csproj --nologo` completed with 0 errors after including the new Editor source in the generated project for the focused compile. The one warning is pre-existing in `TileSocketBakerWindow.cs` (`CS0414`).
- Focused call-path review confirmed scenario replay uses the production layout, trap, entrance, floor-prop, and procedural-prop APIs rather than duplicating dungeon reconstruction.
- Serialization review confirmed the asset records tile profile IDs/rotations, connection and width intents, placement identity and prefab references, floor-prop resolved state, metadata, grid dimensions, and the procedural generation seed.
- Manual Unity validation was completed on 2026-08-13. All steps below passed, including capture/save-new, load, reset, intentional update, treasure interaction, repeated replay, and trap regression coverage.
- Follow-up compilation: `dotnet build Assembly-CSharp.csproj --no-restore --nologo` completed with 0 warnings and 0 errors after the non-mutating validation change.
- Follow-up Editor compilation: `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` completed with 0 errors and the same pre-existing `TileSocketBakerWindow.cs` warning.
- Focused mutation-boundary review confirmed every failure in the new preflight path occurs before `RestoreTileLayout()`, prop clearing, object destruction, placement, regeneration, or layout events.
- Follow-up manual Unity validation was completed on 2026-08-14. Valid scenarios still applied correctly, invalid tile/content/prefab/occupancy cases were rejected with clear errors, and every rejection left the running dungeon and active placement preview unchanged.

## Manual Unity Validation Performed

1. Enter Play Mode in the normal dungeon scene and build a layout containing rotated tiles, an entrance, at least one trap, and treasure on a compatible cell without a treasure socket.
2. Open `Tools > Dungeon Test Scenarios`, enter a name, description, and intended test purpose, choose **Capture Current Dungeon**, then **Save Captured Scenario As New...**.
3. Mutate the running dungeon, select the saved scenario, and choose **Load Selected Scenario**. Verify the layout, rotations, entrance, traps, treasure state, and generated props match the captured state.
4. Run an NPC treasure interaction, then choose **Reset Selected Scenario**. Verify the authored initial treasure state and all other captured content return.
5. Modify the authored setup, capture it again, choose **Update Selected Scenario From Capture...**, confirm the warning, then reset and verify the intentional update is now the restored state.
6. Repeat load/reset several times and verify the Console contains no placement or restore warnings and trap behavior remains functional.

## Known Limitations

- Scenario capture, load, and reset require Play Mode because the production dungeon grid initializes at runtime.
- A scenario can only be applied to a running grid with the same dimensions and compatible tile profiles/prefabs.

## Git
Implementation branch: `feature/dev002-test-scenarios`
