VISUAL AND INTERACTION DESIGN
=============================

Purpose
-------

Define how lighting, selection, camera focus, progression information, and menus
communicate the dungeon's current state. The interface should make building easy
to read while preserving atmosphere during adventurer runs.


CORE PRINCIPLES
---------------

1. Building clarity takes priority during Expansion.
   The player should be able to read cells, sockets, footprints, and placement
   previews without fighting darkness or localized light falloff.

2. Atmosphere returns when the dungeon opens.
   Exploring should use the dungeon's authored lights and deliberately dramatic
   darkness rather than the uniform construction view.

3. Selection should connect information to the world.
   Selecting an NPC, trap, or spawner should reveal its details, visually identify
   it, and bring it into camera focus.

4. Progression should always be understandable.
   Dungeon level progress and spendable Adventurer Aura should be visible without
   opening a menu. Detailed unlock paths may live in dedicated progression menus.

5. Linear dungeon growth and optional build choices are different systems.
   Dungeon tiers use a readable linear milestone track. Traps, spawners, and decor
   use a branching technology tree.

6. High contrast should come from intentional values and shapes.
   Lighting should not rely only on broad additive glow. Darkness, lit regions,
   silhouettes, and interaction highlights should remain visually distinct.

7. UI styling should come from one shared visual language.
   Menus, HUD panels, inspectors, buttons, bars, tooltips, and popups should use a
   common theme asset instead of defining colors and spacing independently.


CURRENT TECHNICAL BASELINE
--------------------------

- `GameplayLoopController` already exposes Expansion and Exploring phases.
- `DungeonLightingManager` owns ambient color, a grid light texture, static and
  dynamic contributions, and Legacy, Smooth2x, and Smooth4x quality presets.
- `CameraFollow` supports smooth panning and distance-based perspective dolly
  zoom without changing field of view, but has no explicit focus-session API.
- `GameplayLoopUI` creates the current HUD and build palette at runtime.
- The build palette exposes Auto, Narrow, Wide, and Toggle Wall controls, but the
  tools do not yet provide object-footprint previews or wall-edge hover tooltips.
- NPC dodge, damage, and defeat outcomes can create short-lived world-space
  action feedback popups.
- `GameplayLoopUI` currently contains hard-coded colors and dimensions, so a theme
  asset will need to replace those values gradually.
- Adventurer Aura, pending visit Aura, and dungeon level are already available to
  the HUD and save system.


MAIN MENU AND GAME ENTRY
------------------------

The game should open to a main menu before entering the dungeon scene or active
simulation.

Primary options:

- Continue
- New Game
- Load Game
- Options
- Quit, for standalone builds

Continue:

- Load the most recently used valid save slot.
- Disable the button when no valid save exists.
- Display the save name and last-played time as secondary text when space permits.
- If the most recent save is missing or unreadable, fall back to the next valid
  recent save and report the problem non-destructively.

New Game:

- Start from a clean dungeon, default progression state, and newly generated
  adventurer roster.
- Ask for confirmation when starting a new game would abandon unsaved progress.
- Do not delete existing save slots. The first save of the new game should create
  or overwrite a slot only after the player explicitly chooses it.
- A later version may add seed, difficulty, or starting-theme choices, but the
  first flow should remain quick.

Load Game:

- Open the existing named save-slot browser.
- Show save name, last-played time, dungeon level/tier, Aura, adventurer count,
  and built-cell count.
- Handle incompatible or unreadable saves with a clear disabled/error state rather
  than silently removing them.
- Return to the main menu without loading when Back is selected.

Options:

- Open a dedicated options panel without starting a game.
- Initial categories should include Audio, Display, Gameplay, Controls, and
  Accessibility.
- Apply display and accessibility preview changes immediately where safe.
- Provide Apply, Revert, and Restore Defaults behavior for changes that need
  confirmation.
- Store global options separately from dungeon save data so they apply across all
  save slots.

Menu navigation:

- Support pointer, keyboard, and controller navigation from the beginning.
- Give the initially focused button an obvious state.
- Escape/Back closes a submenu before offering to quit.
- Avoid starting or simulating the dungeon behind the main menu.
- Use short transitions, but never delay input merely to finish decorative motion.


SHARED UI THEME
---------------

Create a `UITheme` ScriptableObject that acts as the visual source of truth for
runtime-created UI and authored menu prefabs.

Theme tokens should include:

- Core colors: background, panel, elevated panel, text, muted text, accent,
  positive, warning, harmful, disabled, selection, and focus.
