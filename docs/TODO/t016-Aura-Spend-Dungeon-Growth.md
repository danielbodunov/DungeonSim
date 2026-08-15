# t016 — Aura Spend & Dungeon Growth Foundation

## Tracking
- **ID:** t016
- **Status:** Planned
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t011; t009 Aura foundation

## Goal
Give Aura (working name) a clear supernatural purpose by making it a progression/growth currency rather than the universal cost for mundane construction.

## Initial Direction
Aura represents supernatural dungeon power. Candidate sinks include generating/replenishing treasure bait, increasing dungeon level, unlocking tile families, increasing build depth, unlocking stronger traps, unlocking spawners, and activating supernatural structures.

## Requirements
- Establish an authoritative Aura spending API complementary to the existing harvest path.
- Implement the smallest progression purchase needed to prove harvest → spend → dungeon growth.
- Keep Aura distinct from physical construction resources.
- Spending must be validated, atomic, and auditable enough for debugging.
- Do not hard-code the entire future progression tree into this foundation.

## Acceptance Criteria
- Player can spend harvested Aura on at least one meaningful dungeon-growth action.
- Insufficient Aura rejects the purchase without partial effects.
- Successful spending updates persistent Aura exactly once.
- The growth effect is persistent/authoritative.
- Physical building costs are not automatically converted to Aura costs.

## Design Questions to Resolve During Ticket
- Final currency name
- Best first growth action: treasure generation, dungeon level, build depth, or unlock
- Whether progression is global, per-dungeon, or hybrid

## Out of Scope
- Full tech tree
- Final balancing
- Physical material economy (t017/t018)

## Git
Suggested branch: `feature/t016-aura-dungeon-growth`
