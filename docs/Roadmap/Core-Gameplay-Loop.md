# Core Gameplay Loop Roadmap

## Purpose

Turn DungeonSim's existing generation, building, NPC traversal, trap, Aura, persistence, and progression foundations into a repeatable player-facing loop:

**Build → Adventurer enters → Explore → Encounter / Treasure → Decide whether to continue → Return or fail → Settle reward → Build / upgrade → Next expedition**

The design goal is not simply to make the dungeon lethal. The player should benefit from creating a dungeon that entices adventurers to explore deeply, exposes them to meaningful risk, and still allows successful expeditions often enough to generate progression.

Ticket IDs are stable. Do not renumber or reuse them if priorities change.

---

## Slice A — Expedition Loop

**Milestone outcome:** An adventurer can enter through an authored entrance, explore, discover treasure, resolve it, return through familiar space, and settle the expedition reward.

### t001 — NPC Traversal Memory
**Status:** Complete

NPCs retain personal traversal knowledge and use familiar connections for return routing.

### t002 — Dungeon Entrance / Adventurer Spawn Contract
**Status:** Complete

Establish an authoritative semantic dungeon entrance/spawn contract. Place the entrance object through a compatible authored socket, with the component/socket—not the host tile type—as the gameplay contract.

Depends on: t001

### t003 — Point-of-Interest Foundation
**Status:** Complete

Create the minimum common investigation-target model needed for treasure and future meaningful cell interactions. Avoid building a generalized encounter framework beyond current needs.

Depends on: t002

### t004 — Treasure Prop + Treasure Socket
**Status:** Complete

Allow authored treasure props to occupy appropriate sockets, expose themselves as points of interest, carry a reward value, and transition from unresolved to resolved state.

Depends on: t003

### t005 — NPC Treasure Discovery & Investigation
**Status:** Ready

Connect treasure to NPC investigation behavior so treasure creates a meaningful stop and resolves through the investigation flow.

Depends on: t004

### t006 — NPC Carried Treasure / Visit Reward
**Status:** Planned

Track treasure or equivalent expedition reward locally during a visit so successful exit and failure can produce different outcomes.

Depends on: t005

### t007 — Expedition Vertical Slice Validation
**Status:** Planned

Integrate and validate entrance → exploration → treasure → investigation → familiar return → exit → reward settlement. Add minimal functionality only where required to make the slice coherent.

Depends on: t002–t006

---

## Slice B — Meaningful Exploration

**Milestone outcome:** NPC exploration varies intelligently according to personal knowledge instead of appearing scripted at junctions.

### t008 — Frontier Classification
**Status:** Planned

Classify outgoing exploration choices using personal memory: unexplored, partially explored, familiar transit, and exhausted/dead-end.

Depends on: t007

### t009 — Weighted Junction Selection
**Status:** Planned

Prefer useful unexplored choices while varying equivalent branches with controlled weighted randomness. Retain a choice long enough to prevent oscillation and expose reasoning in debug output.

Depends on: t008

### t010 — Known Treasure Exploration Goal
**Status:** Planned

Allow known unresolved treasure to influence exploration priority without granting knowledge of undiscovered treasure or unknown routes.

Depends on: t009

### t011 — Exploration Vertical Slice Validation
**Status:** Planned

Validate repeated exploration across varied generated layouts, including branch variation, meaningful stopping, treasure-directed behavior, dead-end avoidance, and familiar return.

Depends on: t008–t010

---

## Slice C — Player Economy Loop

**Milestone outcome:** Expedition outcomes generate a resource the player can spend to meaningfully alter the next dungeon/expedition.

### t012 — Treasure / Player Economy Settlement
**Status:** Planned

Define and implement how successfully returned treasure becomes persistent player value. Decide whether treasure remains distinct from Aura or converts into the existing progression resource during the prototype.

Depends on: t007

### t013 — Atomic Build Costs
**Status:** Planned

Add costs to build definitions and charge the selected resource through an atomic placement transaction: affordability → valid placement → charge, with no partial state on failure.

Depends on: t012

### t014 — Dungeon Level Purchase UI
**Status:** Planned

Expose the existing irreversible dungeon-level purchase hooks through minimum viable player-facing UI and communicate current level, next cost, and result.

Depends on: t012

### t015 — Economy Vertical Slice Validation
**Status:** Planned

Validate build → expedition → reward → spending → dungeon improvement → next expedition as one coherent loop. Stop and tune here if the loop is not understandable or rewarding before expanding feature breadth.

Depends on: t012–t014

---

## Slice D — Risk / Reward Decisions

**Milestone outcome:** Dungeon depth and danger create meaningful pressure on adventurer exploration and player dungeon design.

### t016 — Treasure Placement Rules
**Status:** Planned

Add simple treasure-generation/placement constraints such as valid sockets, minimum entrance distance, chance, and dungeon-level scaling without introducing full itemization.

Depends on: t015

### t017 — Risk-Scaled Treasure Value
**Status:** Planned

Increase potential treasure value based on appropriate dungeon depth/difficulty factors so deeper or more dangerous expeditions can justify their risk.

Depends on: t016

### t018 — Return-Stamina Estimation
**Status:** Planned

Give NPCs a basic estimate of familiar return cost and allow retreat before total stamina exhaustion rather than relying exclusively on free return at zero stamina.

Depends on: t011

### t019 — Reward vs Survival Behavior
**Status:** Planned

Allow carried reward to influence willingness to continue exploring versus returning safely, introducing a small goal-driven decision layer before a full personality system.

Depends on: t017, t018

### t020 — Core Gameplay Loop Validation
**Status:** Planned

Validate the complete core loop and assess whether building choices, NPC decisions, treasure, traps, retreat, success/failure, reward settlement, and progression produce understandable and interesting decisions. Use findings to define the next roadmap rather than automatically expanding into combat, parties, inventory, or deep personality systems.

Depends on: t015–t019

---

## Deferred Until the Core Loop Is Proven

Unless required by an earlier ticket, defer:

- party formation and detailed social behavior;
- full equipment/inventory systems;
- loot rarity and itemization;
- full combat;
- sophisticated personality architecture;
- multiple procedural depth planes;
- large visual-polish passes;
- elaborate UI;
- advanced trap-disarming systems.

These can multiply a strong gameplay loop, but should not substitute for establishing one.

## Gameplay Loop Review Questions

At each vertical-slice validation ticket, ask:

- Does the player understand what changed and why?
- Can the player understand why the NPC made its important decisions?
- Are NPC stops reserved for meaningful events?
- Does deeper exploration create meaningful opportunity and risk?
- Is a successful return valuable and legible?
- Does failure create tension without making progress feel arbitrary?
- Does the resulting reward give the player an interesting next decision?
- Does spending/upgrading materially change a later expedition?
