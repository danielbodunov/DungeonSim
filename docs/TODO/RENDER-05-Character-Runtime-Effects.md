# RENDER-05 — Character Runtime Material Effects

## Tracking
- **ID:** RENDER-05
- **Status:** Planned
- **Milestone:** Pixel Rendering / Characters
- **Depends on:** RENDER-04

## Goal
Add a small standardized runtime-effect interface to the Pixel-Lit character shader without creating material variants for every temporary gameplay state.

## Requirements
- Add parameterized support for a damage/hit flash.
- Add generalized effect/status tint controls suitable for states such as poison or frozen without hard-coding gameplay logic into the shader.
- Add optional emission/effect intensity support where useful.
- Provide an approach for selection/highlight behavior that remains visually consistent with the pixel-art style.
- Runtime state changes should use renderer/material-property mechanisms that avoid persistent duplicated materials where practical.
- Keep gameplay state ownership outside the shader.

## Acceptance Criteria
- A character can flash on hit and return to its base appearance.
- A generalized status tint can be applied and removed at runtime.
- Multiple characters sharing the same base material can display different temporary visual states without requiring a unique authored material for every state combination.
- Effects remain coherent under dynamic lighting and do not break point-sampled texture presentation.

## Out of Scope
- Gameplay status systems
- Damage calculation
- VFX particles
- Full death/dissolve implementation unless separately approved

## Manual Validation
Run multiple characters sharing a base material and independently exercise hit flash, effect tint, and highlight controls while they remain dynamically lit.

## Post-Implementation Report
Record shader properties, runtime control mechanism, material-instancing implications, example call sites/components if added, and follow-up effect requests.

## Git
Suggested implementation branch: `render/render05-character-effects`

Proceed according to `docs/AGENTS.md`.
