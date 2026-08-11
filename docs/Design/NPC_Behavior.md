NPC BEHAVIOR DESIGN
===================

Purpose
-------
Define how adventurers explore, investigate, remember, and leave the dungeon.
This document is intended to evolve as NPC traits, equipment, traps, enemies,
and other dungeon encounters are designed.


CORE PRINCIPLES
---------------

1. Exploration should feel continuous.
   NPCs should normally move through ordinary cells without stopping.

2. Stopping should communicate a decision or event.
   An NPC should pause only when there is something worth investigating,
   interacting with, avoiding, or recovering from.

3. NPC knowledge should constrain NPC decisions.
   An NPC may only deliberately use information that character has acquired.

4. Returning home must use a familiar route.
   An NPC must not discover a shortcut while planning its exit. Its return path
   must be assembled entirely from cells and connections it previously explored.

5. Character traits should eventually influence all of these choices.

6. Movement should use the dungeon's authored depth.
   NPCs should not always travel on one flat Z line when a cell provides safe,
   readable paths through the foreground or background.

7. Important actions should be legible without stopping the simulation.
   Brief world-space feedback should communicate dodges, damage, social actions,
   and other meaningful outcomes.


CURRENT IMPLEMENTED BASELINE
----------------------------

- NPCs move through ordinary destination cells without an unconditional wait.
  A pause occurs only when the explicit investigation decision hook approves it.
- Cell-entry experience is awarded as each route edge completes, while walking
  and climbing retain their separate stamina costs.
- Each NPC records visited cells and physically traversed connections. Planning
  or rebuilding the global navigation graph does not grant route knowledge.
- Return routing is restricted to familiar connections, so an untraversed global
  shortcut cannot be selected deliberately.
- Debug gizmos expose the active NPC's visited cells, familiar connections,
  active route, and next waypoint.

Current follow-up:

- Exploration target selection is too deterministic at junctions. NPCs tend to
  choose the same branch, which makes exploration appear scripted even when
  multiple unknown routes are available.


PROPOSED HIGH-LEVEL STATE FLOW
------------------------------

ENTER DUNGEON
    |
    v
EXPLORE CONTINUOUSLY
    |
    +--> Something interesting detected? --> INVESTIGATE / INTERACT
    |                                           |
    |                                           v
    |                                      RESOLVE RESULT
    |                                           |
    +<------------------------------------------+
    |
    +--> Stamina depleted or retreat triggered? --> PLAN FAMILIAR RETURN
                                                        |
                                                        v
                                                  RETURN TO ENTRANCE
                                                        |
                                                        v
                                                   EXIT DUNGEON


CONTINUOUS EXPLORATION
----------------------

Desired behavior:

- Plan toward an unexplored frontier rather than treating every cell as a task.
- Move through already-known transit cells without pausing.
- Continue through newly discovered ordinary cells when no encounter or point
  of interest is present.
- Award exploration experience when a new cell is entered, without requiring a
  visible stop.
- Continue spending movement stamina while walking and additional stamina while
  climbing.
- Replan when the intended route becomes invalid or a meaningful event occurs.

Implementation direction:

- Replace the unconditional wait at each destination with an investigation
  decision.
- Allow one planned route to contain multiple cells, ending at either:
    * an unexplored frontier,
    * an investigation target,
    * a decision point where multiple unknown routes branch,
    * or the stamina/retreat threshold.
- Record cell entry as the NPC crosses each cell boundary, rather than only when
  the full route finishes.


JUNCTION CHOICE AND FRONTIER PRIORITY
-------------------------------------

Desired behavior:

- At a junction, prefer connections that lead toward cells or branches the NPC
  has not explored.
- When several unexplored choices are reasonable, make a weighted random choice
  so different NPCs and visits do not always follow the same route.
- Avoid branches the NPC has fully explored and confirmed as dead ends unless an
  investigation target, return route, or other explicit goal requires them.
- Do not treat a merely unseen branch as a dead end. Dead-end knowledge must come
  from the NPC's own explored cells and familiar connections.
