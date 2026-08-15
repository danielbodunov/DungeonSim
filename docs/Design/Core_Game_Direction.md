# Core Game Direction

## Player Fantasy

The player is a sinister force inhabiting and cultivating a dungeon.

The player primarily shapes the dungeon to attract, manipulate, exploit, and sometimes kill adventurers who enter seeking wealth, glory, or discovery. The player may also physically manifest within the dungeon during preparation, maintenance, recovery, and other local interactions; this embodied role supports dungeon management rather than replacing it with a conventional action-game loop.

The dungeon should feel like an active predatory ecosystem rather than a static maze or conventional tower-defense board.

## Core Gameplay Loop

**Build and bait → attract adventurers → observe/manipulate expeditions → harvest soul energy and recover spoils → expand/improve the dungeon → attract more valuable prey**

The optimal dungeon should not simply kill every adventurer as quickly as possible. Long, dangerous, dramatic expeditions can create more value and more interesting consequences than immediate death.

## Treasure Is Bait

Treasure placed in the dungeon is fundamentally dungeon-owned value risked to entice adventurers.

- Available treasure attracts or rewards exploration.
- An adventurer who takes treasure carries it out of dungeon ownership.
- If that adventurer dies, the dungeon can recover its treasure and potentially acquire valuables the adventurer brought with them.
- If that adventurer escapes, the carried treasure is genuinely lost from the dungeon.

This creates a risk/reward decision for the player: valuable bait may draw better or greedier adventurers deeper, but it can also be stolen.

Treasure should therefore not automatically convert into player currency merely because an NPC finds it.

## Soul Energy / Aura

Soul energy/Aura is the dungeon's supernatural progression resource.

The long-term direction is for adventurer activity inside the dungeon—not only death—to potentially generate harvestable energy. Examples may eventually include exploration, fear, pain, traps, combat, magic use, or death.

Exact generation rules should be introduced incrementally and balanced through later tickets. Death can provide a substantial harvest without becoming the only valuable expedition outcome.

## Adventurer Outcomes

An expedition should eventually have explicit outcomes such as:

- escaped successfully;
- retreated;
- defeated/killed;
- potentially other story-relevant outcomes later.

These outcomes can affect treasure ownership, loot, Aura, reputation, and future adventurer behavior differently.

## Adventurers as Story Generators

NPC knowledge, personality, greed, fear, injuries, discoveries, survival, and death should create small emergent stories.

Examples:

- A greedy adventurer pushes one room farther for visible treasure and dies to a trap.
- A wounded survivor escapes with dungeon treasure.
- A cautious adventurer retreats early and may later contribute to the dungeon's reputation.
- A powerful adventurer penetrates unusually deep before becoming valuable prey.

Systems should favor understandable decisions and observable consequences so these stories are legible to the player.

## Survival Is Not Necessarily Failure

An adventurer escaping should not automatically mean the player failed.

A future reputation/notoriety system may make survivors useful because they spread stories of danger and treasure, attracting stronger or wealthier adventurers. This creates tension between harvesting an adventurer now and allowing a survivor to increase future opportunity.

Reputation is a future direction, not a requirement of the current expedition tickets.

## Dungeon Management Focus

The core game remains dungeon management/simulation.

Likely long-term player tools include:

- dungeon topology and expansion;
- treasure/bait placement;
- traps;
- monsters/minions;
- rooms and specialized dungeon functions;
- resource investment;
- manipulation of adventurer choices and risk;
- observation/debug-like information presented as player-facing dungeon knowledge;
- embodied local interaction for construction, installation, recovery, repair, and inspection.

Each system should be evaluated partly by how it helps the player influence adventurer behavior.

## Capability-Gated Traversal

Different adventurers should not necessarily perceive the dungeon as the same usable navigation graph. Physical capabilities may make a discovered connection viable, risky, or impossible for one NPC while another can use it confidently.

The long-term traversal decision model is:

**knowledge × capability × risk × reward**

This creates room for athletic adventurers to jump gaps or use traversal shortcuts unavailable to others, and for dungeon geometry to filter or favor different adventurer types. Discovery remains personal: global navigation knowledge must never automatically become character knowledge.

