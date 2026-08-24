# t023 — Strategic Building Vertical Slice

## Tracking
- **ID:** t023
- **Status:** Complete
- **Milestone:** Strategic Construction
- **Depends on:** t018–t022 as applicable

## Goal
Validate that dungeon construction now creates meaningful spatial and economic decisions rather than feeling like unconstrained creative tile painting.

## Core Question
Does the player need to plan dungeon growth around traversal, future traps, service space, resources, bait, and expansion intent?

## Result
The current strategic-building foundation is directionally successful and is sufficient to close this validation ticket.

Physical construction costs and external trap/service-space requirements now create meaningful reasons to plan corridor growth instead of treating dungeon construction as unrestricted tile painting. Trap orientation, compatibility, and multi-cell reservations establish real spatial constraints, and the player can deliberately leave room for future mechanisms.

The validation also exposed presentation and world-generation gaps that should be handled as dedicated follow-up tickets rather than expanding this validation ticket.

## Validated Findings

### Construction economy creates a useful planning constraint
Construction Materials make expansion compete with other future uses of physical resources. The current broad-resource model is sufficient as a foundation; detailed balancing can continue after more of the loop is playable.

### Trap service space meaningfully affects layout planning
External trap placement and multi-cell mechanism/infrastructure reservations create a meaningful distinction between a corridor that merely fits traversal and a corridor deliberately planned for hazards.

Retrofitting traps into tightly built areas can fail because the required external space is unavailable. This is desirable strategic pressure rather than an implementation defect.

### Trap placement rules are substantially more understandable
Service-cell-first placement, automatic orientation, valid-side cycling, complete footprint previews, and compatibility validation make the trap's logical requirements understandable during placement.

### Reserved trap space lacks sufficient persistent visual representation
Although mechanism/infrastructure cells are authoritative and block incompatible construction, the normal dungeon presentation does not yet clearly show that those cells have been consumed by trap machinery.

A large trap can reserve multiple ground cells while those cells continue to look like ordinary solid terrain. This weakens the player's ability to read the spatial cost of previous construction decisions without debug visualization.

This is a presentation gap, not a reservation/validation gap.

**Follow-up:** t025 — Trap Construction Presentation.

### t021 provides the correct surface hooks but not trap integration
`t021` established `TileConstructionSurfaces` and a safe distinction between `VisualOnly` module swaps and `RequiresTopologyResolution` changes.

However, no integration layer currently translates an authoritative trap placement into target-corridor surface variants or external service-space presentation.

The desired direction is to use t021 for visual-only target-surface changes such as trap openings, grates, wall slits, or hatches while keeping topology authority on the tile/socket/traversal systems.

**Follow-up:** t025 — Trap Construction Presentation.

### Starting ground remains too unconstrained
Even with construction costs and trap-service requirements, the unbuilt starting ground is largely uniform and empty. This means the player's initial expansion problem is still more permissive than the intended strategic-building fantasy.

Generated physical obstacles can create pre-existing spatial constraints that force the player to route around terrain and make individual starting layouts more strategically distinct.

**Follow-up:** t026 — Generated Build Obstacles.

## Strategic Building Model After Validation

```text
Construction Materials
→ constrain how much dungeon can be built

Dungeon topology
→ constrains traversal and future expansion

Trap service/mechanism footprints
→ constrain where hazards can physically fit

Trap construction presentation (t025)
→ makes those spatial costs readable in the finished dungeon

Generated build obstacles (t026)
→ create pre-existing terrain constraints the player must plan around
```

## Recommended Follow-Ups
- **t024 — Rotation-Safe Tile Textures:** continue visual authoring support for rotation-safe modular dungeon presentation.
- **t025 — Trap Construction Presentation:** make trap surface changes and service-space occupation physically visible using the t021 surface contract where appropriate.
- **t026 — Generated Build Obstacles:** add deterministic 1–4 cell starting-ground obstacles with interchangeable visual variants and authoritative build blocking.

## Deferred Questions
- Exact resource balance between dungeon expansion, traps, and later upgrades.
- Whether some obstacles can eventually be excavated, harvested, mined, or otherwise converted into opportunities.
- How much service/infrastructure detail should be visible in the final 2.5D art direction.
- Which topology-changing construction actions eventually deserve their own explicit transaction model.

## Output
Validated strategic-building behavior and exposed the next presentation/world-generation requirements. No additional runtime implementation is required to close t023 itself.

## Git
No implementation branch is required for closure; findings were recorded directly in planning documentation.
