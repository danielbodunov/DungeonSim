# Capability-Gated Traversal

## Purpose

Define a long-term traversal model in which adventurers do not all perceive the dungeon as the same navigation graph. A connection may be known to an NPC yet remain unusable, costly, or risky because of that character's physical capabilities.

The goal is to make dungeon geometry, adventurer archetypes, personal knowledge, and route choice interact meaningfully.

## Core Principle

A route should eventually be evaluated through four distinct questions:

**knowledge × capability × risk × reward**

1. **Knowledge:** Does this NPC know the connection exists?
2. **Capability:** Can this NPC physically use it?
3. **Risk:** How difficult, costly, or dangerous is it for this NPC?
4. **Reward:** Does using it advance the NPC's current goal enough to justify the cost or danger?

Global navigation knowledge must never automatically become character knowledge.

## Traversal Capabilities

Connections should be able to express requirements or costs beyond simple walkability.

Potential traversal types include:

- ordinary corridor — broadly accessible;
- small gap — basic jumping capability;
- large gap — stronger athletics requirement;
- climbable ledge — athletics/strength requirement;
- ladder — common capability with time/stamina cost;
- drop — easy or safe in one direction but difficult/impossible to reverse;
- crawl/squeeze passage — size/agility requirement;
- damaged or unstable passage — increased risk;
- locked or mechanically blocked route — future tool/skill capability;
- supernatural barrier — future magical capability.

Do not implement all of these at once. Jumping/gaps are the most natural first extension because they also fit the proposed Embodied Dungeon Mode.

## Athletic Adventurers

Athletic adventurers should be able to discover and use traversal options unavailable to less capable characters.

Example:

```text
                 long ordinary route
              ┌──────────────────────┐
Entrance ──────┤                      ├──── Treasure
              └────── GAP ───────────┘
                       ↑
                athletic shortcut
```

A normal adventurer may know both sides of the dungeon but still be unable to use the gap connection. An athletic adventurer that has discovered the gap may treat it as a viable shortcut.

This allows adventurer capability to create different expedition stories and prevents one dungeon layout from functioning identically for every visitor.

## Knowledge and Capability Are Separate

Discovery must remain personal.

An NPC should not reason:

> There is an athletic shortcut elsewhere in the dungeon, therefore I can use it.

It may only reason about a special connection after learning enough about that connection through actual exploration or future perception systems.

The decision sequence should remain conceptually:

```text
Does the NPC know the connection?
        ↓
Does the NPC meet its requirements?
        ↓
What does traversal cost / risk?
        ↓
Does it improve the current goal enough to use?
```

Two adventurers can therefore discover the same gap and retain different conclusions:

- Adventurer A: known and traversable.
- Adventurer B: known but currently unusable.

## Deterministic First, Risky Later

Initial capability-gated traversal should use reliable thresholds.

If an adventurer qualifies for an authored basic jump, the traversal should succeed consistently. Do not introduce random traversal failure merely to create variety.

A later system may distinguish comfortable and risky capability ranges, for example:

- comfortable jump distance;
- maximum attempted jump distance;
- stamina/injury modifiers;
- willingness to attempt a risky crossing.

This could eventually allow emergent decisions such as a wounded adventurer attempting a dangerous shortcut while fleeing with valuable treasure.

Risky traversal should be introduced only after route reasoning is inspectable enough for the player and developer to understand why the NPC attempted it.

## Interaction with Route Scoring

Capability-gated traversal should integrate with the planned scored route-choice system rather than becoming a separate pathfinder.

A viable candidate route may eventually include values such as:

- exploration novelty;
- treasure/reward attraction;
- route distance;
- stamina cost;
- known danger;
- traversal difficulty;
- carried-loot value;
- return safety;
- character-specific capability fit.

A traversal connection the NPC cannot perform should normally be excluded from viable candidates rather than merely assigned a poor score.

A risky-but-possible connection may remain a candidate with an appropriate penalty.

## Strategic Dungeon Design

Capability-gated traversal should give the player reasons to create geometry beyond simple straight corridors.

Possible uses include:

- athletic shortcuts that bypass a heavily trapped ordinary route;
- one-way drops that pull adventurers deeper but complicate escape;
- climbing routes that expose adventurers to different hazards;
- alternate routes aimed at different adventurer archetypes;
- bait positioned so more capable adventurers can reach it differently;
- layouts where a capability that helps during exploration creates a dangerous return choice.

The player should sometimes discover that a dungeon design is strong against one type of adventurer but weak against another.

This creates a useful management question:

> Which adventurers does this dungeon geometry reward, filter, expose, or accidentally empower?

## Shared World Traversal Vocabulary

The proposed Embodied Dungeon Mode and NPC traversal should operate on the same physical world concepts without requiring the same movement controller.

Both player and adventurer systems should understand authored concepts such as:

- floors;
- gaps;
- ladders;
- ledges;
- drops;
- platforms;
- traversable openings;
- future special traversal surfaces.

The player may execute movement through real-time controls such as WASD + jump, while NPCs evaluate authored traversal connections, choose a route, and execute an appropriate traversal behavior.

A player walking through the dungeon should therefore be able to visually understand why a route is or is not accessible to a particular adventurer.

## Debug and Observability Requirements

When capability-gated traversal is implemented, debug tooling should expose enough information to distinguish navigation failures from intentional decisions.

Useful information includes:

- connection type;
- whether the NPC knows it;
- required capability;
- NPC capability value/category;
- viable / impossible / risky classification;
- estimated traversal cost;
- risk modifier;
- route score contribution;
- selected or rejected reason.

Do not allow a special traversal connection to silently disappear from routing without an inspectable reason.

## Relationship to Adventurer Motivation

Capability-gated traversal belongs naturally alongside the future Adventurer Motivation & Risk work.

The intended long-term decision model is not merely pathfinding. It is:

**personal knowledge × physical capability × perceived danger × expected reward × current condition**

Health, stamina, carried treasure, personality, fear, and voluntary retreat can later influence whether a capable adventurer actually chooses to use a route.

## Implementation Timing

This is a design direction, not an immediate implementation ticket.

Revisit it when the project begins the adventurer motivation/navigation slice and when tile traversal can meaningfully represent gaps, jumps, climbs, or similar authored special connections.

A reasonable first prototype would use:

1. one authored jump/gap connection type;
2. one simple athletic capability threshold;
3. deterministic traversal success;
4. personal discovery/knowledge rules;
5. debug visibility for viable versus rejected connections;
6. one dungeon scenario containing an ordinary route and an athletic shortcut.

Do not build a generalized RPG skill system as a prerequisite.
