# RENDER-07 — Pixel Rendering Validation Scene

## Tracking
- **ID:** RENDER-07
- **Status:** Planned
- **Milestone:** Pixel Rendering / Validation
- **Depends on:** RENDER-02, RENDER-04, RENDER-06

## Goal
Create a repeatable visual validation scene for comparing terrain, props, traps, characters, and FX under representative dungeon lighting conditions.

## Requirements
- Include representative terrain/dungeon surfaces.
- Include at least one wood prop, metal prop, socket decoration, and trap.
- Include a representative skinned character.
- Include representative emissive/cutout FX.
- Provide representative dynamic-light positions/intensities and dark/ambient conditions.
- Include material-mask examples for emission, roughness, and metallic response.
- Make the scene suitable for regression checks after future shader changes.
- Keep it isolated from gameplay/procedural-generation requirements where possible.

## Acceptance Criteria
- All major Pixel-Lit shader categories can be inspected side by side.
- Rotation-safe terrain material behavior can be checked in the same environment as ordinary UV0 assets.
- Specular, emission, diffuse lighting, shadows, texture filtering, and texel-density mismatches are easy to identify.
- The scene can be reopened and used without recreating test conditions manually.

## Out of Scope
- Automated screenshot comparison
- Performance benchmark scene
- Full gameplay vertical slice

## Manual Validation
Open the scene from a clean editor session, exercise the documented lighting/material cases, and verify each shader category can be evaluated without additional setup.

## Post-Implementation Report
Record scene path, included reference assets, lighting cases, known visual limitations, and recommended baseline screenshots/settings if useful.

## Git
Suggested implementation branch: `render/render07-validation-scene`

Proceed according to `docs/AGENTS.md`.
