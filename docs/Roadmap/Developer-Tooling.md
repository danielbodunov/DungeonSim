# Developer Tooling Roadmap

## Purpose

Create practical Unity testing tools that make gameplay scenarios quick to reproduce while continuing to exercise normal production systems.

## Immediate milestone

The immediate testing-environment milestone is substantially implemented:

- DEV001 — Generic Prop & Treasure Placement — **Awaiting Unity Validation**
- DEV002 — Reusable Dungeon Test Scenarios — **Complete**
- DEV003 — NPC Runtime Debug Harness — **Complete**

DEV001 still requires the targeted cross-layout regression validation for stale generated-prop occupancy. Once that passes, the immediate developer-tooling gate is complete and gameplay work can resume with t007.

## Future tooling

Later tooling may include:

- AI navigation/debug visualization
- NPC decision history
- Scenario NPC initial conditions
- Scenario assertions
- Automated scenario runner

The initial one-floor-prop-per-cell rule from DEV001 is intentionally conservative rather than the final placement model. Future content may require several ordinary props in one cell. When that need becomes concrete, evolve the generic placement system toward multiple placement records using local offsets, footprints, generic anchors, or a similarly bounded approach rather than adding content-specific sockets solely to work around the one-prop limit. Topology-sensitive content should continue using dedicated sockets or equivalent authored constraints where alignment and traversal semantics require them.
