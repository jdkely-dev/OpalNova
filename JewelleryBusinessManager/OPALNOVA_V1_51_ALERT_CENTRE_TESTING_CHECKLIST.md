# OPALNOVA V1.51 Alert Centre Testing Checklist

## Build And Launch

- Open the published `OPALNOVA.exe`.
- Confirm the header shows `Version 1.51.0`.
- Confirm the dashboard loads without errors.
- Confirm the new `Next Actions` dashboard tile shows a number.

## Alert Centre Entry Points

- Open Alert Centre from the quick toolbar.
- Open Alert Centre from the dashboard one-click actions.
- Click the dashboard `Next Actions` tile and confirm Alert Centre opens.
- Open the Workflow menu and choose Alert Centre.
- Open the Project Workbench section and confirm the Alert Centre tool action is available.

## Alert Centre Behaviour

- Confirm the Alert Centre opens inside a workspace tab.
- Confirm the tab close button closes only the Alert Centre tab.
- Confirm the bottom Close button closes the current Alert Centre tab.
- Confirm search filters rows by customer/project/title/detail/action text.
- Check filters: Action needed, Urgent, High, Quotes, Production, Payments, Diamonds, Inventory, Follow-ups, All alerts.
- Confirm summary counts update to match the visible filtered rows.
- Select rows and confirm the detail panel updates.
- Double-click a row and confirm it opens the intended workflow.
- Use the primary action button and confirm it opens the intended workflow.

## Alert Data Scenarios

- Create or identify an expired quote and confirm it appears as urgent.
- Create or identify a sent quote with follow-up due and confirm it appears.
- Create or identify an accepted quote without linked job and confirm it appears.
- Create or identify an overdue production job and confirm it appears.
- Create or identify a ready-for-pickup/ship job with balance owing and confirm it appears as payment-related.
- Create or identify a supplier diamond hold expiring within 24 hours and confirm it appears.
- Create or identify a material at or below reorder level and confirm it appears.
- Create or identify an overdue/high-priority task and confirm it appears.

## Dashboard Setup Readiness

- Confirm the Setup Readiness card appears on the dashboard.
- Confirm the progress bar updates based on existing settings and data.
- Click Continue Setup and confirm it opens the expected next setup area.
- Confirm Create Backup still opens the backup workflow.

## Regression Checks

- Open Project Workbench and confirm existing rows still load.
- Open Custom Quotes and confirm the V1.50 proposal/send workflow still opens.
- Open Production Board and Payment & Collection from Alert Centre actions.
- Confirm dark theme styling remains readable.
- Test at Windows scaling 100%, 125%, and 150% if possible.
