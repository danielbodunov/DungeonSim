# 0014 — Pivot to Validated Dungeon Building and Raiding

- **Status:** Accepted
- **Date:** 2026-09-10

## Context

The user replaced management simulation with a Meet Your Maker-like build, prove, publish and raid loop. Talismans and trap modifiers were explicitly moved after the prototype.

## Decision

Adopt [core direction](../Design/Core_Game_Direction.md) and [prototype contract](../Design/Raid_Prototype.md). Retain 2.5D pixel-lit 3D presentation as the working baseline. Prove the loop locally with one Warrior, three traps including a physics rock, and one minion before online work.

## Supersession and Reuse

| Existing area | Disposition |
| --- | --- |
| Grid/socket construction, trap service footprints, rendering | Retain; adapt for authoring and raids |
| Save/load, entrance, treasure, damage and NPC runtime | Reuse owners; revise attempt/publishing contracts |
| Completed expedition/recovery/Dread tickets | Historical implementation; not new roadmap acceptance |
| 0005 Dread/defeat | Superseded for active raid economy and attempt death semantics |
| 0007 boss lockout | Deferred; does not define prototype treasure behavior |
| 0011 progression UI | Dread/tier requirements superseded for prototype resources/results |
| 0002 stranded recovery | Retain as legacy NPC behavior; never automatic raid success or validation |
| 0009 phase lighting | Retain visual intent; map to new modes in implementation |
| Other existing decisions | Retain where compatible; this record wins on conflicting core-loop scope |

Future talisman modifiers are not the existing trap mounting/attachment surfaces. Keep structural placement foundations in scope.

## Consequences

Player movement/combat and clean resets become immediate priorities. Published content must not change with a working-copy edit or silently altered definitions. Existing code remains unchanged by the documentation pivot; architecture pages retain implementation descriptions with transition notices.

New gameplay tickets use unused t034–t052 IDs. Conversation aliases are retained only for traceability; existing CHAR-01/02 are not repurposed.

See [roadmap](../Roadmap/Core-Gameplay-Loop.md) and [queue](../TODO/README.md).
