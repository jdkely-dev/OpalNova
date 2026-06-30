# OPALNOVA V1.90.0 Stability Milestone

V1.90.0 is the whole-number stability checkpoint for the V1.81-V1.89 development run.

## Reviewed Scope

- V1.81 Market POS speed polish.
- V1.82 inventory media batch workflow.
- V1.83 supplier diamond replacement readiness.
- V1.84 production capacity snapshot.
- V1.85 proposal revision PDF-ready polish.
- V1.86 data integrity check.
- V1.87 workflow search finder.
- V1.88 practical jeweller tools.
- V1.89 release readiness prep.

## Redundancy And Safety Findings

- Same-section tool action duplicate scan found no duplicate action labels inside a single tool section.
- Repeated cross-studio actions remain intentional shortcuts so daily workflows can be opened from more than one relevant studio.
- V1.81-V1.89 did not add destructive database migration patterns.
- New data-safety and release-readiness tools are read-only report generators.
- New practical jeweller tools are local calculators and do not create database records.
- Workflow search additions navigate to existing sections and preserve saved-view behavior.
- Nivoda credentials remain user-entered; endpoint reset still resets URLs only and does not fill credentials.
- Installer creation, desktop shortcut creation, update channel, formal staging configuration and OS backup scheduling remain explicit future release decisions.

## Validation

- Debug build must pass with zero warnings and zero errors.
- Release publish must complete through `win-x64` self-contained output.
- Published `OPALNOVA.exe` must launch and close cleanly.
- Commit and push are appropriate at V1.90 because this is a whole-number milestone.