- Keep familiar-return routing goal-directed and safe; random exploration choices
  must never introduce an unknown shortcut while retreating.
- Preserve a chosen branch long enough to prevent frame-to-frame indecision or
  visible oscillation at the junction.

Implementation direction:

- Classify outgoing junction connections as unexplored, partially explored,
  familiar-through-route, or fully explored dead end.
- Give unexplored branches the highest exploration weight, partially explored
  branches a smaller weight, and known exhausted dead ends a weight of zero or a
  small fallback weight.
- Make the random choice once per decision point using the project's controlled
  random source, then retain that choice until traversal, invalidation, or a
  meaningful behavior-state change.
- Expose the candidate classifications, weights, and selected reason in debug
  output so repeated path choice can be diagnosed.


DEPTH-AWARE MOVEMENT WITHIN CELLS
---------------------------------

Desired behavior:

- NPC routes should include more variation along the world Z axis instead of
  always crossing each cell on the same flat line.
- Variations should come from authored walk lanes, portal/socket positions, and
  points of interest rather than unrestricted random Z offsets.
- NPCs may enter near the foreground, drift toward a background lane, or choose
  a different valid lane at an intersection when the tile supports it.
- Movement should remain readable and should never make an NPC appear to walk
  through walls, props, ledges, or another depth plane with no transition.

Implementation direction:

- Give traversable tile profiles one or more local movement lanes or waypoint
  sets, with explicit links to compatible entry/exit portals.
- Build a local route through the cell from the chosen entry portal to the chosen
  exit portal, then transform its authored XYZ points into world space.
- Use weighted route variants to reduce repetition. NPC personality, hazards,
  nearby party members, and points of interest may influence the selected lane.
- Preserve the selected local path long enough to avoid visible side-to-side
  indecision during a crossing.
- Reserve arbitrary procedural offsets for subtle animation only; they should not
  define collision or navigation.

If world generation later adds discrete background depth planes, local Z-lane
variation and a depth-plane transition must remain different concepts. An NPC may
move within a lane without changing planes; changing planes requires an explicit
navigable transition.


WHEN AN NPC SHOULD STOP
-----------------------

Potential investigation triggers:

- Trap detected
- Enemy encountered
- Treasure or loot discovered
- Door, switch, chest, shrine, or other interactive object found
- Unusual environmental feature found
- Route appears dangerous or obstructed
- Another adventurer needs assistance
- NPC chooses to rest, heal, or use equipment
- A major pathfinding decision requires evaluation

Ordinary empty cells should not trigger a stop.

Investigation duration and outcome may eventually depend on:

- Intelligence: identifying traps, enemies, and useful details
- Dexterity: disarming traps and handling precise interactions
- Strength: forcing objects, breaking obstacles, and physical outcomes
- Luck: discovery chance and uncertain outcomes
- Health: willingness to continue versus retreat
- Stamina: time available and willingness to perform optional tasks
- Level/experience: speed, confidence, and quality of decisions
- Equipment: detection, protection, tools, weapons, and supplies
- Playstyle/personality: cautious, curious, greedy, aggressive, supportive, etc.


TRAP DETECTION, AVOIDANCE, AND DODGING
--------------------------------------

Trap resolution should distinguish three moments:

1. Detection: the NPC notices a trap before triggering it.
2. Avoidance/disarming: the NPC deliberately routes around it or interacts with
   it before entering the danger area.
3. Reflex dodge: a triggered trap gives the NPC a last chance to avoid some or
   all of its effect.

Initial dodge direction:

- Dexterity should provide the main contribution to dodge chance.
- Luck should provide a smaller contribution and create occasional surprising
  successes or failures.
- Trap difficulty applies an opposing modifier.
- Equipment, status effects, party assistance, and prior knowledge may later add
  modifiers.
- Clamp the final probability to configurable minimum and maximum chances so an
  ordinary trap is neither always unavoidable nor automatically harmless.

Keep the formula data-driven. A useful prototype is:

    dodge score = dexterity weight + luck weight + situational modifiers
    success chance = evaluated curve(dodge score - trap difficulty)

