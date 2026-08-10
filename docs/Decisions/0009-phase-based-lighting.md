# 0009: Phase-Based Lighting

- Status: Accepted
- Date: 2026-08-09

## Context

Atmospheric darkness makes the open dungeon more evocative, but it interferes with reading cells, sockets, and placement previews while building.

## Decision

Expansion uses uniform, neutral construction lighting across the buildable dungeon. Opening the dungeon transitions back to its normal ambient, static, and dynamic atmospheric lighting.

## Consequences

- Lighting presentation follows the gameplay phase.
- The normal light field should remain available while the uniform presentation is active.
- A brief visual blend should prevent a harsh flash when phases change.
- The construction presentation must preserve selection and placement-feedback colors.

See [Phase-Based Lighting](../Design/Visual_and_Interaction_Design.md#phase-based-lighting).
