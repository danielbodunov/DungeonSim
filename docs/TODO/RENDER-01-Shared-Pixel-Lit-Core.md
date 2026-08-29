# RENDER-01 — Shared Pixel-Lit Core

## Tracking
- **ID:** RENDER-01
- **Status:** Planned
- **Milestone:** Pixel Rendering / Shared Shader Architecture
- **Depends on:** RENDER-00

## Goal
Extract the established pixel-art lighting behavior into reusable shader functions/subgraphs so terrain, props, traps, and characters can share one visual lighting model without inheriting terrain-specific atlas logic.

## Requirements
- Identify the current terrain shader behavior that defines the project's pixel-lit visual language.
- Extract reusable lighting functionality where practical, including dynamic-light response, diffuse stylization/quantization, ambient/shadow treatment, and the approved specular behavior from RENDER-00.
- Keep terrain-only atlas selection, height/world-position logic, and rotation-safe tile addressing outside the shared core.
- Preserve the existing terrain appearance after the refactor.
- Design the shared interface so ordinary UV0-based shaders can consume it later.
- Avoid unrelated shader cleanup or rendering redesign.

## Acceptance Criteria
- Terrain output matches the pre-refactor reference within expected numerical/rendering tolerance.
- Rotation-safe terrain atlas behavior is unchanged.
- Shared lighting code can be consumed without requiring terrain tile-selection inputs.
- Emission/specular behavior established in RENDER-00 remains functional.
- No duplicated copy of the core lighting calculation is required for the next prop shader.

## Out of Scope
- Prop shader implementation
- Character shader implementation
- FX shader implementation
- New visual features beyond those already established

## Manual Validation
Compare representative terrain tiles before and after the refactor under multiple dynamic-light positions, rotations, and material-mask values.

## Post-Implementation Report
Record shared shader assets/functions created, terrain shader changes, compatibility notes, validation results, and any intentionally terrain-specific logic left behind.

## Git
Suggested implementation branch: `render/render01-shared-pixel-lit-core`

Proceed according to `docs/AGENTS.md`.