The trap should request a dodge check through a central action-resolution system,
not read character fields independently. The result should report avoided,
partially avoided, or hit so damage, animation, sound, statistics, and UI all use
the same outcome.


SHORT-LIVED ACTION FEEDBACK UI
------------------------------

Add a brief world-space popup when an NPC completes or suffers a meaningful
action. Initial examples:

- "Dodged"
- Damage amount and damage type, where useful
- "Trap Detected" or "Disarmed"
- Healing or recovery
- "Party Formed" or "Joined Party"
- Important combat outcomes such as blocked, critical, or defeated

Behavior direction:

- Spawn near the acting or affected NPC, rise slightly, and fade quickly.
- Use consistent colors/icons for positive, harmful, social, and neutral events.
- Combine or stagger rapid repeated events so the screen remains readable.
- Keep debug messages separate from player-facing action feedback.
- Drive the popup from a resolved action event; game rules must never depend on
  whether the UI is present.
- Pool popup objects if frequent combat or traps would otherwise create excessive
  allocations.


SOCIAL ENCOUNTERS AND PARTIES
-----------------------------

When independent NPCs meet in the dungeon, there should be a chance for them to
pause and mingle. A successful interaction may form a party or add an NPC to an
existing party.

Suggested encounter conditions:

- NPCs are within a compatible cell or social radius and are not in immediate
  combat, fleeing, climbing, or resolving a trap.
- Neither NPC has recently declined or completed a social interaction with the
  other.
- The meeting has a safe location where a short pause will not block traversal.

Formation chance may depend on personality/playstyle, charisma or a future social
trait, current health, fear, dungeon danger, complementary roles, prior meetings,
and party capacity. Luck may provide a small uncertainty modifier.

Party navigation direction:

- Give a party one shared destination or leader route while each member retains
  an individual movement position.
- Followers choose nearby formation slots or adjacent local lanes rather than
  occupying the leader's exact point.
- Members wait briefly at transitions when the group is stretched, but do not
  force the entire party to stop at every cell.
- Ladders and other single-file traversal should temporarily queue the party and
  regroup it at the endpoint.
- Replan when a member is trapped, incapacitated, separated, or retreating.
- Define whether the party retreats together, may abandon a member, or can split
  by individual morale.

Knowledge direction:

- Party membership must not silently grant complete global dungeon knowledge.
- Decide whether mingling shares remembered cells, hazards, or traversed
  connections, and show that exchange as an explicit social result.
- If members know different routes, a leader may choose only information that was
  actually shared or personally learned.

Party state should have a stable party ID, leader, ordered members, maximum size,
formation spacing, current goal, and lifecycle events for join, leave, split,
leader change, and disband.


EXPLORATION MEMORY
------------------

Track these separately for each NPC during a dungeon visit:

- Visited cells
- Traversed connections between cells
- Direction(s) in which each connection was traversed
- Known investigation targets
- Resolved or exhausted investigation targets
- Known hazards and blocked routes
- Entrance/start cell
- Breadcrumb history or route history, if needed

Important rule:

A cell becomes visited when the NPC physically enters it. A connection becomes
known only after the NPC physically traverses it. Seeing a debug graph connection
does not mean the character knows that connection.

Initial recommendation:

- Treat normal connections as familiar in both directions after one successful
  traversal.
- Treat special one-way traversal connections separately if they are introduced.


FAMILIAR RETURN ROUTING
-----------------------

When returning to the entrance:

1. Build a temporary knowledge graph containing only connections this NPC has
   personally traversed during the current visit.

2. Find a route from the current cell to the entrance using only that knowledge
   graph.

3. Follow that route without stopping on ordinary cells.

4. Do not use unexplored shortcuts, even if the global navigation system knows
   they exist.

5. Returning should not consume stamina once stamina has reached zero, preserving
   the current guarantee that an exhausted adventurer can still leave.

Fallback behavior if no familiar return route exists:

- First choice: reverse the NPC's breadcrumb history until a familiar route to
  the entrance is restored.
