# Core Gameplay Loop Roadmap

## Purpose

DungeonSim is a dungeon-management simulation in which the player acts as a sinister force cultivating a dungeon to attract and manipulate adventurers.

Core loop:

**Build & bait → Adventurers enter → Explore / suffer / steal → Escape or die → Harvest Dread / recover spoils / lose bait → Improve dungeon → Attract more valuable prey**

See `docs/Design/Core_Game_Direction.md` for the standing game-direction principles.

Treasure is dungeon-owned bait, not an automatic player reward. Death is valuable but should not necessarily become the only desirable expedition outcome. Future survivor/reputation systems may make escapes useful even when they carry treasure away.

Tower-defense-style raids remain a possible future bonus mode and should not drive current core architecture.

Ticket IDs are stable and are not renumbered when the roadmap changes.

---

## Slice A — Sinister Dungeon Expedition

### t001–t010
**Status:** Complete

Traversal memory, entrance/spawn contract, POIs, treasure discovery/custody, death recovery, successful escape loss, Dread harvesting, and authoritative expedition outcomes are implemented.

### t011 — Sinister Dungeon Vertical Slice Validation
**Status:** Planned

Validate the complete bait → explore → steal → escape/die → harvest/recover loop and assess whether expeditions create understandable consequences and small emergent stories.

---

## Slice B — Physical Consequences & Dungeon Economy

**Status:** Planned after t011 validation

Make expedition consequences materially visible and establish distinct supernatural and physical economies.

### t012 — Visible Adventurer Carried Loot
Show stolen treasure/loot on adventurers through a generic carried-loot representation.

### t013 — Physical Death Loot Drops
Materialize recoverable death loot at the death location as a persistent world object.

### t014 — Adventurer Loot Rediscovery
Allow later adventurers to discover and steal loot left by previous expeditions.

### t015 — Player Recovery Phase
Allow the player to deliberately recover remaining physical loot between expeditions.

### t016 — Dread Spend & Dungeon Growth Foundation
Use Dread for supernatural progression such as dungeon growth, unlocks, build depth, bait generation, traps/spawners, or similar powers. Keep Dread distinct from mundane construction materials. Treasure manifestation is the first proving purchase.

### t017 — Adventurer Physical Resource Drops
Prototype broad physical resources brought by adventurers, such as construction materials, trap components, and arcane components.

### t018 — Build Cost Foundation
Use physical resources to constrain selected construction actions and prove a spend/build logistics loop.

---

## Slice C — Strategic Construction

**Status:** Planned; details may be revised by t011 and Slice B findings

Dungeon growth should be constrained by space, resources, and future intent. Corridors should be planned not only for traversal, but also for traps, bait, recovery, infrastructure, and future expansion.

### t019 — External Trap Attachment Model
Move trap mechanisms outside traversable dungeon space and require compatible floor/wall/ceiling service regions.

### t020 — Rotatable Trap Placement
Make trap orientation and hazard direction explicit placement decisions.

### t021 — Modular Tile Construction Surfaces
Evolve tile prefabs toward controlled floor/ceiling/wall/opening/service modules sufficient for physical trap installation without becoming voxel construction.

### t022 — Trap Space & Compatibility Validation
Reserve and validate complete trap mechanism/service footprints separately from their hazard volume.

### t023 — Strategic Building Vertical Slice
Validate whether spatial and resource constraints produce meaningful dungeon-planning tradeoffs instead of unconstrained creative tile painting.

---

## Economy Direction

Keep three concepts distinct unless later validation proves a better model:

- **Dread:** supernatural growth, manifestation, progression, and unlocks.
- **Physical resources:** construction, trap fabrication, upgrades, and other material logistics.
- **Treasure:** dungeon-owned bait and risked wealth used to attract adventurers.

Avoid prematurely expanding physical resources into a detailed crafting inventory. Begin with broad categories and add granularity only when gameplay requires it.

## Later Gameplay Areas

After these slices, likely areas include meaningful exploration, weighted junction selection, known-treasure/greed pressure, return-stamina estimation, fear/non-death Dread sources, personality, survivor reputation/notoriety, and story-facing expedition histories.

## Deferred / Future Modes

Defer party/social expansion, full inventory/itemization, broad combat expansion, sophisticated personalities, major polish, advanced trap disarming, and tower-defense-style organized raids until the management loop is proven.

Tower-defense-style raids may later exist as a bonus/alternate mode using shared dungeon systems, but are not part of the core progression loop.
