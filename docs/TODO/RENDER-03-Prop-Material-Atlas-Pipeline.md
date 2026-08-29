# RENDER-03 — Prop Material and Atlas Pipeline

## Tracking
- **ID:** RENDER-03
- **Status:** Planned
- **Milestone:** Pixel Rendering / Asset Authoring
- **Depends on:** RENDER-02

## Goal
Formalize the Blender-to-Unity texture/material workflow for socket props, traps, decorations, and interactables so new assets can be authored consistently without ad-hoc material decisions.

## Requirements
- Define the prop-atlas strategy and expected atlas resolution/ranges.
- Define padding/gutter requirements for point-sampled atlas regions.
- Establish baseline world-space texel density relative to the dungeon environment.
- Allow arbitrary rectangular atlas regions rather than forcing props into the dungeon tile grid.
- Document when UV overlap/reuse is encouraged for shared materials such as wood, metal, stone, bone, and cloth.
- Define Blender UV expectations and pixel/grid alignment guidance.
- Define Unity texture import settings required to preserve the pixel-art appearance.
- Define material naming and project-location conventions.
- Define when an asset should share the standard prop material versus require a distinct material/texture.
- Account for the material-mask workflow established in RENDER-00 where props need emission/roughness/metallic data.

## Acceptance Criteria
- A new Blender-authored prop can be mapped to the prop atlas and imported into Unity using the documented process without inventing new conventions.
- Multiple props can intentionally reuse the same atlas texels.
- Pixel size is visually coherent between representative props and terrain.
- Texture import settings preserve crisp point-sampled pixels.
- The workflow supports both generic reusable material patches and unique prop artwork.

## Out of Scope
- Automated Blender atlas-assignment tooling
- Runtime atlasing
- Texture arrays
- Character texture organization
- Draw-call optimization not justified by profiling

## Manual Validation
Author/import at least one simple reusable-material prop and one prop with unique artwork using only the documented pipeline.

## Post-Implementation Report
Record the finalized atlas layout rules, texel-density baseline, Blender workflow, Unity import settings, material conventions, and any proposed automation/tooling follow-ups.

## Git
Suggested implementation branch: `art/render03-prop-atlas-pipeline`

Proceed according to `docs/AGENTS.md`.
