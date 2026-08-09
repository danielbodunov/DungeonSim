WORLD GENERATION AND BUILDING DESIGN
====================================

Purpose
-------
Define how the player expands, shapes, and upgrades the dungeon while keeping
tile generation, traversal, navigation, lighting, progression, and encounters
compatible with one another.

This is a design plan rather than a final specification. Reward values, balance
curves, tier dimensions, and the final presentation of Adventurer Aura still need
playtesting.


CORE PRINCIPLES
---------------

1. The player chooses the dungeon's meaningful structure.
   Tile generation may resolve compatible art, but it should not silently add
   ladders, platforms, encounters, or other strategically important objects.

2. Placement rules should be visible before committing a build.
   The preview should show footprint, socket requirements, cost, unlock state,
   and any invalid cells.

3. Logical layout and visual tile choice should be related but separate.
   A built cell establishes dungeon space and connections. Wide versus narrow
   controls how that space is represented and how much usable room it contains.

4. Larger structures must participate in the same placement and save systems as
   single-cell objects.

5. Expansion should be earned through adventurer activity.
   Exploration, danger, injury, and major victories generate Adventurer Aura,
   the single resource used for construction and dungeon level upgrades.

6. New depth and complexity should be introduced in controlled steps.
   Prefer discrete depth planes and authored transitions before considering fully
   free three-dimensional building.


CURRENT TECHNICAL BASELINE
--------------------------

- The dungeon layout is currently addressed as a two-dimensional Vector2Int
  cell grid.
- Tile socket profiles constrain which neighboring tile profiles can connect.
- Prop sockets provide authored locations for objects within a resolved tile.
- NPC routes are built from horizontal openings and ladder connections.
- Lighting is sampled over the two-dimensional cell grid and spreads through
  valid cell connections.
- Traps currently occupy one built cell.

These are useful foundations, but multi-cell structures and background depth
will require explicit data rather than being inferred only from cell position.


MANUAL TRAVERSAL PLACEMENT
--------------------------

Desired behavior:

- Ladders and platforms are selected from a Traversal section of the build
  palette.
- The player places them manually at compatible traversal sockets authored on
  the resolved tiles.
- World generation does not automatically create traversal objects merely
  because two potential sockets align.
- Traversal objects cannot be added, moved, or removed while an adventurer run is
  active. This keeps the navigation graph stable while NPCs depend on it.
- A placement preview snaps to the socket and shows whether it will create a
  usable navigation connection.
- Removing or replacing a tile must also validate traversal objects attached to
  its sockets.

Validation rules to consider:

- A ladder requires a compatible start and end socket, a clear vertical path,
  and valid standing space at both endpoints.
- A platform requires a compatible anchor socket and must not overlap an
  occupied structure volume.
- Socket direction, traversal type, allowed prefab tags, and optional size class
  should be checked before placement.
- An object that would strand an NPC or invalidate a required dungeon entrance
  route should be rejected or clearly warned about.

Suggested data direction:

- Give each placeable traversal prefab a build definition containing its palette
  icon, socket type, footprint/clearance, unlock requirement, and cost.
- Save the stable object ID, owning cell, socket ID, orientation, and any linked
  endpoint socket. Do not rely on prefab names or world position alone.
- Rebuild NPC navigation only after a placement transaction succeeds.


WIDE AND NARROW CELL CONTROL
----------------------------

Problem:

Dense painted layouts tend to resolve as broad connected rooms. The player needs
a way to preserve corridor-like spaces or explicitly request narrow construction.

Chosen placement modes:

- Auto: store automatic intent, then choose wide or narrow from local density and
  connection needs whenever the affected neighborhood is resolved.
- Wide: constrain newly resolved cells to wide-compatible tile profiles.
- Narrow: constrain newly resolved cells to narrow-compatible tile profiles.

The mode should be visible in the build palette and changeable before painting.
An optional conversion tool should allow selected existing cells to be changed
between Auto, Wide, and Narrow when compatible replacements exist.

Important rules:

- Store the player's width intent per cell separately from the selected tile
  prefab. Regeneration must not forget an explicit Wide or Narrow choice.
- Explicit intent overrides automatic density rules.
- Auto remains Auto after resolution. Nearby edits may deterministically
  re-resolve that cell, while explicit Wide and Narrow cells remain locked.
- Converting one cell may require re-resolving nearby unresolved or compatible
  neighbors, but should not unexpectedly replace distant built cells.
- Preview all affected cells and reject the conversion if no valid socket/profile
  solution exists.