- Typography: heading, body, numeric, and optional decorative font assets, plus
  standard size/style presets.
- Controls: normal, hovered, pressed, selected, focused, and disabled colors or
  sprites for buttons, toggles, tabs, sliders, and input fields.
- Surfaces: panel, tooltip, modal, inspector, and menu background sprites/materials.
- Progress displays: health, stamina, Aura, pending Aura, cooldown, and locked
  progress colors/sprites.
- Layout metrics: common padding, spacing, corner radius, border width, row height,
  button height, and inspector width.
- Motion: standard short/medium transition durations, popup lifetime, focus zoom
  duration, and easing presets where practical.
- World feedback: selection outline/ring, valid placement, invalid placement,
  dodge, damage, defeat, and interaction-highlight colors.

Usage rules:

- UI code requests semantic tokens such as `Warning` or `Panel`, not raw color
  values copied into each component.
- `GameplayLoopUI`, the main menu, inspector, progression menus, action popups,
  and future tooltips should all consume the same assigned theme.
- Provide a project default theme through a stable Resources location or a small
  UI settings asset. Individual scenes should not need to find themes by name.
- Components may expose a narrow local override only when the difference has a
  semantic purpose. Overrides should fall back to the shared theme.
- Missing themes or missing optional assets should use a readable built-in fallback
  and log one clear warning rather than breaking the interface.
- Runtime theme switching should refresh existing UI without requiring scene reload
  if alternate themes or accessibility themes are introduced later.

Initial theme variants to plan for:

- Default dungeon theme
- High-contrast accessibility theme
- Color-vision-safe variant or token override set

The theme controls presentation only. It must not contain gameplay costs, unlock
rules, localized text, or button behavior.


BUILD PLACEMENT PREVIEWS AND EDGE AFFORDANCES
---------------------------------------------

Placement preview behavior:

- Hovering a buildable cell shows a highlighted preview before placing a dungeon
  tile, trap, or spawner.
- Multi-cell or adjacency-based objects highlight their complete affected
  footprint rather than only the anchor cell.
- Valid previews use the shared theme's valid-placement treatment; invalid or
  blocked previews use its invalid-placement treatment and explain the reason.
- The preview updates immediately when the hovered cell, selected buildable,
  orientation, cost, unlock state, or local tile solution changes.
- Preview validation and committed placement consume the same authoritative
  result so the highlight cannot promise a placement that the click rejects.

Toggle Wall hover behavior:

- While the Toggle Wall tool is active, highlight the nearest editable shared
  edge rather than highlighting an entire cell.
- Show a compact tooltip next to the pointer describing the pending action:
  `Add Wall` for an open edge or `Remove Wall` for a closed edge.
- If the edge cannot be changed, show an unavailable treatment and a concise
  reason, such as requiring two adjacent built cells or having no valid local
  tile solution.
- Offset the tooltip from the pointer so it does not obscure the edge, clamp it
  within the screen, and hide it immediately when no eligible edge is hovered or
  the tool is exited.
- Use shared UI-theme tokens for tooltip surfaces, text, valid highlights,
  invalid highlights, and unavailable states.


PHASE-BASED LIGHTING
--------------------

Expansion lighting:

- Use a uniform, neutral work-light presentation across the buildable dungeon.
- Make placed cells, unresolved cells, grid boundaries, sockets, and previews easy
  to distinguish.
- Do not let missing torches or room topology make a cell too dark to edit.
- Preserve enough material color and surface detail to judge what is being built.
- Selection highlights and invalid-placement colors must remain readable against
  the uniform light.

Exploring lighting:

- Restore normal ambient darkness and the grid-propagated static/dynamic lights.
- Allow traps, NPC light sources, room lights, and occlusion to control atmosphere.
- Keep essential HUD and action feedback readable independently of world light.

Transition behavior:

- `DungeonLightingManager` should expose an explicit presentation mode such as
  `ExpansionUniform` and `ExploringAtmospheric`.
- `GameplayLoopController` should set the lighting mode whenever its phase changes.
- The expansion mode should bypass or override world-light sampling rather than
  destroying the normal light field. Returning to Exploring should not require
  reconstructing every light unless the layout changed.
- Fade between modes over a short unscaled duration, approximately 0.2 to 0.5
  seconds, to avoid a harsh flash.
- Save gameplay lighting configuration, not the temporary blend position.

Initial implementation direction:

- Add a global shader blend between uniform construction light and the existing
  sampled dungeon light.
