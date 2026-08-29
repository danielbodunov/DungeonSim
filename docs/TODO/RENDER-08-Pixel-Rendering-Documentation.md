# RENDER-08 — Pixel Rendering Pipeline Documentation

## Tracking
- **ID:** RENDER-08
- **Status:** Planned
- **Milestone:** Pixel Rendering / Documentation
- **Depends on:** RENDER-03, RENDER-07

## Goal
Document the finalized pixel-art rendering pipeline so future terrain, prop, trap, character, and FX assets can be authored consistently.

## Requirements
- Document shader-family responsibilities and boundaries.
- Document terrain atlas versus prop atlas versus character-texture usage.
- Document the material-mask channel contract.
- Document rotation-safe terrain constraints.
- Document Blender UV authoring expectations for props/characters where established.
- Document texel-density conventions and intentional exceptions.
- Document Unity texture import/filtering requirements.
- Document material naming/location conventions.
- Document emission/specular/roughness/metallic authoring guidance.
- Explicitly note deferred features such as rotation-safe normal maps rather than implying support.
- Link relevant implementation tickets and validation scene.

## Acceptance Criteria
- A contributor can determine which shader/material/texture organization to use for a new terrain tile, socket prop/trap, character, or FX asset.
- The mask-channel contract and rotation-safety rules are unambiguous.
- The documentation reflects implemented behavior rather than speculative features.

## Out of Scope
- New shader implementation
- New Blender tooling
- Rendering redesign

## Manual Validation
Follow the documentation as if onboarding a new asset and verify no undocumented project-specific decisions are required for the normal workflow.

## Post-Implementation Report
Record documentation files created/updated, cross-links added, unresolved decisions, and future tooling opportunities.

## Git
Suggested implementation branch: `docs/render08-pixel-rendering-pipeline`

Proceed according to `docs/AGENTS.md`.
