# RENDER-03 — Prop Material and Atlas Pipeline

## Tracking
- **ID:** RENDER-03
- **Status:** Complete
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

- Added the task-oriented [pixel-lit prop authoring guide](../HowTo/Author_A_Pixel_Prop.md)
  and linked it from the How-To index and prefab conventions.
- Standardized a 512x512 base/mask pair that may grow to 1024x1024 without
  moving existing pixels. The left half holds reusable patches, lower-right
  holds unique art, and upper-right 256x64 remains validation/reserve space.
- Regions are arbitrary integer rectangles with two-pixel extruded gutters;
  UVs address inner art and sample edge texels at their centers.
- Set 96 texels per Unity world unit from terrain's 32-pixel logical cells and
  three cells per one-unit tile, with a documented 72-120 exception range.
- Documented deliberate overlap for reusable wood, metal, stone, bone, cloth,
  repeated, mirrored, and hidden surfaces, and when unique or directional data
  must not overlap.
- Documented Blender scale, UV0, grid, texel-center, vertex-AO, and FBX rules,
  plus Unity Point/no-mipmap/no-compression/Clamp/color-space settings.
- Standardized atlas, standalone texture, imported slot, and `PLP_` material
  names. `Assets/Materials/PixelLitProp.mat` remains the default; distinct
  materials/textures require a rendering or ownership need.
- Preserved RENDER-00 channels: R emission, G roughness, B metallic, A reserved,
  with `(0, 1, 0, 1)` neutral and exact base/mask UV registration.
- Possible follow-ups after proving the manual workflow are allocation
  metadata/visualization, Blender density checks, and Unity import validation.
  No automation was added by this ticket.

Manual validation and completion were confirmed by the user on 2026-08-30.

## Validation Result

Complete. The documented workflow was approved for production use on
2026-08-30.

## Git
Suggested implementation branch: `art/render03-prop-atlas-pipeline`

Proceed according to `docs/AGENTS.md`.