- Keep the current texture and light-source data alive during Expansion.
- Rebuild the normal light field only when layout or source changes require it.


STYLIZED ATMOSPHERIC LIGHTING
-----------------------------

Problem:

The current smooth additive falloff can appear broadly glowy. A more stylized
approach should produce stronger separation between light and shadow without
discarding the existing connectivity-aware light field.

Recommended first prototype: stepped high-contrast lighting.

- Quantize sampled brightness into three to five deliberate value bands.
- Lower and neutralize ambient light so unlit rooms read as shadow rather than a
  colored glow.
- Tighten source falloff and limit additive saturation where lights overlap.
- Preserve colored-light identity primarily in brighter bands.
- Optionally add subtle screen-space or shader dithering between bands if hard
  boundaries shimmer during camera movement.
- Keep a narrow readable floor/silhouette minimum so NPCs are not completely lost.

Alternatives worth comparing:

- Hard stepped/cel lighting: graphic and readable, but transitions can look abrupt.
- Posterized gradient ramp: more art-directable, but requires a maintained ramp
  texture and careful color handling.
- Very low ambient plus tight smooth lights: easiest extension of the current
  system, but may still retain some glow.
- Hybrid: tight smooth propagation followed by mild quantization. This is the
  recommended comparison candidate because it preserves spatial falloff while
  increasing contrast.

The lighting-quality setting and the visual-lighting style should be separate.
Sample density controls resolution and performance; stylization controls how the
sampled value is rendered.


SELECTION AND INSPECTION
------------------------

Selectable object types for the first version:

- Adventurer NPC
- Trap
- Enemy spawner or spawning structure

Selection behavior:

- A normal click selects the highest-priority selectable object under the pointer
  when no placement/removal tool is consuming that click.
- The selected object receives a clear outline, ring, or other non-light-dependent
  highlight.
- Clicking empty dungeon space, pressing Escape, or selecting another object ends
  the current inspection.
- Destroying, despawning, or unloading the selected object closes the inspector
  safely.
- Selection is inspection-only during Exploring. During Expansion, later versions
  may expose upgrade or configuration actions after ordinary inspection works.

Common inspector information:

- Display name and object type
- Current state
- Owning cell or location
- Level/tier, if applicable
- Short description
- Relevant status effects, warnings, or disabled reasons

NPC details:

- Name, level, and personal experience
- Current and maximum health/stamina
- Strength, Dexterity, Luck, and Intelligence
- Current behavior state and destination
- Party membership and leader, when parties exist
- Cells explored, pending visit Aura contribution, and recent important action

Trap details:

- Trap name and description
- Damage, difficulty, and calculated dodge context
- Ready/triggered/resetting state
- Cooldown and remaining cooldown
- Upgrade level and future upgrade effects

Spawner details:

- Spawned enemy type and encounter tier
- Active enemy count and maximum count
- Spawn interval/cooldown and current state
- Pursuit mode, radius, vertical-floor constraint, and owning structure
- Blocked-spawn reason when no adjacent cell is valid

Inspector layout direction:

- Use a docked side window rather than placing a large panel over the selected
  object.
- Keep the most frequently changing values near the top.
- Use compact labels and bars for health, stamina, cooldown, and spawn capacity.
- Avoid displaying debug-only data unless a debug-details toggle is enabled.


CAMERA FOCUS AND FOLLOW
-----------------------

When an object is selected:

1. Save the camera's previous free position, zoom, and follow state.
2. Smoothly center the selected object with an offset that leaves room for the
   inspector window.
3. Zoom to a configurable inspection size rather than snapping to a fixed scale.
4. Highlight the object after focus begins so the relationship is unmistakable.

For an NPC:

- Continue following the NPC until the selection is cleared or replaced.
- Manual pan input may adjust a temporary follow offset, but should not silently
  cancel selection or tracking.
- Manual zoom remains available within inspection limits.

For a trap or spawner:

- Focus once and remain centered on the static object.
- The player may pan away, but the object remains selected until explicitly
  cleared. A "refocus" control may return to it.

When focus ends:

- Smoothly restore the previous free-camera zoom.
- Preserve the current visible region where practical; do not unexpectedly jump
  across the whole dungeon if the player panned while inspecting.
- If the selected object disappears, clear focus automatically.

`CameraFollow` should gain explicit methods such as `BeginFocus`, `SetFollowOffset`,
`Refocus`, and `EndFocus` rather than allowing UI code to directly mutate camera
fields.


