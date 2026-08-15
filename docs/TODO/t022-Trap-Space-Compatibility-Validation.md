# t022 — Trap Space & Compatibility Validation

## Tracking
- **ID:** t022
- **Status:** Planned
- **Milestone:** Strategic Construction
- **Depends on:** t019; t020; t021 as required

## Goal
Make strategic trap planning reliable by validating the complete physical space a trap requires, not merely whether its target corridor cell is available.

## Requirements
- Formalize trap footprint/service-space reservations.
- Detect conflicts with neighboring dungeon construction, other trap mechanisms, incompatible surfaces, and reserved infrastructure.
- Keep hazard volume and mechanism/service footprint distinct.
- Preview all relevant conflicts before committing placement.
- Ensure save/load/scenario reconstruction uses the same compatibility rules.

## Acceptance Criteria
- Overlapping trap mechanisms are rejected.
- A corridor can remain traversable while its external service space is reserved by a trap.
- Future construction that would invalidate occupied service space is rejected or handled by an explicit removal/replacement flow.
- Preview and committed placement use shared validation rules.

## Out of Scope
- Final resource costs
- Trap effectiveness balancing
- Automated optimal-layout suggestions

## Git
Suggested branch: `feature/t022-trap-space-validation`
