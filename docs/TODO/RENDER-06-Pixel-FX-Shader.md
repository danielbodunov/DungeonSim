# RENDER-06 — Pixel FX Shader

## Tracking
- **ID:** RENDER-06
- **Status:** Planned
- **Milestone:** Pixel Rendering / Effects
- **Depends on:** RENDER-01

## Goal
Provide a focused shader path for emissive, cutout, and transparent pixel-art effects such as flames, magic, particles, and other non-opaque surface effects.

## Requirements
- Preserve crisp point-sampled pixel artwork.
- Support emissive output appropriate for fire/magic-style visuals.
- Support alpha clipping and, where required, transparent rendering.
- Reuse shared visual conventions where they are meaningful without forcing opaque terrain/prop lighting behavior onto unlit or primarily emissive effects.
- Keep the shader narrowly scoped rather than accumulating character/prop features.

## Acceptance Criteria
- Representative flame/magic artwork remains crisp at runtime.
- Emissive effects remain readable in dark dungeon lighting.
- Alpha clipping works without unintended filtering artifacts.
- Transparent mode, if implemented, behaves predictably with the project's rendering pipeline.

## Out of Scope
- Particle-system authoring
- Gameplay spell systems
- Automatic light generation
- Post-processing bloom configuration unless separately required

## Manual Validation
Test representative cutout and emissive/transparent effects in a dark dungeon scene and against dynamically lit geometry.

## Post-Implementation Report
Record shader modes/properties, rendering-pipeline limitations, sorting concerns, validation assets, and follow-up VFX work.

## Git
Suggested implementation branch: `render/render06-pixel-fx`

Proceed according to `docs/AGENTS.md`.
