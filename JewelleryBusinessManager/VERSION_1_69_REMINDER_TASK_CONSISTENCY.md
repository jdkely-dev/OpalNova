# OPALNOVA V1.69.0 - Reminder Task Consistency

V1.69.0 cleans up reminder and follow-up task creation across active workflow screens.

## Implemented

- Added shared `TaskWorkflowService.OpenTaskExists(...)` duplicate-safe open-task detection.
- Reused the shared duplicate check in quote follow-ups, sent-proposal follow-ups, Proposal Pipeline follow-ups, Project Workbench follow-ups, balance follow-ups, pickup/handover reminders and supplier diamond reminders.
- Replaced remaining ad-hoc active workflow task-code creation with `TaskWorkflowService.GenerateTaskCode()`.
- Made Payment & Collection pickup/handover reminders duplicate-safe for the selected job.
- Made pickup/handover reminders visible on the dashboard/work queue.

## Preserved

- No database schema changes.
- Existing `BusinessTask` records remain valid.
- Existing quote, proposal, payment, handover and supplier-diamond workflow behavior is preserved apart from duplicate open-task prevention.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
