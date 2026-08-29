# RENDER-04 — Pixel-Lit Character Shader

## Tracking
- **ID:** RENDER-04
- **Status:** Planned
- **Milestone:** Pixel Rendering / Characters
- **Depends on:** RENDER-01

## Goal
Create a dedicated character shader that uses the shared Pixel-Lit lighting model while remaining ready for later character-specific runtime effects.

## Requirements
- Consume the shared Pixel-Lit core from RENDER-01.
- Support ordinary UV0 character textures with point sampling.
- Support base tint and the established material response needed for character surfaces.
- Work correctly on skinned meshes.
- Keep the initial implementation intentionally minimal: establish visual parity/coherence before adding status and combat effects.
- Provide a clean extension point for later hit flash, status tint, selection/highlight, emission, and death/dissolve behavior without requiring those systems now.

## Acceptance Criteria
- A moving/skinned character renders correctly under representative dungeon dynamic lighting.
- Character lighting is visually coherent with terrain and Pixel-Lit props.
- Character textures remain crisp and stable during animation.
- No temporary-state material duplication system is introduced.
- Terrain and prop shaders remain unaffected.

## Out of Scope
- Hit flash
- Poison/frozen/status effects
- Selection highlighting
- Dissolve/death effects
- Character gameplay logic

## Manual Validation
Place a representative skinned character beside terrain and Pixel-Lit props, animate it, and inspect it under moving lights and shadows from several viewing angles.

## Post-Implementation Report
Record shader assets/properties, test character used, skinned-renderer compatibility, visual comparison notes, and requirements discovered for runtime character effects.

## Git
Suggested implementation branch: `render/render04-pixel-lit-character`

Proceed according to `docs/AGENTS.md`.
