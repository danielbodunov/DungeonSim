# Known Issues and Follow-ups

This document records issues discovered while implementing or investigating a requested feature or task when those issues are outside the task's approved scope.

Finding an issue does not authorize an unrelated fix. Unless the issue is part of the requested work, it should be recorded here and left unchanged so it can be reviewed and prioritized deliberately.

## Working rule

When an unrelated issue is discovered:

1. Perform only enough read-only investigation to describe the issue accurately.
2. Do not fix, refactor, or expand the active task to include it automatically.
3. Add an entry under **Open issues and follow-ups** using the template below.
4. Continue the requested task when the discovered issue does not block it.
5. Mention the new entry in the task handoff so it is visible without interrupting the current work.

If an issue blocks the requested task, risks data loss, or requires a decision that would materially change the implementation, stop and ask for direction rather than assuming permission.

## Status labels

- `Observed`: Noticed during development but not fully reproduced.
- `Confirmed`: Reproduced or supported by clear evidence.
- `Prioritized`: Approved for future implementation.
- `In Progress`: Being handled as an explicitly requested task.
- `Resolved`: Fixed and verified.
- `Deferred`: Intentionally postponed.
- `Won't Fix`: Reviewed and intentionally left unchanged, with a recorded reason.

## Issue entry template

Copy this template for each newly discovered issue:

```md
### KI-YYYY-MM-DD-### - Short descriptive title

- **Discovered:** YYYY-MM-DD
- **Status:** Observed
- **Area:** System, feature, scene, or asset
- **Discovered while:** Requested feature or task that exposed the issue
- **Description:** What appears to be wrong
- **Impact:** Player, designer, performance, stability, or maintenance impact
- **Evidence:** Reproduction steps, logs, screenshots, or relevant file references
- **Suggested follow-up:** The smallest reasonable investigation or next action
- **Scope note:** Why this was documented instead of changed during the active task
```

Use a three-digit sequence for entries created on the same day, beginning with `001`.

## Open issues and follow-ups

No issues have been recorded yet.

## Resolved or closed issues

Move entries here when their status becomes `Resolved` or `Won't Fix`. Preserve the original discovery information and add the resolution date, verification, or reason for closure.

