# DEV003 — NPC Runtime Debug Harness

## Tracking
- **ID:** DEV003
- **Status:** Complete
- **Depends on:** DEV002
- **Blocks:** t007

## Goal
Provide an Editor/developer-only way to select an NPC directly from Game View, inspect important runtime state, and deliberately create gameplay conditions needed for testing.

## Requirements
- Add a developer/debug selection mode for clicking an NPC in Game View.
- Clearly highlight the selected NPC.
- Show health, stamina, behavior/state, current cell, home cell, carried dungeon treasure, known cells/connections, investigation state, and return state where available.
- Add gameplay-oriented actions such as damage, kill, heal, drain/restore stamina, and force return using normal gameplay APIs where practical.
- Allow exact health/stamina setting as clearly identified raw debug manipulation.
- Provide hierarchy selection for the selected NPC.
- Keep tooling Editor/development-only and isolated from normal player UI/input.

## Acceptance Criteria
- A running NPC can be selected from Game View.
- Its relevant state is visible without searching components manually.
- Health/stamina can be manipulated quickly.
- The NPC can be killed, healed, or forced toward return behavior for scenario testing.
- Carried treasure and traversal-memory summary can be inspected.
- Normal gameplay is unaffected when debug mode is disabled.

## Constraints
Do not implement a broad AI visualization system or decision-history framework here. Those remain later tooling work.

## Implementation Notes

- `Tools > NPC Runtime Debug Harness` opens an Editor-only window. Its explicit selection toggle subscribes to the running scene's existing `InputManager.OnClicked` event and accepts the click only while Game View is hovered or focused. It unsubscribes when disabled, closed, or outside Play Mode.
- Selection first raycasts against NPC child colliders and then uses a small screen-space fallback for compatible NPC prefabs without colliders.
- While selection mode is enabled, the selected NPC receives a transient cyan world-space cage built from hidden, non-saved `LineRenderer` objects. Disabling the mode, clearing the selection, closing the window, or leaving Play Mode removes the highlight.
- The inspector shows character health/stamina, traversal behavior, current/home cells, visit and return state, active investigation target/time, known cells/connections, and carried dungeon treasure details.
- Hierarchy selection remains available throughout Play Mode and resolves an `NPCTraversalAgent` from the selected object, its parents, or its children. Invalid selections produce an explanatory message instead of disabling the action.
- Gameplay actions use `NPCActionResolver.ResolveDamage`, `NPCCharacter.Heal`, `SpendStamina`, `RestoreStamina`, and `NPCTraversalAgent.TryForceReturnHome`. Force Return cancels the current activity and uses the same familiar-connection route planning as stamina-driven return.
- Exact health and stamina setters are separated under a warning-labeled **Raw Debug Manipulation** section. Exact zero health still invokes the normal death event.
- `NPCTraversalAgent` now exposes its current coarse behavior and investigation state as read-only runtime data. No decision history or broad AI visualization was added.

## Validation Performed

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo` completed with 0 warnings and 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` completed with 0 errors after including the new Editor source in the generated project for the focused compile. The one warning is pre-existing in `TileSocketBakerWindow.cs` (`CS0414`).
- Focused call-path review confirmed damage/kill use the existing action resolver, health/stamina actions raise existing character progress/death events, and forced return uses familiar traversal memory and the existing return route coroutine.
- Scope review confirmed Game View click handling and highlight objects exist only in the Editor assembly; the harness removes its transient highlight and unsubscribes from input when disabled or closed.
- Follow-up Editor compilation after replacing unreliable Editor-frame mouse polling with the production click event completed with 0 errors and only the same pre-existing `TileSocketBakerWindow.cs` warning.
- Follow-up Editor compilation after making Hierarchy selection resolution explicit completed with 0 errors and only the same pre-existing warning.
- Manual Unity validation was completed on 2026-08-14. Game View and Hierarchy selection, highlighting, runtime inspection, gameplay/raw resource actions, forced return, death cleanup, and disabled-mode isolation passed.

## Manual Unity Validation Performed

1. Enter Play Mode with a dungeon scenario that can spawn an NPC, then open `Tools > NPC Runtime Debug Harness`.
2. Enable **Game View NPC Selection**, click a moving NPC in Game View, and verify it becomes selected in the window with a clearly visible cyan highlight.
3. Verify health, stamina, behavior, current/home cells, visit/return state, investigation state, known cells/connections, and carried treasure update while the NPC explores and investigates treasure.
4. Choose **Select In Hierarchy** and verify the selected NPC GameObject is selected and pinged in Hierarchy.
5. Exercise Damage, Heal, Drain, Restore, and their full-value variants. Verify the status bars and window values agree and normal runtime feedback/events still occur.
6. During movement or investigation, choose **Force Return Home**. Verify the current activity stops, return state changes, and the NPC follows its known route to the entrance and completes the visit.
7. Spawn another NPC and test the clearly labeled exact health/stamina setters, including clamping outside valid ranges. Verify exact zero health follows the normal death/despawn path.
8. Use **Kill** and verify the normal death/despawn behavior occurs once without an orphaned highlight or window error.
9. Disable selection mode and verify the highlight disappears, Game View clicks no longer change harness selection, and normal gameplay input behaves as before. Also verify closing the window and leaving Play Mode remove all debug visuals.

## Known Limitations

- The harness requires Play Mode and an active Game View camera.
- Forced return can report failure when the NPC has no familiar route from its recorded current cell to its home cell; it does not reveal unexplored connections for debug convenience.

## Git
Implementation branch: `feature/dev003-npc-debug-harness`
