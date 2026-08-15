# t012 — Visible Adventurer Carried Loot

## Tracking
- **ID:** t012
- **Status:** Planned
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t011 validation

## Goal
Make stolen treasure materially visible while an adventurer carries it so ownership changes are readable from the game world rather than only from debug state.

## Requirements
- Add a visual carried-loot representation driven by authoritative NPC treasure custody.
- Prefer a generic bag/bundle/container representation for the first pass rather than attaching arbitrary treasure prefab geometry directly.
- Visual appears when relevant loot is acquired and disappears when custody is cleared by escape, death, recovery, or other authoritative resolution.
- Support future mixed physical loot without coupling the visual system specifically to `TreasureProp`.
- Keep visual state derived from gameplay ownership; it must not become a second inventory authority.

## Acceptance Criteria
- Treasure pickup visibly changes the adventurer.
- Death/escape/custody clearing removes the carried visual at the correct time.
- Empty-handed adventurers show no loot visual.
- Multiple carried items can be represented without requiring one visible model per item in the first implementation.

## Out of Scope
- Full equipment/inventory visualization
- Physical death drops (t013)
- Player recovery interaction

## Git
Suggested branch: `feature/t012-visible-carried-loot`