- If the route was destroyed or made impassable during exploration, mark the NPC
  as stranded and use a future stranded/recovery behavior. Do not silently use an
  unknown route.

Why connections must be remembered:

If an NPC has visited cells A, B, and C via A -> B -> C, and the global dungeon
also contains an untraveled C -> A shortcut, the NPC knows the cells but does not
know that shortcut. The valid return is C -> B -> A.


STAMINA AND RETREAT
-------------------

Current direction to retain:

- Walking drains current stamina.
- Climbing drains current stamina at a higher configurable rate.
- Investigation and interaction tasks drain stamina over time.
- Current stamina resets at the beginning of a fresh visit.
- NPCs leave only after stamina reaches zero, unless another retreat condition is
  introduced (critical health, fear, missing equipment, explicit orders, etc.).

Future consideration:

- Reserve stamina for the trip home versus allowing free return at zero.
- Different playstyles may estimate return cost differently.
- An inexperienced or reckless NPC may overextend, while a cautious NPC may turn
  around early.


PROPOSED IMPLEMENTATION PHASES
------------------------------

Phase 1 - Continuous movement (Implemented; awaiting play-mode validation)

- Remove unconditional per-cell waiting.
- Preserve cell-entry experience and stamina costs.
- Add an explicit investigation decision hook.
- Verify continuous movement across floor and ladder routes.

Phase 2 - Personal route memory (Implemented; awaiting play-mode validation)

- Record each connection when it is physically traversed.
- Expose visited cells and familiar connections for debugging.
- Ensure route rebuilding does not automatically grant new knowledge.

Phase 3 - Familiar return path (Partially implemented)

- Add pathfinding restricted to the NPC's familiar connections.
- Retain breadcrumb history as a safe fallback.
- Prevent all global-graph shortcuts during return.
- Add an explicit stranded state when editing destroys every familiar route.

Phase 3A - Junction choice and frontier priority

- Classify outgoing paths using personal exploration memory.
- Prefer unexplored branches and vary equivalent choices with controlled random
  weighting.
- Avoid fully explored dead ends unless they serve a current goal.
- Add debug output for candidates, weights, and the selected branch.

Phase 4 - Investigation framework

- Define a common interface/data model for points of interest and encounters.
- Let a cell report whether it contains something worth investigating.
- Add investigation duration, stamina cost, outcome, and completion state.

Phase 5 - Trait-driven decisions

- Connect attributes, equipment, experience, and playstyle to detection,
  investigation, combat, trap resolution, and retreat decisions.

Phase 6 - Depth-aware local routes

- Author multiple safe XYZ waypoint lanes for a small test set of tiles.
- Connect local lanes to existing traversal portals/sockets.
- Select stable weighted variants and verify that collision and cell-entry events
  remain correct.

Phase 7 - Trap dodging and action feedback

- Add a central, data-driven dodge check using dexterity, luck, and trap
  difficulty.
- Make trap resolution produce one authoritative action result.
- Add pooled world-space popups for dodge, damage, and other initial outcomes.

Phase 8 - Social encounters and parties

- Detect safe NPC meetings and add a mingle cooldown.
- Add party formation, membership, leader, and disband state.
- Add close group navigation, formation spacing, single-file traversal queues,
  and separation recovery.
- Decide and implement explicit knowledge sharing.


DEBUG VISUALS
-------------

Implemented:

- Visited cells for the active NPC
- Familiar/traversed connections for the active NPC
- Current exploration route and next waypoint

Still to add:

- Junction candidates, exploration classifications, weights, and selected reason
- Planned familiar return route
- Current behavior state
- Current investigation target
- Reason for stopping or retreating
- Selected local Z lane and lane alternatives
- Last trap check, modifiers, probability, and result
- Party ID, leader, goal, formation slot, and separation distance
- Recent action-result events and popup pooling counts


INITIAL ACCEPTANCE CRITERIA
---------------------------