- Decide whether wide and narrow profiles may connect directly, require a
  transition profile, or are incompatible.

Possible Auto heuristic:

- Prefer narrow when a cell has two opposite connections and dense neighboring
  construction would otherwise merge into a large room.
- Prefer wide at junctions, encounter spaces, entrances, and cells explicitly
  marked as rooms.
- Treat this only as a starting rule; authored constraints and player overrides
  remain authoritative.


MULTI-CELL STRUCTURES
---------------------

Examples include a spider nest, a large shrine, a boss-room centerpiece, or a
machine spanning several cells.

Each large structure should have:

- One anchor cell and an orientation.
- A footprint expressed as cell offsets from the anchor.
- Optional socket and clearance requirements for every occupied cell.
- A distinction between occupied cells, blocked cells, and effect-only cells.
- Placement cost, unlock requirement, category, and stable save ID.
- Rules for whether tiles beneath it remain traversable.

A multi-cell structure is purchased and saved as one structure. Its quoted cost
includes any unbuilt cells in its footprint, and committing it builds and reserves
the complete footprint atomically. The player does not construct those cells one
at a time first.

Placement flow:

1. Select the structure and orientation.
2. Preview the complete footprint.
3. Validate that all required cells exist, have compatible sockets, are unlocked,
   and are not reserved by another structure.
4. Display the complete cost and all cells that would be changed.
5. Commit the structure and its cell reservations as one transaction.

Spider nest example:

- The nest occupies more than one cell and reserves that footprint.
- It owns one or more spawn sockets or chooses valid adjacent cells at runtime.
- Spawn candidates must be built, reachable, unoccupied, and allowed by the
  encounter rules.
- If no adjacent spawn cell is valid, the spawn is delayed rather than appearing
  inside an invalid cell.
- Removing the nest cancels future spawns and cleans up only enemies it explicitly
  owns, according to the eventual encounter-lifecycle rules.

Spawned-enemy pursuit must be configurable per enemy or encounter definition.
Initial leash modes should include:

- Unrestricted: may pursue across any valid route.
- Radius: may pursue only within a configured graph or cell distance of its spawn.
- Vertical floor: may roam on its current vertical dungeon floor but may not use
  transitions to a different floor.

Depth-plane restrictions, if needed, should be a separate flag from vertical-floor
restrictions because background Z-depth and vertical dungeon floors are different
layout dimensions.


BACKGROUND DEPTH / Z-PLANES
---------------------------

Initial recommendation:

Use a small number of discrete depth planes, such as Foreground, Main, and
Background. Keep the current X/Y cell coordinates within each plane and add a
depth-plane ID. Connect planes only through explicitly authored depth-transition
sockets such as stairs, tunnels, ramps, or doorways.

This provides visible depth variation without immediately requiring arbitrary
3D pathfinding or overlapping cells with ambiguous ownership.

Systems affected:

- Building: selection, raycasts, previews, occupancy, and placement data must
  include the depth-plane ID.
- Navigation: graph nodes become (cell, depth plane); movement in Z occurs only
  along explicit transition edges.
- Rendering: sprites/meshes, selection highlights, and UI indicators need a clear
  sorting and occlusion policy.
- Camera: foreground and background planes remain visible simultaneously. Depth
  cues, selective transparency, and occlusion handling may improve readability,
  but the player should not have to switch planes off to see the full dungeon.
- Saving: every placed cell and object needs a persistent depth-plane field.
- Encounters: sight, attack range, trap effects, and spawning must state whether
  they cross planes.

Lighting direction:

- The current lighting grid is two-dimensional, so using one shared light value
  for stacked planes would cause light to leak between foreground and background.
- Store a separate light field for each depth plane.
- A light normally spreads only within its plane through valid cell connections.
- Depth-transition sockets may transmit a configurable fraction of light between
  planes.
- Render depth can still affect apparent intensity, fog, or tint, but it should
  not replace connectivity-based light propagation.

Before implementation, prototype two overlapping rooms on separate planes, one
depth transition, and one light source. Verify navigation, selection, occlusion,
and light leakage before expanding the content set.


BUILDING LIMITS AND PROGRESSION
-------------------------------

Building should be limited by several understandable gates:

- Adventurer Aura: paid when expanding into a new cell or placing an object.
- Unlock tree: grants new tile families, traversal objects, traps, rooms,
  structures, upgrades, and boss content.
