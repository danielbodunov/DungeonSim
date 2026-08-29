# RENDER-00 — Rotation-Safe Material Mask, Emission, and Specular

## Tracking
- **ID:** RENDER-00
- **Status:** Planned
- **Milestone:** Pixel Rendering / Material Foundation
- **Depends on:** Current rotation-safe terrain shader and terrain atlas pipeline

## Goal
Extend the existing rotation-safe dungeon/terrain shader with a packed material-mask atlas that supports emission, roughness, metallic response, and stylized specular while preserving the current atlas selection and rotation behavior.

## Requirements
- Preserve the existing rotation-safe UV/tile-selection path as the authoritative coordinate path.
- Add a companion material-mask atlas that uses exactly the same tile layout, tile selection, and rotation-safe UV transformation as the base-color atlas.
- Define mask channels as:
  - **R:** Emission mask
  - **G:** Roughness
  - **B:** Metallic
  - **A:** Reserved for future use
- Add material controls for emission color and emission intensity.
- Emission must support partial per-pixel masking; enabling emission must not make an entire tile emissive unless its mask requests it.
- Treat shader emission as surface appearance only. Do not automatically create or drive Unity lights.
- Add roughness/metallic-driven specular response compatible with the existing dynamic-lighting model.
- Provide independent controls for specular strength and stylization/quantization so specular can be tuned separately from diffuse lighting.
- Prefer a small number of discrete specular steps when stylization is enabled; retain the ability to evaluate a smoother response for comparison/tuning.
- Existing materials must retain their current appearance when the new features are disabled or use neutral/default mask values.
- Keep the implementation suitable for later extraction into a shared Pixel-Lit core used by terrain, props, traps, and characters.

## Rotation-Safety Constraints
- Do not introduce a second independent UV-orientation system for the mask atlas.
- Base-color and mask samples must remain pixel-aligned after every supported tile rotation.
- Material properties encoded in the mask must rotate with the artwork rather than remain fixed in world orientation.
- Do not change existing tile taxonomy, atlas addressing, or rotation semantics unless required to correct a demonstrated defect.

## Acceptance Criteria
- Existing dungeon and terrain rendering is visually unchanged with emission/specular additions disabled.
- A test tile containing a localized emission mask emits only from the authored pixels.
- Emission remains registered to the correct pixels through every supported tile rotation.
- Roughness variation visibly changes highlight width/intensity without changing base-color atlas selection.
- Metallic variation visibly changes the intended specular/material response.
- Roughness and metallic masks remain registered through every supported tile rotation.
- Specular can be switched/tuned between smooth and stylized/quantized behavior without modifying the diffuse-lighting configuration.
- A representative stone, wet-stone/low-roughness region, metal region, and emissive region can coexist in the atlas and render correctly under dynamic lighting.
- No normal-map dependency is introduced.

## Out of Scope
- Normal maps or rotation-safe tangent-space normal-map correction
- Parallax/height mapping
- Runtime-generated lights from emissive pixels
- Prop, trap, or character shaders
- Refactoring the current terrain shader into the final shared Pixel-Lit shader family
- Reauthoring the existing base-color atlas

## Manual Validation
1. Capture/reference the current rotation-safe shader appearance before changes.
2. Verify an unmasked/default tile matches the reference under representative dynamic lighting.
3. Author a mask test tile with clearly separated emission, high/low roughness, and metallic regions.
4. Rotate the same tile through every supported orientation and verify base color and all mask channels stay registered.
5. Move a dynamic point light across the tile and inspect roughness/metallic specular behavior from multiple viewing angles.
6. Compare smooth versus quantized specular settings and choose values that remain coherent with the pixel-art lighting style.
7. Verify emission remains visible as intended in both lit and dark conditions without itself illuminating neighboring geometry.

## Implementation Notes
- Use a generic name such as `MaterialMaskAtlas` rather than an emission-specific texture name so the texture remains extensible.
- Keep specular quantization independent from diffuse quantization. The final values should be art-directed rather than assumed to match.
- Reserve mask alpha rather than assigning it opportunistically during this ticket.
- Point sampling and import settings should preserve pixel registration between the base and mask atlases.

## Post-Implementation Report
Record:
- Files/shaders/materials changed
- Final mask-channel contract
- New shader properties and defaults
- Any atlas/import-setting requirements
- Selected default specular behavior/step count
- Rotation-safety validation results
- Performance or shader-variant impact observed
- Follow-up work discovered

## Git
Suggested implementation branch: `render/render00-material-mask-specular-emission`

Proceed according to `docs/AGENTS.md`.
