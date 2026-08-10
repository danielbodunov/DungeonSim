\# DungeonSim Ticket



\## Title



\[Short, descriptive title]



\## Type



\[Feature / Bug Fix / Refactor / Tooling / Investigation]



\## Summary



\[1–3 sentences describing what this ticket should accomplish.]



\---



\## Current Behavior



\[Describe how the system currently behaves.]



If this is a new feature with no existing behavior:



`Not applicable — new feature.`



\---



\## Desired Behavior



\[Describe what should happen after this ticket is implemented.]



Focus on observable behavior rather than prescribing the implementation unless a particular implementation is required.



\---



\## Requirements



\* \[Required behavior]

\* \[Required behavior]

\* \[Required behavior]



These are the functional requirements for the ticket.



\---



\## Acceptance Criteria



The ticket is complete when:



\* \[Concrete, verifiable result]

\* \[Concrete, verifiable result]

\* \[Concrete, verifiable result]

\* Existing unrelated behavior remains unchanged.



\---



\## Relevant Systems / Files



\[Optional]



Known systems, components, scripts, assets, prefabs, or scenes related to the request:



\* `\[System / file / asset]`

\* `\[System / file / asset]`



These are starting points, not necessarily the complete implementation boundary. Investigate the existing architecture before making changes.



\---



\## Constraints



\[Optional]



Ticket-specific restrictions that go beyond the repository's `AGENTS.md` rules.



Examples:



\* Do not change the existing Tile Profile data format.

\* Maintain backward compatibility with existing generated profiles.

\* Do not modify prefabs as part of this ticket.

\* This feature must work in Edit Mode.

\* Avoid adding a new MonoBehaviour.

\* Existing saves must continue to load.



If there are no additional constraints:



`None beyond AGENTS.md.`



\---



\## Implementation Notes



\[Optional]



Information that may help explain the intended design, but is not itself an acceptance criterion.



Examples:



\* There is already a helper method in `TileProfile.cs` that may be relevant.

\* I would prefer this to build on the existing placement pipeline.

\* The problem appears to happen after rotated profiles are evaluated.

\* This should probably remain editor-only.



Codex should still investigate the existing implementation before assuming these notes are correct.



\---



\## Reproduction Steps



\[For bug tickets when applicable]



1\. \[Step]

2\. \[Step]

3\. \[Step]



\### Actual Result



\[What currently happens.]



\### Expected Result



\[What should happen.]



\---



\## Manual Test Scenario



\[Optional but recommended for Unity features]



A useful Unity Editor scenario for validating this ticket:



1\. \[Open scene/prefab/tool]

2\. \[Configure relevant state]

3\. \[Perform action]

4\. \[Expected result]



Codex should include any additional manual validation it believes is necessary in its post-implementation report.



\---



\## Out of Scope



\[Optional but useful for tickets near other systems]



Explicitly excluded from this ticket:



\* \[Related feature that should NOT be changed]

\* \[Potential cleanup that should NOT be performed]

\* \[Future functionality]



If nothing specific needs to be excluded:



`Anything not required by this ticket's requirements or acceptance criteria.`



\---



\## Git



Create/use an appropriate dedicated ticket branch according to `AGENTS.md`.



Suggested branch name:



`\[feature/fix/refactor/tool]/\[short-ticket-name]`



Do not merge into `master`.



\---



\## Additional Context



\[Optional]



\[Any screenshots, error messages, previous decisions, examples, edge cases, or other useful context.]



\---



Proceed according to the repository's `AGENTS.md`.



Before implementation, inspect the existing systems sufficiently to understand the current architecture and determine the smallest appropriate implementation boundary.



After implementation, provide the standard post-implementation report required by `AGENTS.md`.



