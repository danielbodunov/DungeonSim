# DEV005 — Editor Window Input Isolation

## Tracking

- **ID:** DEV005
- **Status:** Complete
- **Milestone:** Developer Tooling — Observation & Control
- **Depends on:** DEV004
- **Blocks:** DEV006

## Goal

Prevent mouse-wheel and other Editor-tool interactions from simultaneously driving gameplay camera/input behavior. Scrolling a debug/test Editor window should scroll that window only, not zoom the Game View camera.

## Requirements

- Investigate how gameplay camera input is currently consumed in the Unity Editor.
- Establish a central input-ownership/focus rule rather than adding independent scroll suppression hacks to every Editor window.
- Gameplay camera zoom must only consume scroll input when the Game View is the intended input target.
- Scrolling within `NPCRuntimeDebugHarnessWindow`, `DungeonTestScenarioWindow`, and future Editor tools must not zoom the gameplay camera.
- Apply the same ownership principle to mouse clicks/drags where overlapping Editor and Game View input could produce unintended gameplay actions.
- Preserve normal Game View camera controls when the Game View is actively being used.
- Do not make production input depend on UnityEditor APIs outside Editor-conditional integration.

## Acceptance Criteria

- Mouse-wheel scrolling over the NPC runtime debug harness scrolls the Editor window without changing gameplay camera zoom.
- Mouse-wheel scrolling over the dungeon test scenario window does not change gameplay camera zoom.
- Normal Game View scrolling still controls camera zoom.
- Clicking/dragging Editor controls does not unintentionally trigger overlapping gameplay camera actions where the same input is consumed.
- Input behavior remains correct after switching focus repeatedly between Game View and tooling windows.
- Player/runtime builds remain free of Editor-only dependencies.

## Architecture Direction

Solve input ownership centrally. The gameplay input layer should consume camera/input actions only when the Game View/gameplay surface owns that interaction.

Avoid per-window patches such as each Editor window manually disabling the camera on `MouseEnter` unless investigation proves there is no reliable centralized alternative.

## Out of Scope

- Rebinding player controls
- Replacing the entire input system
- Debug hotkey redesign
- Selective simulation pause

## Manual Validation

1. Enter Play Mode and open the NPC debug and scenario windows.
2. Scroll each Editor window while the mouse is over its scrollable content.
3. Verify the Editor content scrolls and Game View camera zoom remains unchanged.
4. Move focus to Game View and verify camera zoom works normally.
5. Alternate rapidly between Game View and Editor tools and verify input ownership remains correct.
6. Exercise clickable/drag controls in the Editor tools and confirm they do not leak into gameplay input or NPC selection.
7. With an NPC camera focus active, scroll the NPC debug window and verify neither camera zoom nor focus changes. Then scroll over Game View and verify focused zoom still works.
8. Start tile/object placement, click controls in both Editor tools, and verify no placement occurs. Click Game View and verify normal placement still occurs.
9. Focus an Editor tool and press movement/Escape keys; verify gameplay camera/placement does not respond. Focus Game View and verify the same keys work normally.
10. Build or inspect the player runtime assembly and verify it has no `UnityEditor` dependency.

## Implementation Status

- `InputManager` now owns the central input gate and separately resolves pointer and keyboard ownership before exposing continuous action state.
- `InputManager` runs before normal gameplay consumers so camera and placement code observe the current ownership-filtered state in the same frame.
- Left/right click and Escape callbacks resolve ownership at callback time, preventing an Editor interaction from leaking before the next gameplay `Update`.
- The gameplay save-menu Escape shortcut now consumes `InputManager.EscapePressed` instead of bypassing ownership through `Keyboard.current`.
- `EscapePressed` remains a one-frame press signal, so holding Escape cannot repeatedly toggle the save/load menu.
- In the Unity Editor, pointer input is owned only while the pointer is over Game View. Keyboard/gamepad movement and Escape are owned only while Game View has focus.
- An Editor-only `GameplayInputOwnershipEditorBridge` supplies current `EditorWindow.mouseOverWindow` and `focusedWindow` state through a generic runtime resolver API.
- Player/runtime builds register no Editor resolver and retain full input ownership by default. The runtime assembly contains no `UnityEditor` reference.
- The ownership rule applies automatically to the NPC debug harness, dungeon scenario window, and future Editor windows; no per-window enter/leave hooks were added.

## Validation Result

- Manual Unity validation completed successfully on 2026-08-14, including the Escape press/hold regression check for the save/load menu.

## Known Limitations

- Pointer ownership follows the window currently under the cursor. Dragging out of Game View intentionally stops gameplay pointer motion until the cursor returns.
- This ticket gates the existing gameplay input actions; it does not redesign bindings or introduce input-context stacks.

## Git

Suggested branch: `dev/DEV005-editor-input-isolation`

Active branch: `tool/dev005-editor-input-isolation` (`dev/` is unavailable because a branch named `dev` already occupies that Git ref namespace.)

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
