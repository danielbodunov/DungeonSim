# 0013: Shared UI Theme

- Status: Accepted
- Date: 2026-08-09

## Context

The current runtime HUD defines colors and dimensions directly in code. Adding a main menu, inspectors, progression screens, and accessibility variants this way would create inconsistent visuals and expensive global restyling.

## Decision

UI presentation comes from a shared `UITheme` ScriptableObject containing semantic colors, typography, control states, surface assets, progress styles, layout metrics, motion values, and world-feedback colors. UI components request semantic tokens rather than copying raw visual values.

## Consequences

- Existing hard-coded HUD and popup values should migrate to the theme before major new screens are styled.
- A readable project default and built-in fallback are required.
- Runtime theme changes should refresh existing UI without scene reload.
- Gameplay rules, localized text, and behavior do not belong in the theme.

See [Shared UI Theme](../Design/Visual_and_Interaction_Design.md#shared-ui-theme).
