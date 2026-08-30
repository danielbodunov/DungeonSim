# CHAR-05 — Procedural and Player Appearance Data Contract

## Tracking
- **ID:** CHAR-05
- **Status:** Planned
- **Milestone:** Character Architecture / Customization
- **Depends on:** CHAR-02

## Goal
Define one authoritative appearance-data contract that can drive both randomly generated NPC appearances and future player adventurer customization without creating separate rendering pipelines.

## Requirements
- Define serializable resolved appearance data referencing approved visual modules and runtime colors.
- Separate resolved appearance from generation rules/probability tables.
- Provide deterministic reconstruction of a character visual from saved appearance data.
- Support random NPC generation as one producer of the contract.
- Keep the contract suitable for a future player-facing character creator as another producer.
- Define validation/fallback behavior for missing or incompatible appearance assets.
- Integrate with the modular appearance assembly layer without giving the renderer knowledge of generation rules.
- Account for save compatibility/versioning if appearance data is persisted.

## Acceptance Criteria
- A seeded or otherwise deterministic generator can produce a resolved appearance and rebuild it consistently.
- The same appearance structure can be populated manually without invoking random generation.
- Generated and manually specified characters use the same visual assembly path.
- Save/load or equivalent reconstruction preserves the resolved appearance.

## Out of Scope
- Final character-creator UI
- Final distribution/balance of random traits
- Gameplay effects of species/traits
- Equipment generation
- Animation selection
- Character naming/personality generation

## Manual Validation
Generate several appearances, serialize/reconstruct them, and manually construct an equivalent appearance definition to confirm both paths feed the same renderer/assembly system.

## Post-Implementation Report
Record the final data schema, asset references, deterministic-generation approach, persistence/versioning behavior, fallback rules, and future character-creator requirements.

## Git
Suggested implementation branch: `character/char05-appearance-data`

Proceed according to `docs/AGENTS.md`.
