# OPALNOVA V1.40 — Visual Production Board

## Added
- Visual horizontal job pipeline for the workshop.
- Job cards showing customer, quote, due date and outstanding balance.
- Overdue highlighting, search, active-only and overdue-only filters.
- Quick Move Forward / Move Back controls with confirmation.
- Double-click or button access to the existing job editor.
- New additive stages: Setting, Polishing and Quality Check.
- Existing persisted JobStatus numeric values were explicitly preserved.
- Production Board access from Custom Workflow Studio and Production & Opal Studio.

## Data safety
No tables were removed or replaced. The existing Jobs table continues to store status as an integer. Existing enum values retain their original numeric values; new stages use values 11–13.

## Suggested testing
1. Open Production Board from either studio.
2. Confirm existing jobs appear in the correct columns.
3. Search by job, customer and linked quote.
4. Test Active only and Overdue only.
5. Select a card and move it forward/back.
6. Double-click a card, edit it, save, and verify refresh.
7. Restart OPALNOVA and confirm stages persist.
