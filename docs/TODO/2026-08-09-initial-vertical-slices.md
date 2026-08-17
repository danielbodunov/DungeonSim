# Initial Building and Adventurer Vertical Slices

- Status: Implemented; awaiting Unity play-mode validation and balance tuning
- Started: 2026-08-09
- Last updated: 2026-08-09

## Completed implementation

- [x] Add persistent Auto, Narrow, and Wide intent per dungeon cell.
- [x] Restrict explicit width choices and deterministically re-resolve the local Auto neighborhood.
- [x] Add width-mode controls to the runtime build palette.
- [x] Save and restore width intent, Dread, and dungeon level.
- [x] Centralize trap dodge probability and action outcomes.
- [x] Make Dexterity the primary dodge attribute and Luck the secondary attribute.
- [x] Add pooled, rising, fading popups for dodge, damage, and defeat.
- [x] Keep defeated adventurers in the persistent roster.
- [x] Accumulate visit Dread from exploration and damage, plus a level-scaled defeat bonus.
- [x] Settle pending Dread exactly once on normal exit, defeat, or forced session return.
- [x] Expose the original prototype spending/level API; superseded by t016's audited Dread spending path and treasure-manifestation sink.

## Validation and follow-up

- [ ] Play-test Auto, Narrow, and Wide painting across corridors, dense rooms, and conversions.
- [ ] Decide whether Transition profiles satisfy explicit Narrow/Wide intent or require their own tool.
- [ ] Tune trap difficulty, attribute weights, and minimum/maximum dodge chances.
- [ ] Confirm popup placement and readability at every gameplay speed and camera position.
- [ ] Tune exploration, damage, and level-scaled defeat Dread rewards.
- [ ] Add physical build-definition costs through an atomic placement transaction (t018).
- [ ] Add the dungeon-level purchase UI and data-driven level costs.
- [ ] Extend action feedback beyond traps as new action types are implemented.

## Verification

- The Unity C# project compiles with zero warnings and zero errors.
- Save version 3 remains backward-compatible with earlier saves by defaulting missing width intent to Auto, Dread to zero, and dungeon level to one.
