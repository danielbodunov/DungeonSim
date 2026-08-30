# RENDER-01 — Shared Pixel-Lit Core

## Tracking
- **ID:** RENDER-01
- **Status:** Complete
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

- Added `Assets/Shaders/Includes/DungeonPixelLitCore.hlsl` with reusable
  `DungeonPixelLitSettings`, `DungeonPixelLitSampleLocalLighting`, and
  `DungeonPixelLitEvaluate` interfaces.
- The core owns previous/current propagated-field reconstruction, visible pixel
  snapping, ambient and quantized diffuse response, local tint, HDR overbright
  and hot wash, vertex AO, emission, and RENDER-00 specular behavior.
- `RotationSafeTileAtlas.shader` now samples base color/material mask, constructs
  settings from its existing material properties, and delegates its final color
  evaluation to the include. The extracted arithmetic and operation ordering are
  intentionally unchanged to preserve terrain output.
- Terrain keeps `ResolveAtlasRect`, world-normal role classification, family and
  weighted variant lookup, ground depth bands, rotation-safe projection, and
  aligned base/mask sampling. Shadow and depth passes are unchanged.
- The shared interface accepts already-sampled color/mask data and ordinary
  world-space geometry inputs. It has no dependency on UVs, tile roles, atlas
  rectangles, surface families, or terrain depth, making it directly reusable
  by a future UV0 prop shader.
- Added editor source-contract tests confirming the shared boundary and that the
  terrain shader consumes rather than duplicates the core evaluation. Existing
  RENDER-00 shader import/material-mask tests continue to cover shader import and
  material features.
- C# editor assemblies compile successfully. Terrain visual equivalence,
  rotation behavior, and material-mask results remain awaiting manual Unity
  validation.

## Git
Suggested implementation branch: `render/render01-shared-pixel-lit-core`

## Validation Result

Unity validation completed on 2026-08-29. Representative terrain appearance,
rotation-safe atlas behavior, dynamic-light response, and RENDER-00 material-mask
emission/specular behavior were confirmed after extraction.

Proceed according to `docs/AGENTS.md`.
