# Developer Tooling Roadmap

## Purpose

Create practical Unity testing tools that make gameplay scenarios quick to reproduce while continuing to exercise normal production systems.

## Testing Environment — Complete

- DEV001 — Generic Prop & Treasure Placement — **Complete**
- DEV002 — Reusable Dungeon Test Scenarios — **Complete**
- DEV003 — NPC Runtime Debug Harness — **Complete**

## Observation & Control — Complete

- DEV004 — NPC Camera Focus & Follow — **Complete**
- DEV005 — Editor Window Input Isolation — **Complete**
- DEV006 — Selective Simulation Pause — **Complete**

Camera focus/follow is production-capable functionality exposed through developer tooling and may later support normal player-facing NPC observation. Input isolation and selective simulation pause remain developer infrastructure unless explicitly promoted into gameplay features.

## Scenario Reliability / Entrance Rules

- DEV007 — Scenario Default Entrance Compatibility — **Ready**
- DEV008 — Single Authoritative Entrance Placement — **Planned**

DEV007 fixes scenario validation/apply when the dungeon relies on its normal/default entrance rather than a manually authored entrance. DEV008 establishes a 0-or-1 entrance invariant: a valid newly placed entrance replaces the previous one, while manual removal remains available.

## Future tooling

Later tooling may include:

- AI navigation/debug visualization
- NPC decision history
- Scenario NPC initial conditions
- Scenario assertions
- Automated scenario runner
- Simulation speed controls / single-step debugging

## Future generic prop placement

The initial one-floor-prop-per-cell rule from DEV001 is intentionally conservative rather than the final placement model. Future content may require several ordinary props in one cell. When that need becomes concrete, evolve the generic placement system toward multiple placement records using local offsets, footprints, generic anchors, or a similarly bounded approach rather than adding content-specific sockets solely to work around the one-prop limit. Topology-sensitive content should continue using dedicated sockets or equivalent authored constraints where alignment and traversal semantics require them.
