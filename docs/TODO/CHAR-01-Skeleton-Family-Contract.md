# CHAR-01 — Skeleton Family Contract

## Tracking
- **ID:** CHAR-01
- **Status:** Planned
- **Milestone:** Character Architecture / Animation Foundation
- **Depends on:** RENDER-04 validation findings

## Goal
Define a reusable skeleton-family contract so characters with compatible anatomy can share animation clips, modular skinned content, and equipment attachment conventions without per-character rigs.

## Requirements
- Establish the first humanoid skeleton-family contract.
- Define required bone hierarchy/naming, forward/up orientation, scale, root placement, and import expectations.
- Define stable attachment/socket locations needed by later equipment work without implementing the equipment system.
- Define how body variants, hair, clothing, and skinned armor bind to the family.
- Define compatibility rules for sharing animation clips across body variants.
- Document when a creature requires a new skeleton family rather than forcing retargeting onto the humanoid family.
- Keep gameplay jobs, equipment inventory, and animation-state logic out of the skeleton contract.

## Acceptance Criteria
- Two visually different humanoid meshes can bind to the same family and play the same validation clips without duplicated animation assets.
- Required bone/socket names and Blender-to-Unity orientation/scale conventions are documented.
- A contributor can determine whether a new character belongs to the existing family or requires a new one.

## Out of Scope
- Full animation library
- Runtime equipment system
- Procedural character generation
- Advanced IK
- Facial animation
- Creature families beyond documenting extension rules

## Manual Validation
Import two representative humanoid variants using the contract and play the same idle/walk clips. Inspect deformation, root behavior, scale, orientation, and attachment transforms.

## Post-Implementation Report
Record the final hierarchy, naming contract, import settings, socket placeholders, validation meshes/clips, compatibility limitations, and follow-up requirements.

## Git
Suggested implementation branch: `character/char01-skeleton-family-contract`

Proceed according to `docs/AGENTS.md`.
