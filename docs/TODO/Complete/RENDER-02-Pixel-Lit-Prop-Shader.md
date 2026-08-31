# RENDER-02 — Pixel-Lit Prop Shader

## Tracking
- **ID:** RENDER-02
- **Status:** Complete
- **Milestone:** Pixel Rendering / Props and Traps
- **Depends on:** RENDER-01

## Goal
Create the general-purpose Pixel-Lit shader for socket props, traps,
decorations, and interactable environment objects using ordinary model UVs
while matching the established terrain lighting style.

## Requirements
- Consume the shared Pixel-Lit lighting core from RENDER-01.
- Use conventional UV0 sampling suitable for Blender-authored prop UVs and prop atlases.
- Support point-sampled base color and base tint.
- Support the approved emission, roughness, metallic, and specular model.
- Support optional alpha clipping for appropriate pixel-art props.
- Keep trap gameplay semantics and terrain atlas-addressing logic out of the shader.

## Acceptance Criteria
- Representative wood, stone, and metal props visually belong with terrain under the same dynamic lights.
- A trap can use the same shader without trap-specific shader logic.
- Point-filtered prop textures remain crisp at the intended texel density.
- Emission and specular behave consistently with terrain.
- Alpha-clipped geometry renders correctly on a representative asset.
- Terrain rendering remains unchanged.

## Out of Scope
- Character-specific rendering/effects
- Gameplay interaction highlighting
- Final prop-atlas authoring conventions
- FX/transparency shader beyond alpha clipping

## Manual Validation

1. Duplicate `Assets/Materials/PixelLitProp.mat` for each validation asset and assign its point-filtered base texture.
2. Place wood, stone, metal, hanging/socket-decoration, and spike/mechanical trap renderers beside representative terrain.
3. Move the same `DungeonLightSource` across terrain and props. Confirm quantized diffuse, color propagation, overbright/hot-wash, and minimum-light behavior remain coherent.
4. Enable the material mask on emission and metal examples. Confirm R emission, G roughness, and B metallic match terrain behavior.
5. On geometry with transparent base texels, enable Alpha Clipping and adjust Alpha Cutoff. Confirm cutouts appear in color, depth, and cast shadows.
6. Rotate and inspect ordinary UV0 props. Confirm no terrain family, ground-depth, or tile-rotation selection is applied.

## Post-Implementation Report

- Added `Assets/Shaders/PixelLitProp.shader`, a handwritten URP shader that samples `_BaseMap` and optional `_MaterialMask` through conventional UV0 and delegates lighting to `DungeonPixelLitCore.hlsl`.
- Added `Assets/Materials/PixelLitProp.mat` as a neutral reusable starting material. Material-mask, emission, specular, and alpha clipping are disabled by default.
- Exposed base tint, mask-driven emission/roughness/metallic, stylized specular settings, shared dungeon-light settings, vertex-red AO intensity, culling, and optional alpha clipping/cutoff.
- Added matching alpha rejection to ForwardLit, ShadowCaster, and DepthOnly passes. Blended transparency remains out of scope.
- The shader contains no trap-specific behavior and no terrain atlas, surface family, ground stratification, height selection, or rotation-safe addressing.
- Added editor tests for shader import, shared-core consumption, the material contract, alpha coverage, and isolation from terrain/trap semantics.
- Updated `DungeonLightReceiver` to preserve materials already using the Pixel-Lit Prop shader instead of converting them to the legacy Dungeon Grid Lit shader. An explicitly configured receiver material override remains authoritative.
- No terrain shader, terrain material, prop prefab, or trap gameplay asset was changed. Unity shader import and representative visual comparison remain pending.

## Compatibility and Authoring Notes

- Base and mask textures share UV0. The mask channels are R emission, G roughness, B metallic, and A reserved.
- Set pixel-art textures to Point filtering, disabled mipmaps, Clamp wrapping where atlas edges require it, and no compression. Sampling explicitly uses LOD 0.
- Vertex color red follows terrain AO: `1` is unoccluded and `0` is fully occluded. Meshes without vertex colors normally import as white.
- Props receive the propagated dungeon light field, not Unity realtime-light BRDF lighting. Specular direction remains art-directed because the field does not encode per-source direction.

## Git
Suggested implementation branch: `render/render02-pixel-lit-props`

## Validation Result

Unity validation completed on 2026-08-29. The Pixel-Lit Prop shader was tested
on ladder geometry and confirmed to remain assigned through
`DungeonLightReceiver` rather than being replaced by Dungeon Grid Lit. Its
appearance under the dungeon lighting system was manually approved.

Proceed according to `docs/AGENTS.md`.
