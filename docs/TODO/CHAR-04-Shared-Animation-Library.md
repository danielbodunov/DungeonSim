# CHAR-04 — Shared Animation Library Foundation

## Tracking
- **ID:** CHAR-04
- **Status:** Planned
- **Milestone:** Character Architecture / Animation
- **Depends on:** CHAR-01

## Goal
Establish the first reusable animation-library and playback contract for a skeleton family so NPC appearance, equipment, and character variants do not require duplicated animation sets.

## Requirements
- Define the initial animation categories and naming conventions for the humanoid family.
- Establish a minimal shared locomotion set and a small number of representative action/tool animations.
- Ensure clips target the skeleton-family contract rather than individual visual variants.
- Define root-motion versus in-place expectations for Dungeon Sim movement.
- Define how equipment-independent actions such as generic tool swings can support multiple compatible tools where practical.
- Keep job/AI decision logic separate from animation playback.
- Avoid creating character-specific Animator Controllers solely for appearance variants.

## Acceptance Criteria
- Multiple humanoid appearances use the same locomotion clips.
- At least one representative action/tool clip is reusable across more than one compatible equipped item or character appearance.
- Animation naming, organization, import, and root-motion policy are documented.
- Adding a new compatible visual variant does not require copying the animation library.

## Out of Scope
- Complete production animation library
- Procedural animation
- Advanced IK
- Facial animation
- Gameplay combat timing redesign
- Non-humanoid skeleton families

## Manual Validation
Play the shared locomotion and representative action clips on multiple humanoid visual variants and with compatible visible equipment attached.

## Post-Implementation Report
Record clip organization, naming, import settings, root-motion policy, Animator/playback architecture, validation characters/items, and limitations.

## Git
Suggested implementation branch: `character/char04-shared-animation-library`

Proceed according to `docs/AGENTS.md`.