- Dungeon level: controls unlock-tree tiers and maximum build reach.
- Build-layer limit: restricts vertical dungeon floors only. It does not count
  foreground/background Z-depth planes. Additional vertical sections unlock as
  the dungeon advances through progression tiers.
- Placement prerequisites: socket compatibility, adjacency, footprint, required
  supporting structures, and connection to the existing dungeon.

Recommended build transaction order:

1. Check unlock and dungeon-level requirements.
2. Check build bounds/layer limits.
3. Check sockets, footprint, occupancy, and reachability constraints.
4. Calculate and show the final cost.
5. Spend Adventurer Aura and commit all changes atomically.
6. Update generation, navigation, lighting, and saving.

If any step fails, no Adventurer Aura or partial placement should remain.


DUNGEON PROGRESSION AND ADVENTURER AURA
---------------------------------------

Adventurer experiences inside the dungeon generate Adventurer Aura. Aura is the
single spendable resource used for both building and purchasing permanent dungeon
level upgrades. Buying a level is irreversible; later spending cannot reduce the
dungeon's current level.

Potential reward events:

- An NPC enters a cell they have not previously explored.
- An NPC discovers a new room, depth plane, encounter, or major structure.
- A trap or enemy damages an NPC.
- An NPC is defeated inside the dungeon.
- A boss or other special encounter is completed.

Reward-shape direction:

- Exploration provides frequent, dependable, low-value income.
- Damage provides a smaller capped reward so repeated healing/damage loops cannot
  generate unlimited Aura.
- Defeating an NPC provides a large reward that scales sharply with NPC level.
- High-level defeats should be worth disproportionately more than low-level ones,
  but the exact curve should be data-driven and capped where needed.
- Rewards should be attributed once through a central event ledger to prevent
  double-awards from traps, enemies, and defeat handling.
- Building additional rooms and adding decor increase future Aura yield from
  explorers. The bonus should measure meaningful dungeon development, with caps
  or diminishing returns for duplicate low-cost decor to prevent farming.

Reward settlement:

- During a visit, qualifying events accumulate in a pending reward ledger.
- Pending Aura is deposited when the NPC leaves, is defeated, or is forcibly
  returned outside after becoming stranded.
- Defeat is not permanent death. It ends the current visit, returns the NPC to the
  outside population, and may apply recovery time or other consequences later.
- The defeat bonus and all visit activity are settled through the same ledger so
  the final trap hit, enemy attack, or exit cannot award twice.


BOSS ROOMS AND BOSSES
---------------------

Boss rooms should be deliberate, high-tier buildables rather than ordinary
random cells.

Proposed requirements:

- Unlock the boss-room tile family and a compatible boss separately or as a
  progression milestone.
- Reserve a multi-cell room footprint with entrance, arena-clearance, spawn, and
  reward sockets.
- Require at least one valid route from the dungeon entrance to the boss-room
  entrance.
- Prevent normal props, traps, and spawns from blocking required arena space.
- Start the encounter when the appropriate NPC or party crosses the room trigger.
- Keep doors, retreat rules, victory, defeat, cooldown, and reset behavior explicit.
- Scale the boss and resulting dungeon reward using the adventurers involved,
  dungeon level, and encounter tier.
- After the boss is defeated and its treasure is taken, seal the boss-room entrance
  for the remainder of the current open-dungeon session.
- The boss becomes available again in a later open-dungeon session and provides
  approximately the same base reward rather than a per-defeat diminishing reward.
- Session lockout, progression costs, and deeper-tier requirements provide the
  main limit on repeatedly farming the boss.

Provisional vertical-tier shape:

- Tier 1 begins with a vertical building section three floors high.
- Each newly unlocked section extends farther downward and is one floor taller
  than the previous section: 3, 4, 5, 6, then 7 floors through Tier 5.
- Exact horizontal bounds, unlock costs, and whether all five section heights are
  desirable remain subject to prototyping.

Boss rooms should eventually support parties, but the first version can validate
one boss, one room layout, and one or more NPCs using ordinary navigation.


PROPOSED IMPLEMENTATION PHASES
------------------------------

Phase 1 - Build definitions and validation

- Define a shared placeable/build-definition model.
- Add unlock, cost, footprint, socket, and stable-ID data.
- Make placement preview and commit use the same validation result.

Phase 2 - Manual traversal palette

- Add ladder and platform buttons to a Traversal palette category.
- Snap previews to compatible authored sockets.
- Save placed traversal objects and rebuild navigation after changes.

Phase 3 - Wide/narrow intent

