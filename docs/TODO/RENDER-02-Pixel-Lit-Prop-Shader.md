# RENDER-02 — Pixel-Lit Prop Shader

## Tracking
- **ID:** RENDER-02
- **Status:** Planned
- **Milestone:** Pixel Rendering / Props and Traps
- **Depends on:** RENDER-01

## Goal
Create the general-purpose Pixel-Lit shader for socket props, traps, decorations, and interactable environment objects using ordinary model UVs while matching the established terrain lighting style.

## Requirements
- Consume the shared Pixel-Lit lighting core from RENDER-01.
- Use conventional UV0 sampling suitable for Blender-authored prop UVs and prop atlases.
- Support point-sampled base color.
- Support base tint.
- Support the approved emission, roughness, metallic, and specular model where applicable.
- Support optional alpha clipping for appropriate pixel-art props.
- Keep trap gameplay semantics out of the shader; traps and ordinary props should share this rendering path when their surface requirements match.
- Do not import terrain tile-selection, height-selection, or rotation-safe atlas-addressing logic into the prop shader.

## Acceptance Criteria
- Representative wood, stone, and metal props visually belong with the terrain under the same dynamic lights.
- A trap can use the same shader without trap-specific shader logic.
- Point-filtered prop textures remain crisp at the intended texel density.
- Emission and specular behave consistently with the terrain implementation.
- Alpha-clipped geometry renders correctly on a representative asset.
- Terrain rendering remains unchanged.

## Out of Scope
- Character-specific rendering/effects
- Gameplay interaction highlighting
- Final prop-atlas authoring conventions
- FX/transparency shader beyond alpha clipping

## Manual Validation
Test at minimum a wood object, metal object, hanging/socket decoration, and spike/mechanical trap beside representative terrain under moving dynamic lighting.

## Post-Implementation Report
Record shader/material assets created, exposed properties/defaults, test assets used, visual differences from terrain if intentional, and follow-up requirements for the prop-atlas pipeline.

## Git
Suggested implementation branch: `render/render02-pixel-lit-props`

Proceed according to `docs/AGENTS.md`.
