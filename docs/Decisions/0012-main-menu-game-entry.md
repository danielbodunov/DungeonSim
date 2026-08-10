# 0012: Main Menu Game Entry

- Status: Accepted
- Date: 2026-08-09

## Context

The prototype currently enters the gameplay loop directly. A complete game needs a safe entry point for starting, resuming, loading, and configuring play.

## Decision

The main menu provides Continue, New Game, Load Game, and Options, plus Quit in standalone builds. Continue loads the most recently used valid save and is disabled when no valid save exists. New Game starts clean progression without deleting existing save slots. Load Game opens the named save browser, and Options stores global settings separately from dungeon saves.

## Consequences

- Dungeon simulation must not run behind the main menu.
- Save discovery needs a reliable most-recent-valid result for Continue.
- The existing save browser should be reusable outside the gameplay HUD.
- Options need their own persistence and pointer, keyboard, and controller navigation.
- The exact definition of "most recently used" remains to be finalized.

See [Main Menu and Game Entry](../Design/Visual_and_Interaction_Design.md#main-menu-and-game-entry).