See `docs/Design/Capability_Gated_Traversal.md` for the detailed design direction.

## Overview and Embodied Dungeon Modes

The long-term player experience may expose two complementary interfaces onto the same dungeon simulation.

### Overview / Planning Mode

The existing side-view dungeon presentation remains the macro-management interface. It supports broad planning, expansion, inspection, economy management, and observation of expeditions.

### Embodied Dungeon Mode

The player can enter the dungeon as a controllable manifestation/entity while retaining the same side-view visual language. The camera becomes substantially more zoomed in and follows the player character through the dungeon.

Initial movement direction:

- WASD-based character movement;
- Space to jump;
- normal gravity and grounded traversal;
- contextual vertical traversal such as ladders where appropriate.

The embodied character should obey the dungeon's spatial rules rather than functioning as a free camera. It should allow the player to physically experience corridor lengths, elevation, service access, room scale, and other consequences of dungeon layout.

Potential embodied interactions include:

- recovering treasure and physical resources after expeditions;
- installing and rotating traps from valid local positions;
- repairing or maintaining dungeon mechanisms;
- placing or interacting with treasure/bait;
- inspecting local dungeon state;
- interacting with future construction infrastructure.

The exact player manifestation is intentionally unresolved. It may represent the sinister dungeon force itself, a keeper-like avatar, summoned servant, possessed minion, or another thematic embodiment.

### Shared-System Requirement

Overview and Embodied modes are two interfaces onto **one production simulation**, not separate implementations of dungeon management.

Both should use the same underlying systems for:

- placement validation;
- trap compatibility;
- construction costs;
- inventory/resources;
- dungeon state;
- save/load;
- interactions and recoverable loot.

For example, Overview Mode may designate or preview a trap location while Embodied Mode may require local installation, but both must resolve through the same authoritative trap-placement rules.

### Strategic and Logistical Purpose

Embodied interaction should reinforce management rather than turn DungeonSim into a conventional platformer or action RPG. Its value is making construction, maintenance, recovery, and dungeon geometry spatially meaningful.

This direction may support a preparation/expedition/aftermath rhythm:

**Plan and prepare → run expedition → inspect aftermath → physically recover/repair/install → expand and prepare again**

It also creates room for future logistics progression. Early dungeon management may require more direct player labor, while later minions, infrastructure, workshops, or supernatural upgrades could automate repetitive tasks.

Service spaces and maintenance routes may eventually become strategically important alongside adventurer-facing corridors, especially for externally mounted traps and other infrastructure.

### Scope Guardrails

Embodied Dungeon Mode should not automatically imply:

- elaborate player combat;
- RPG equipment/stat systems;
- precision platforming as a core pillar;
- a separate first-person/third-person game mode;
- duplicated construction or interaction systems.

Prototype the embodied interaction model when strategic construction and physical recovery systems are mature enough to benefit from it rather than implementing a standalone character controller without supporting gameplay.

## Tower-Defense-Style Raids

Large organized raids remain a possible future bonus/alternate mode.

They may reuse dungeon construction, traps, monsters, NPC navigation, and adventurer-party systems, but they are not currently part of the core progression loop and should not drive near-term architecture.

Do not introduce raid-specific abstractions into core systems unless a future ticket explicitly activates this direction.

## Design Principles

1. **The player manipulates adventurers rather than directly controlling them.**
2. **NPCs act from their own knowledge.** Unknown treasure or routes should not influence them magically.
3. **Treasure is an investment and lure, not free reward generation.**
4. **Death is valuable but should not always be the only optimal outcome.**
5. **Escapes, retreats, discoveries, and deaths should create understandable stories.**
6. **Dungeon layout should meaningfully influence NPC choices and outcomes.**
7. **Adventurer route choice may depend on personal knowledge, physical capability, risk, and reward.**
8. **Add complexity only when it strengthens the core management loop.**
9. **Overview and embodied interaction must operate on the same authoritative dungeon systems.**
10. **Embodied interaction should make dungeon management spatial and tactile, not redefine the game as an action platformer.**
11. **Preserve room for future raids without designing the core game around tower defense.**
