# OPALNOVA V1.64.0 - Proposal Pipeline

V1.64.0 adds a focused workflow queue for proposal follow-up work.

## What Changed

- Added a hosted Proposal Pipeline workspace.
- Added filters for action needed, prepared not sent, follow-up due, sent, accepted, converted and all proposals.
- Added proposal counts, follow-up due counts and open proposal value summary.
- Added selected-proposal details for quote status, proposal status, expiry, generated time, sent time, follow-up due date, recipient, display option and proposal file.
- Added actions to open the exact quote, open the proposal file, copy the recorded email draft and create a duplicate-safe follow-up task.
- Added Proposal Pipeline entry points in the Workflow menu, Project Workbench, Alert Centre, Quotes & Proposals, Reports and Custom Workflow Studio.

## What Did Not Change

- No database schema changes.
- No proposal send/delivery automation was added.
- The existing quote builder remains the source of proposal generation, sending/recording, acceptance and job conversion.
- No quote pricing, stock reservation, production or payment calculations changed.

## Validation

- Debug build should complete with zero warnings and zero errors.
- Release publish should complete for `win-x64-self-contained`.
- Published `OPALNOVA.exe` should launch and close cleanly.
