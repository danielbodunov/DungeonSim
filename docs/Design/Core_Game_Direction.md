# Core Game Direction

## Player Fantasy

The player is a sinister force inhabiting and cultivating a dungeon.

The player does not primarily explore the dungeon themselves. They shape it to attract, manipulate, exploit, and sometimes kill adventurers who enter seeking wealth, glory, or discovery.

The dungeon should feel like an active predatory ecosystem rather than a static maze or conventional tower-defense board.

## Core Gameplay Loop

**Build and bait → attract adventurers → observe/manipulate expeditions → harvest soul energy and recover spoils → expand/improve the dungeon → attract more valuable prey**

The optimal dungeon should not simply kill every adventurer as quickly as possible. Long, dangerous, dramatic expeditions can create more value and more interesting consequences than immediate death.

## Treasure Is Bait

Treasure placed in the dungeon is fundamentally dungeon-owned value risked to entice adventurers.

- Available treasure attracts or rewards exploration.
- An adventurer who takes treasure carries it out of dungeon ownership.
- If that adventurer dies, the dungeon can recover its treasure and potentially acquire valuables the adventurer brought with them.
- If that adventurer escapes, the carried treasure is genuinely lost from the dungeon.

This creates a risk/reward decision for the player: valuable bait may draw better or greedier adventurers deeper, but it can also be stolen.

Treasure should therefore not automatically convert into player currency merely because an NPC finds it.

## Soul Energy / Aura

Soul energy/Aura is the dungeon's supernatural progression resource.

The long-term direction is for adventurer activity inside the dungeon—not only death—to potentially generate harvestable energy. Examples may eventually include exploration, fear, pain, traps, combat, magic use, or death.

Exact generation rules should be introduced incrementally and balanced through later tickets. Death can provide a substantial harvest without becoming the only valuable expedition outcome.

## Adventurer Outcomes

An expedition should eventually have explicit outcomes such as:

- escaped successfully;
- retreated;
- defeated/killed;
- potentially other story-relevant outcomes later.

These outcomes can affect treasure ownership, loot, Aura, reputation, and future adventurer behavior differently.

## Adventurers as Story Generators

NPC knowledge, personality, greed, fear, injuries, discoveries, survival, and death should create small emergent stories.

Examples:

- A greedy adventurer pushes one room farther for visible treasure and dies to a trap.
- A wounded survivor escapes with dungeon treasure.
- A cautious adventurer retreats early and may later contribute to the dungeon's reputation.
- A powerful adventurer penetrates unusually deep before becoming valuable prey.

Systems should favor understandable decisions and observable consequences so these stories are legible to the player.

## Survival Is Not Necessarily Failure

An adventurer escaping should not automatically mean the player failed.

A future reputation/notoriety system may make survivors useful because they spread stories of danger and treasure, attracting stronger or wealthier adventurers. This creates tension between harvesting an adventurer now and allowing a survivor to increase future opportunity.

Reputation is a future direction, not a requirement of the current expedition tickets.

## Dungeon Management Focus

The core game remains dungeon management/simulation.

Likely long-term player tools include:

- dungeon topology and expansion;
- treasure/bait placement;
- traps;
- monsters/minions;
- rooms and specialized dungeon functions;
- resource investment;
- manipulation of adventurer choices and risk;
- observation/debug-like information presented as player-facing dungeon knowledge.

Each system should be evaluated partly by how it helps the player influence adventurer behavior.

## Tower-Defense-Style Raids

Large organized raids remain a possible future bonus/alternate mode.

They may reuse dungeon construction, traps, monsters, NPC navigation, and adventurer-party systems, but they are not currently part of the core progression loop and should not drive near-term architecture.

Do not introduce raid-specific abstractions into core systems unless a future ticket explicitly activates this direction.

## Design Principles

1. **The player manipulates adventurers rather than directly controlling them.**
2. **NPCs act from their own knowledge.** Unknown treasure or routes should not influence them magically.
3. **Treasure is an investment and lure, not free reward generation.**
4. **Death is valuable but should not always be the only optimal outcome.**
5. **Escapes, retreats, discoveries, and deaths should create understandable stories.**
6. **Dungeon layout should meaningfully influence NPC choices and outcomes.**
7. **Add complexity only when it strengthens the core management loop.**
8. **Preserve room for future raids without designing the core game around tower defense.**
