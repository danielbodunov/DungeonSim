# Raid Prototype Contract

Status: Approved implementation target, not implemented by this documentation change.
Authority: [core direction](Core_Game_Direction.md), [decision 0014](../Decisions/0014-raid-building-pivot.md).

## Separate State Owners

| State | Meaning |
| --- | --- |
| Working dungeon | Editable authored layout and monotonically changing revision |
| Validation proof | Successful creator attempt bound to exact revision and compatible gameplay content/rules |
| Published version | Immutable authored snapshot; editing the working copy does not change it |
| Attempt | Fresh runtime instance: seeking treasure, carrying treasure, escaped, dead or abandoned |
| Result and receipt | Terminal outcome plus separately applied, idempotent resource award |

Editing/awaiting-validation/validated belong to the working copy. Validation-in-progress and raid-in-progress are attempt contexts. Published versions coexist with either; do not place all states in one mutually exclusive lifecycle enum.

Every accepted authoring edit invalidates proof in the prototype, including cosmetic edits. Rejected edits do not mutate revision. Preserve the last published version. Death/abandon grant no validation proof. Validation uses the fixed Warrior, authoritative entrance, treasure and return alive, without build/debug privileges or mid-run state restoration.

## Construction and Content

Reuse existing grid, socket, occupancy, service-cell and atomic purchase rules. Require exactly one entrance/exit and one treasure. Validate anchors locally; creator completion, not a basic graph check, proves playability.

Prototype movement is running/jumping with ordinary gravity and a readable follow camera. No advanced climbing or ladder dependency is mandatory for the test dungeon. A minion may use limited supported routes and bounded pursuit; it must not require the future NPC raider planner.

## Reset and Persistence

Store authored initial states separately from spawned runtime objects. Retry restores character health/velocity, objective state, minion positions/health, trigger/cooldown state and hazards; remove old projectiles and rocks.

Extend existing save ownership rather than treating a live management save as a published snapshot. Store stable content IDs, author, revision, version and compatibility identity. Because current saves resolve footprints from live definitions, snapshot loading must either preserve required gameplay definitions or reject incompatible versions. Silently using changed trap geometry/timing would invalidate creator proof.

Legacy save migration/rejection is explicit implementation work. Do not silently reinterpret Dread or stored loot as the new currency.

## Physics

The rolling rock is required prototype content. Restore fixed initial pose, velocities and release parameters; use bounded lifetime, out-of-bounds cleanup and consistent stepping. Test repeatability across frame rates and prevent projectile tunnelling/repeated accidental damage. Deterministic setup is required; bit-identical Unity physics across machines is not promised. Future result verification must account for this.

## Results and Prototype Economy

Store one terminal result per unique attempt ID: snapshot version, builder/raider identity, outcome, duration, treasure acquired and lethal source. Abandon is not a kill or success. Runtime validation/test attempts are distinguishable and reward-ineligible.

Record outcomes first; the economy consumes them and attaches a reward receipt. Reward success to the raider and credited trap/minion kills to the builder. Persist receipts so replaying an outcome after reload cannot duplicate grants. Use configurable amounts and sufficient starter currency for a first dungeon. Purchase debits and placement commit atomically.

Full online anti-farming/security is deferred, not solved by local profile separation. Online publishing must establish trusted result verification, compatibility policy and abuse prevention before real progression is exposed.

## Post-Prototype Backlog

| Preview label | Work | Boundary / prerequisite |
| --- | --- | --- |
| MOVE-01 | Ledge grabbing and climbing | Extend shared movement/collision abilities |
| MOVE-02 | Wall sliding / advanced jumps | Validate movement feel and class fairness |
| MOVE-03 | Probing-rock throw | Shared physical trigger interactions |
| CLASS-01 / CLASS-02 | Rogue / Mage | Fixed class identities; no equipment inventory |
| NPC-RAID-01 | NPC-controlled raiders | Shared abilities, personal knowledge and objective planning |
| TRAP-05 | Treasure-triggered traps | Consume treasure-carried state; validate return route |
| TRAP-ATTACH-01 | Buildable trap modifiers | Real sockets/footprints, compatibility and invalidation |
| TRAP-UPGRADE-01 | Efficiency upgrades | Follow modifier and economy contracts |
| TALISMAN-01 | Behind-the-scenes perk loadout | After prototype; loadout validation policy required |
| ONLINE-01 / ONLINE-02 | Remote publishing and results | Trust, moderation, versioning and anti-farming design |
| PROG-01 | Rare rewards and unlock progression | Reward integrity and content availability |
| Content expansion | More traps/minions, including boiling oil | After all prototype hazards reset reliably |

These are deferred planning labels, not ready tickets or reassigned repository IDs. Assign unused stable IDs when promoted. No talisman/attachment framework implementation is required merely to reserve this direction.

## Open Decisions for Later Tickets

- Class/loadout eligibility: creator completion with one class does not prove every future class/loadout can finish.
- Final currency names, costs, refunds, reward amounts and friendly-fire defaults.
- Supported depth transitions and camera/control tuning; preserve current spatial rules during prototype work.
- Online authority and physics verification approach.