- An NPC crosses several empty cells without stopping at each one.
- Entering a new cell still awards experience and records the visit.
- Walking and climbing still drain current stamina.
- An investigation-worthy target causes a visible stop.
- After stamina reaches zero, the NPC returns using only connections it traversed.
- An untraveled shortcut to the entrance is never selected for the return route.
- Equivalent unexplored junction branches do not always resolve to the same path.
- A fully explored dead-end branch is avoided during ordinary exploration unless
  a current goal requires entering it.
- If the familiar route is unavailable, the NPC is reported as stranded instead
  of gaining unexplained dungeon knowledge.
- The dungeon closes after all scheduled NPCs have exited or died.
- NPCs use authored Z variation without clipping through tile geometry or changing
  depth planes without a valid transition.
- A triggered trap resolves one dodge check whose chance is influenced primarily
  by dexterity and secondarily by luck.
- Dodge and damage outcomes create brief readable UI feedback and do not change
  simulation behavior when the UI is disabled.
- Eligible NPCs can mingle, form a party, and travel close together without
  occupying the exact same position.
- A party queues through single-file traversal and regroups afterward without
  granting members unexplained route knowledge.


OPEN DESIGN QUESTIONS
---------------------

1. Can NPCs see into adjacent cells before entering them? If so, what information
   can be learned without traversing the connection?
   - maybe we add a perception trait for them that helps decide this. in general, if there are no doors or other barriers, they can see into the next cell, unless they have a relatively low perception.

2. Should one traversal teach both directions of a route in all cases?
  - for now yes

3. Should ordinary intersections cause a short decision animation without fully
   stopping movement?
   - Yes, stopping shortly would be fine too.

4. Should NPCs share discoveries with one another during or between visits?
 - As a later goal for adding these intersocial aspects, that would be a nice add. It would also be nice to see a log of interactions, or general events when clicking on an adventurer.

5. Does dungeon knowledge reset every visit, every dungeon rebuild, or persist
   as part of the adventurer's long-term memory? 
   - it should persist until the explorer encounters an unexpected cell. In forks where ther was one opttion they've explored and one they haven't, there should be a somewhat random choice for which they follow this time around (only when traveling into the dungeon)

6. What happens when editing changes or removes an NPC's remembered return route?
  - see question answer 5

7. Should cautious NPCs reserve enough stamina to walk home, or should return
   travel remain free once stamina reaches zero?
   - yes I like this idea. In the future, id like to force them to enter a "camping out" state in the dungeon where a small tent apears with 'zzzz' indicator above until they get enought stamina to exit the dugneon. For now we should just cause the npcs to walk slower if they are out of stamina.

8. Which first encounter type should be used to test investigation behavior:
   treasure, traps, enemies, or a neutral point of interest?
   - lets add a puzzle door that takes a set amount of investigation that progresses its unlocking

9. Should Z-lane choice be mostly random variation, personality-driven, or chosen
   tactically from hazards and points of interest?
   - mostly random unless there isn't a walkable floor under that lane. This would be handing for cases like visually broken ledges or thin bridges that are only centered.
   I would say for cases like long corridors, the npc would most likely maintain the same lane if available

10. Can an undetected trap still be reflex-dodged, and can a successful dodge
    avoid all damage or only reduce it?
    -Id like for strength to reduce it, but dodging avoid all together.
    

11. What minimum and maximum dodge chances should apply regardless of attributes?
    -something low like 5 to 10%

12. Which actions deserve player-facing popups, and should routine exploration
    experience remain silent?
    - invesitigating, attacks, out of stamina (exhausted) and general abrupt interactions should have popups. Routine exploration should be silent.

13. Which trait should govern social success if charisma is not added?
    - lets add charisma for this

14. What is the maximum party size, and can an NPC join a party already in
    progress?
    - maximum of 3, and yes

15. Does a party share discoveries automatically, during mingling, or only after
    leaving the dungeon?
    - automatically, and they should generally move together like a pod.

16. When one party member wants to retreat, does the party follow, split, or vote
    according to its leader and personalities?
    - id be open to leader and personalities contributing in the future, but for now we can split (or just combine the whole groups stamina into one "bar")
