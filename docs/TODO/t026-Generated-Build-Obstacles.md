# t026 — Generated Build Obstacles

## Tracking
- **ID:** t026
- **Status:** Planned
- **Milestone:** Strategic Construction
- **Depends on:** t023; compatible with t021–t022 construction-space rules

## Goal
Introduce deterministic generated obstacles into the starting unbuilt ground so dungeon construction must respond to existing terrain constraints rather than always expanding through an empty uniform grid.

## Design Intent
Build obstacles should create spatial planning problems before the player places the first corridor. They occupy one or more unbuilt ground cells and make those cells unavailable for ordinary dungeon construction or trap service-space use unless a future mechanic explicitly allows removal/exploitation.

Examples include:
- boulders / rock formations;
- skeleton or bone deposits;
- magical relic formations;
- ore/mineral deposits;
- other biome/theme-specific visual variants.

These may share the same logical footprint/behavior while presenting different authored visuals.

## Core Model
Separate obstacle gameplay identity from its visual variant.

```text
GeneratedBuildObstacleDefinition
- stable obstacle kind / ID
- footprint shape
- build/service blocking rules
- visual variant pool

GeneratedBuildObstacleInstance
- definition ID
- anchor cell
- orientation if applicable
- resolved footprint cells
- selected visual variant
```

A visual variant should not need to redefine the gameplay footprint unless it is actually a different obstacle definition.

## Footprints
Support representative obstacle footprints occupying 1–4 cells.

Initial authored shapes should include enough variety to prove the model, for example:
- 1 cell;
- 2-cell horizontal/vertical;
- 3-cell line or L shape;
- 4-cell 2x2 footprint.

Do not assume every N-cell obstacle is rectangular. Footprint offsets should be explicit and rotation-safe if rotation is supported.

## Generation Rules
- Obstacles are generated only in eligible starting/unbuilt ground cells.
- The entire footprint must validate before placement.
- Generation must not overlap:
  - starting entrance requirements;
  - existing built/traversable dungeon cells;
  - fixed/border cells that cannot host the obstacle;
  - generated props/content with conflicting authority;
  - another generated build obstacle;
  - reserved trap/service space when generation occurs in a context where such reservations already exist.
- Placement must be deterministic for a given generation seed/configuration.
- Generation should avoid producing a starting state that prevents viable dungeon expansion. Add conservative placement rules rather than requiring a sophisticated solvability optimizer in the first implementation.

## Construction Integration
Obstacle footprint cells are authoritative construction blockers.

Normal dungeon construction must reject attempts to build through them.

Trap mechanism/infrastructure/service footprints must also reject those cells unless a future obstacle-specific rule explicitly permits interaction.

The player should be able to understand the blocked footprint visually without relying on debug UI.

## Visual Variants
Allow one logical obstacle definition to select from multiple compatible visual prefabs/variants.

Example:

```text
Logical footprint: 2-cell blocking formation

Possible visual variants:
- large boulder
- collapsed skeleton pile
- magic crystal/relic
- exposed ore deposit
```

Visual variants must preserve the same logical footprint and blocking semantics for that definition.

Variants may be weighted and/or theme-tagged later, but do not build a full biome/theme system in this ticket.

## Persistence / Scenarios
Generated obstacles are part of authoritative dungeon/world state.

- Save/load must restore the same obstacle definitions, footprints, and selected visual variants.
- DungeonTestScenario capture/reset must reproduce the same obstacle layout exactly.
- Do not reroll obstacle variants or positions merely because a save/scenario is restored.

## Preview / Debugging
Provide enough editor/debug visibility to inspect:
- obstacle anchor;
- resolved footprint cells;
- obstacle definition/variant;
- reason a construction/service placement is blocked.

This may reuse existing grid/debug visualization rather than introducing a large new tool.

## Future Hooks
Do not implement these now, but keep the model compatible with later obstacle interactions such as:
- spending resources to excavate/remove boulders;
- mining ore deposits;
- harvesting magical relics;
- disturbing skeletons or other encounter-bearing obstacles;
- obstacles that become valuable strategic resources rather than permanent blockers.

These possibilities are one reason obstacle gameplay identity should remain separate from its visual prefab.

## Acceptance Criteria
- Seeded generation can place authored 1-, 2-, 3-, and 4-cell obstacle footprints in eligible ground.
- Obstacles never partially place; the full footprint validates before mutation.
- Obstacles do not overlap each other or authoritative starting content.
- Normal construction rejects obstacle footprint cells without mutating the dungeon or spending resources.
- Trap service/mechanism/infrastructure placement rejects obstacle footprint cells.
- The blocked footprint is visually understandable in normal gameplay view.
- Multiple visual variants can represent the same logical obstacle footprint without changing its blocking behavior.
- Save/load restores exact obstacle positions and variants.
- Scenario capture/reset restores exact obstacle positions and variants.
- A fixed seed/configuration produces deterministic obstacle results.
- Generation retains at least a conservative viable expansion area around required starting dungeon content.

## Out of Scope
- Player obstacle removal/mining
- Resource rewards from obstacles
- Obstacle combat/encounters
- Full biome/theme generation
- Procedural obstacle meshes
- Advanced global solvability optimization

## Post-Implementation Notes
Document:
- obstacle definition/instance authority;
- footprint representation;
- deterministic generation rules;
- construction/trap validation integration;
- persistence/scenario representation;
- how visual variants are selected;
- future removal/resource hooks exposed by the implementation.

## Git
Suggested branch: `feature/t026-generated-build-obstacles`
