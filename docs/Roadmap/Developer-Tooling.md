# Developer Tooling Roadmap

## Purpose

Create practical Unity testing tools that make gameplay scenarios quick to reproduce while continuing to exercise normal production systems.

## Immediate milestone — Complete

The initial testing-environment milestone is complete:

- DEV001 — Generic Prop & Treasure Placement — **Complete**
- DEV002 — Reusable Dungeon Test Scenarios — **Complete**
- DEV003 — NPC Runtime Debug Harness — **Complete**

DEV001's targeted cross-layout generated-prop/floor-prop regression was validated in the Unity Editor. The original developer-tooling gate before t007 is therefore satisfied.

## Observation & Control milestone

A second tooling pass improves day-to-day observation and debugging before returning to gameplay-loop work:

- DEV004 — NPC Camera Focus & Follow — **Ready**
- DEV005 — Editor Window Input Isolation — **Planned**
- DEV006 — Selective Simulation Pause — **Planned**

### Direction

Camera focus/follow should be implemented as production-capable camera functionality exposed through developer tooling. It is expected to become useful for normal player-facing NPC selection and observation later.

Editor input isolation and selective simulation pause are developer infrastructure unless later promoted into explicit gameplay features.

Selective simulation pause should distinguish presentation/observation from gameplay simulation rather than globally freezing Unity. The architecture may later support simulation-speed controls and single-step debugging, but those features are outside DEV006.

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
