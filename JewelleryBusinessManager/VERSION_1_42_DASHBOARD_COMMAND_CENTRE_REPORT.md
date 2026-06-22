# OPALNOVA V1.42 — Dashboard Command Centre

## Purpose
V1.42 turns the dashboard into the daily operating screen for OPALNOVA after the quote, production and payment workflows were connected.

## Added
- Command Centre priority panel on the dashboard.
- Attention cards for:
  - Quotes awaiting approval
  - Overdue jobs
  - Jobs due this week
  - Ready for collection
  - Ready to ship
  - Jobs with balances owing
  - Reserved quote inventory
  - Low stock alerts
  - Customer follow-ups
  - Recent payments
  - Sales this week
  - Sales this month
- One-click dashboard actions for:
  - Production Board
  - Payment & Collection
  - Custom Quotes
  - Follow-Ups
  - Create Backup
  - Weekly Report

## Safety
- No database schema changes.
- No changes to save/load logic.
- No changes to quote acceptance, reservation, production board or payment workflow logic.
- Existing V1.41 workflow remains intact.

## Testing notes
Recommended checks:
1. Open the dashboard and confirm all new cards display values.
2. Click Production Board from the dashboard.
3. Click Payment & Collection from the dashboard.
4. Click Custom Quotes from the dashboard.
5. Click Follow-Ups from the dashboard.
6. Create a backup from the dashboard.
7. Generate the Weekly Report from the dashboard.
8. Publish standalone and open OPALNOVA.exe.
