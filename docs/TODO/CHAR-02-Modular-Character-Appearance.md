# CHAR-02 — Modular Character Appearance Assembly

## Tracking
- **ID:** CHAR-02
- **Status:** Planned
- **Milestone:** Character Architecture / Appearance
- **Depends on:** RENDER-04, CHAR-01

## Goal
Create the runtime visual-assembly layer that resolves character appearance data into reusable body, hair, clothing, and skinned-armor modules without duplicating character prefabs or materials for every combination.

## Requirements
- Define a character appearance data structure separate from gameplay identity.
- Support selecting compatible body, hair, clothing, and other approved visual modules.
- Apply RENDER-04 skin/hair/primary/secondary colors through the shared character material contract.
- Assemble modules against the appropriate skeleton family.
- Validate combinations and reject incompatible modules with useful diagnostics.
- Keep source assets reusable; do not generate persistent per-character mesh/material assets at runtime.
- Provide a deterministic way to rebuild the same visual appearance from saved/resolved appearance data.
- Keep random-generation probabilities and character-creator UI out of this ticket.

## Acceptance Criteria
- Several distinct humanoids can be assembled from the same module library.
- Hair/body/clothing selection and runtime colors can change without duplicating the base character prefab.
- Compatible modules follow the shared skeleton and animation clips correctly.
- Reapplying the same appearance data reconstructs the same visual result.

## Out of Scope
- Random appearance generation
- Player customization UI
- Equipment/inventory
- Full animation-state system
- Runtime mesh combining optimization

## Manual Validation
Create several appearance definitions using different body/hair/clothing selections and colors, rebuild them repeatedly, and animate them using the shared skeleton-family validation clips.

## Post-Implementation Report
Record data contracts, module compatibility rules, prefab/asset organization, material-property application, reconstruction behavior, validation combinations, and performance observations.

## Git
Suggested implementation branch: `character/char02-modular-appearance`

Proceed according to `docs/AGENTS.md`.
