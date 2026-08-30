# CHAR-03 — Visible Equipment and Attachment Sockets

## Tracking
- **ID:** CHAR-03
- **Status:** Planned
- **Milestone:** Character Architecture / Equipment Presentation
- **Depends on:** RENDER-02, CHAR-01; CHAR-02 recommended

## Goal
Provide a stable visual attachment system so equipped tools, weapons, armor, and carried items can be displayed on animated characters without baking equipment into character textures or animation frames.

## Requirements
- Define stable named equipment sockets on the humanoid skeleton family.
- Initial socket categories should cover main hand, off hand, head, and back; add others only when justified by representative content.
- Attach rigid equipment prefabs to sockets while preserving their Pixel-Lit Prop materials.
- Define per-item local position/rotation/scale alignment data without modifying the shared skeleton for each item.
- Support equipping, replacing, and removing visible items at runtime.
- Keep visual attachment derived from authoritative equipment state rather than owning inventory state.
- Document when equipment should be rigid Pixel-Lit Prop content versus skinned Pixel-Lit Character content.

## Acceptance Criteria
- A humanoid can visibly equip and remove at least two different main-hand items while animated.
- A head or back item can be displayed using the same attachment contract.
- Equipment follows animation correctly and retains its intended shader/material behavior.
- Item-specific alignment does not require editing the skeleton or animation clips.
- Visual state can be reconstructed from authoritative equipment data.

## Out of Scope
- Inventory/equipment gameplay rules
- Item stats
- Weapon combat logic
- Hand IK/grip correction unless required for basic validation
- Complex cloth physics
- Procedural equipment generation

## Manual Validation
Equip a sword/pickaxe or comparable pair of rigid items on the same animated humanoid, swap between them, remove them, and validate a head/back attachment while locomotion plays.

## Post-Implementation Report
Record socket names/transforms, alignment-data format, renderer/material behavior, reconstruction path, validation items, limitations, and any IK needs discovered.

## Git
Suggested implementation branch: `character/char03-visible-equipment`

Proceed according to `docs/AGENTS.md`.