PROGRESSION HUD
---------------

Persistent top bar:

- Place a dungeon-level progress bar across the upper center of the screen.
- Label it with the current dungeon level and the Aura cost of the next level.
- Fill represents current spendable Adventurer Aura divided by the next level's
  cost. Because Aura is the single resource for building and leveling, purchasing
  a buildable may lower this bar. Purchased dungeon levels never regress.
- During Exploring, optionally show pending visit Aura as a differently colored
  preview segment beyond the banked fill.

Top-right build budget:

- Extend or replace the current top-right phase card with a compact build-budget
  panel.
- Always show current spendable Adventurer Aura.
- During placement preview, show object cost and projected remaining Aura.
- Show insufficient funds clearly before placement is attempted.
- Include current dungeon level and unlocked vertical tier.
- During Exploring, replace placement cost with pending Aura and session status.

Recommended hierarchy:

    Dungeon Level 2                         Build Budget
    [Aura progress toward Level 3       ]   Aura: 145
                                             Selected: Spike Trap (-20)
                                             Remaining: 125

The bar and numeric Aura display serve different purposes: the bar communicates
progress toward a level purchase, while the exact number supports building
decisions.


PROGRESSION MENUS
-----------------

Use two related but distinct menus.

Dungeon Tier Milestones:

- A linear progression view representing major dungeon expansion milestones.
- Start with three implemented tiers.
- Show the current tier, completed requirements, remaining requirements, Aura
  cost, unlocked vertical space, and major feature unlocks.
- Let the player preview later tiers even when they are locked.
- Initial section-height direction is Tier 1 = 3 floors, Tier 2 = 4 floors, and
  Tier 3 = 5 floors. The data model should permit later Tier 4 and Tier 5 content.

Buildable Technology Tree:

- A branching unlock tree for optional construction choices.
- Initial branches: Traps, Spawners, and Decor.
- Each node shows icon, name, description, prerequisites, dungeon-level/tier gate,
  Aura cost, and unlock state.
- Locked downstream nodes remain visible so the player can plan.
- Purchasing a node is permanent and cannot reduce dungeon level.
- Use stable node IDs and data assets so unlock state survives renaming and save/load.

Menu interaction:

- Provide separate buttons for Tiers and Buildables rather than combining both
  progressions into one dense screen.
- Both menus may be opened during Expansion.
- During Exploring they may be viewable as read-only, but purchases and dungeon
  restructuring remain disabled.
- Opening a progression menu should pause camera input beneath it. Whether it also
  pauses the simulation remains an open decision.
- Confirm purchases that consume a large percentage of current Aura and always
  show the remaining balance.


PROPOSED IMPLEMENTATION PHASES
------------------------------

Phase 1 - Shared UI theme foundation

- Define the `UITheme` ScriptableObject and semantic tokens.
- Create a readable default theme and fallback values.
- Migrate the existing runtime HUD, palette, save browser, and action popups away
  from hard-coded styling.

Phase 2 - Main menu and options shell

- Add Continue, New Game, Load Game, Options, and standalone Quit actions.
- Reuse the named save browser for Load Game.
- Add global option persistence and keyboard/controller navigation.
- Prevent dungeon simulation from starting behind the main menu.

Phase 3 - Lighting presentation modes

- Add uniform Expansion lighting and atmospheric Exploring lighting.
- Connect mode changes to `GameplayLoopController`.
- Add a short shader blend and verify that the normal light field remains intact.

Phase 4 - Build interaction feedback and selection foundation

- Add highlighted placement previews for dungeon tiles, traps, and spawners.
- Add shared-edge highlighting and pointer-following Add Wall/Remove Wall
  tooltips for the Toggle Wall tool.
- Make preview and commit share one validation result.
- Define a common selectable/inspectable contract.
- Add pointer selection, deselection, highlight state, and selection priority.
- Implement NPC, trap, and spawner detail providers.

Phase 5 - Camera inspection mode

- Add focus-session APIs to `CameraFollow`.
- Support one-time focus for static objects and persistent follow for NPCs.
- Handle manual offset, zoom, refocus, deselection, and despawn safely.

Phase 6 - Progression HUD

- Add the top dungeon-level/Aura bar.
- Add the top-right build-budget panel.
- Show pending Aura during Exploring and placement-cost projections during
  Expansion.

Phase 7 - Three-tier milestone menu

- Define data-driven tier requirements and rewards.
- Implement the linear Tier 1 through Tier 3 view.
- Save tier unlock state separately from current dungeon level where needed.

