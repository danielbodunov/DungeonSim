# t028 — Save Deletion UI

## Tracking
- **ID:** t028
- **Status:** Complete
- **Milestone:** Save Management / UX
- **Depends on:** Existing save/load persistence and save-selection UI

## Goal
Allow the player to delete an existing save from the save-selection UI through an explicit, safe, and immediately reflected workflow.

## Requirements
- Add a delete action for each selectable save entry in the existing save-management/load UI.
- Require explicit confirmation before destructive deletion.
- Make the confirmation identify the save being deleted clearly enough to prevent accidental deletion of the wrong slot/file.
- Route deletion through the existing save/persistence authority rather than duplicating file-path or serialization logic in UI code.
- After successful deletion, refresh the visible save list and selection state immediately.
- Handle deletion failure without leaving the UI in a state that implies the save was removed.
- Preserve all unrelated save/load behavior.

## Interaction Contract
The intended flow is:

```text
Save Entry
  -> Delete
  -> Confirmation
      -> Cancel: no mutation
      -> Confirm: delete selected save
                    -> refresh save list
```

Deletion should be impossible without the confirmation step.

## Acceptance Criteria
- Every deletable save entry exposes a clear delete action.
- Selecting delete does not remove data until the player confirms.
- Cancelling confirmation leaves the save and UI unchanged.
- Confirming deletion removes only the selected save.
- The deleted save disappears from the UI without requiring the menu or application to be restarted.
- Remaining save entries continue to load normally.
- Deleting the final save produces the correct empty-save state.
- A failed filesystem/persistence deletion reports failure or otherwise leaves the save visibly present rather than pretending deletion succeeded.

## Out of Scope
- Save renaming
- Save duplication
- Cloud-save synchronization
- Autosave policy changes
- Save-format migration
- Bulk deletion
- Recycling/undo of deleted saves

## Manual Validation
1. Create or identify at least two saves.
2. Trigger delete on one save and cancel; confirm neither save changes.
3. Trigger delete again and confirm; verify only the selected save is removed.
4. Load the remaining save successfully.
5. Delete the final save and verify the empty-save UI state.
6. If practical, simulate a persistence deletion failure and confirm the UI does not report a false success.

## Post-Implementation Report

- Updated runtime-generated `GameplayLoopUI`: every save row now exposes Load
  and Delete actions. Delete opens a modal blocker above the save menu rather
  than mutating persistence immediately.
- Added `GameSaveManager.DeleteSave(SaveSlotInfo)` as the sole deletion
  authority. It refreshes recognized slots, canonicalizes and exactly matches
  the selected path, handles named and legacy saves, deletes only that file,
  verifies absence, and reports through the existing status event.
- Confirmation names both the display name and backing filename and states that
  deletion cannot be undone. Cancel and Escape close only the confirmation and
  make no persistence call. Closing the save menu also clears pending deletion.
- Successful deletion closes confirmation, refreshes rows immediately, and
  moves event-system selection back to the save-name input. Deleting the final
  slot therefore displays the existing empty-save message without reopening the
  menu. Remaining Load actions are rebuilt from fresh slot records.
- Failure keeps confirmation open and rebuilds the still-authoritative save
  list through `StatusChanged`; the error remains visible in menu status rather
  than implying removal.
- Added `GameSaveDeletionTests` using an isolated temporary persistence root.
  Coverage verifies that deleting one recognized save preserves another and
  that an unrecognized path is rejected without deleting its file.
- Runtime and editor C# compilation succeeds. Unity validation was confirmed by
  the user on 2026-08-31.

## Validation Result

Complete. The confirmation/cancel flow, selected-save deletion, immediate list
refresh, remaining-save loading, final-save empty state, and failure-safe UI
behavior were approved in Unity on 2026-08-31.

## Git
Suggested implementation branch: `feature/t028-save-deletion-ui`

Proceed according to `docs/AGENTS.md`.