- Add Auto, Wide, and Narrow placement modes.
- Store cell-width intent independently from the resolved profile.
- Re-resolve only the deterministic local Auto neighborhood after nearby edits;
  never rewrite an explicit Wide or Narrow choice.
- Add safe previewed conversion for existing cells.

Phase 4 - Progression and economy

- Add build costs and atomic spending.
- Add Adventurer Aura spending, permanent level upgrades, unlock tiers, and
  vertical-floor build limits.
- Award activity through a single ledger and add anti-farming rules.

Phase 5 - Multi-cell structures

- Add oriented footprints and cell reservations.
- Implement the spider nest as the first adjacency-based spawning test.
- Add unrestricted, radius, and vertical-floor pursuit modes.
- Extend save/load and removal behavior.

Phase 6 - Discrete depth-plane prototype

- Add a depth-plane ID to cells, navigation nodes, and saved placements.
- Add one authored transition and per-plane lighting data.
- Resolve camera, selection, render-order, and light-transmission behavior.

Phase 7 - Boss-room prototype

- Add one multi-cell boss-room definition and one boss encounter.
- Validate arena clearance, encounter lifecycle, party entry, and rewards.


INITIAL ACCEPTANCE CRITERIA
---------------------------

- Ladders and platforms appear in the palette and can only be placed on valid
  sockets.
- Generated or painted cells do not automatically receive traversal objects.
- Traversal editing is unavailable during an active adventurer run.
- The player can request Auto, Wide, or Narrow construction and explicit choices
  survive regeneration and save/load.
- Invalid conversions explain why they cannot be completed and change nothing.
- A multi-cell structure reserves its full rotated footprint and cannot overlap
  another reservation.
- Placing a multi-cell structure pays once and atomically builds all required
  footprint cells.
- A spider nest spawns only into valid adjacent cells.
- Locked or unaffordable placements cannot be committed or partially charged.
- Dungeon activity accumulates Aura once per qualifying event and deposits it
  when the NPC exits, is defeated, or is forcibly returned from a stranded state.
- Adventurer Aura can purchase construction or an irreversible dungeon level.
- Higher-level NPC defeats produce substantially higher rewards according to a
  visible, data-driven curve.
- Overlapping depth planes do not share navigation or light unless an explicit
  transition permits it.
- A boss room remains reachable and can complete a full start-to-reset encounter
  cycle.
- Defeating and looting a boss seals that room for the rest of the current session
  without permanently killing the participating NPCs.


INCORPORATED DECISIONS
----------------------

- Wide and Narrow use different usable footprints within the same logical cell
  size; they do not create different grid scales.
- Auto width intent persists and may re-resolve after nearby edits.
- Traversal editing is locked during active adventurer runs. A genuinely stranded
  NPC is forcibly returned outside after a configurable timeout.
- A multi-cell structure is one purchase whose cost includes its required cells.
- Enemy pursuit constraints are configurable rather than universal.
- Foreground and background depth planes are visible simultaneously.
- Build-layer progression refers to vertical dungeon floors, not Z-depth planes.
- Adventurer Aura is the single resource spent on building and dungeon leveling.
- NPC defeat is non-permanent and settles the visit's pending Aura.
- A looted boss room seals for the current session and resets in a later session
  without a diminished base reward.

See the individual records in [Design Decisions](../Decisions/README.md) for the
context and consequences of these choices.


OPEN DESIGN QUESTIONS
---------------------

1. What may pass through a connection between foreground and background depth
   planes? For example, if a doorway or ramp connects the Main and Background
   planes:

   - Does torchlight spill through and illuminate the other plane?
   - Can an archer shoot or a spell travel through the opening?
   - Can an area attack affect targets on both sides?

   Recommended starting rule: every depth transition separately declares whether
   it permits navigation, line of sight/projectiles, area effects, and a percentage
   of light transmission. Ordinary unconnected planes share none of these.

2. How large should the local neighborhood be when an Auto-width cell re-resolves,
   and when should an otherwise valid result be frozen to avoid visible churn?

3. What stranded timeout feels fair, and should a forced return apply any recovery
   delay or Aura penalty?

4. What caps, uniqueness rules, or diminishing returns should apply to the room
   and decor Aura bonus?

5. Are the proposed Tier 1 through Tier 5 section heights of 3, 4, 5, 6, and 7
   floors correct, and how wide is each unlocked section?

6. Does the boss room reset whenever the dungeon opens again, after an elapsed
   cooldown, or only after another explicit preparation step?