Phase 8 - Buildable technology tree

- Define stable technology-node data and prerequisites.
- Add Traps, Spawners, and Decor branches.
- Add permanent unlock purchases and save/load support.

Phase 9 - Stylized lighting comparison

- Prototype stepped, tight-smooth, and hybrid lighting on the same dungeon scene.
- Compare readability, atmosphere, motion stability, and performance.
- Select one style before producing final lighting assets and tuning values.


INITIAL ACCEPTANCE CRITERIA
---------------------------

- Starting the game opens the main menu without running an adventurer session.
- Continue loads the most recent valid save and is disabled when none exists.
- New Game creates a clean game without deleting existing save slots.
- Load Game exposes the named save browser and returns safely to the main menu.
- Options persist globally and can be navigated with pointer, keyboard, or
  controller.
- The main menu, HUD, inspectors, progression screens, and action popups consume
  semantic values from one shared `UITheme`.
- Changing a theme token updates all relevant UI without editing individual panels.
- Entering Expansion makes all editable cells uniformly readable regardless of
  placed lights.
- Opening the dungeon restores atmospheric lighting without losing or rebuilding
  unaffected light data.
- An NPC, trap, or spawner can be selected only when a build tool is not consuming
  the click.
- The inspector shows type-appropriate live information and closes safely if the
  selected object disappears.
- Selecting an NPC smoothly focuses and follows it until selection is cleared.
- Selecting a trap or spawner focuses it without forcing permanent camera follow.
- The top HUD shows dungeon level, progress toward the next level, banked Aura,
  and pending Aura during a visit.
- Placement preview shows cost and projected remaining Aura before committing.
- Dungeon tiles, traps, and spawners show a valid or invalid highlighted footprint
  before placement.
- Toggle Wall hover highlights the affected shared edge and shows an Add Wall or
  Remove Wall tooltip beside the pointer.
- The Tier menu presents three linear dungeon tiers.
- The technology tree presents separate Trap, Spawner, and Decor branches.
- Progression purchases are disabled during Exploring.
- The selected atmospheric lighting style produces clearly darker shadows and
  stronger value contrast than the current smooth-glow presentation.


INCORPORATED DECISIONS
----------------------

- The main menu includes Continue, New Game, Load Game, and Options.
- Continue loads the most recently used valid save.
- UI presentation is controlled by a shared, data-driven theme object.
- Expansion uses uniform construction lighting; Exploring restores normal dungeon
  lighting.
- Selecting an NPC, trap, or spawner opens an inspector and focuses the camera.
- NPC camera focus follows the selected NPC until deselection.
- A dungeon-level progress bar occupies the top of the screen.
- Spendable Adventurer Aura and build-budget information appear at the top right.
- Dungeon tiers and buildable technologies use separate menus.
- The first progression implementation contains three dungeon tiers.

See [Design Decisions](../Decisions/README.md) for the corresponding records.


OPEN DESIGN QUESTIONS
---------------------

1. Should New Game immediately enter the default dungeon, or first show a minimal
   setup screen for save name, seed, or difficulty?

2. Should Continue track the most recently loaded slot, the most recently saved
   slot, or simply the newest valid save timestamp?

3. Which options require confirmation and automatic timeout/revert behavior,
   especially resolution and display-mode changes?

4. Should theme selection itself be exposed in Options, or should only
   accessibility variants be player-selectable?

5. Does "build budget" mean only spendable Adventurer Aura, or should dungeon
   level eventually impose a separate maximum construction-capacity budget?

6. Should the top progress bar use current spendable Aura, meaning construction
   lowers level progress, or should the earlier one-resource decision be revised
   to track non-spendable lifetime progression separately?

7. Should progression menus pause an active adventurer run or remain read-only
   over a running simulation?

8. Should manual panning while following an NPC create a persistent camera offset,
   temporarily suspend centering, or snap back after input stops?

9. Which inspector values should be visible to the player versus restricted to a
   debug-details mode?

10. Should selecting a built trap or spawner during Expansion immediately expose
   upgrade/configuration controls, or should inspection and editing remain separate
   modes?

11. Which atmospheric lighting prototype—stepped, tight smooth, or hybrid—best fits
   the intended art direction?

12. How bright should the minimum NPC silhouette remain in an otherwise unlit room?

13. Should the construction-light transition happen instantly for responsiveness
   or retain the proposed short fade?

14. When multiple depth planes overlap under the pointer, how does selection
    priority identify the intended NPC or structure?
