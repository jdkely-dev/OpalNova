# OPALNOVA V1.48 Project Workbench Testing Checklist

## Startup

- [ ] Open OPALNOVA from Visual Studio.
- [ ] Confirm the app starts normally.
- [ ] Confirm version text shows V1.48.

## Navigation

- [ ] Click **Project Hub** in the quick toolbar.
- [ ] Click **Project Workbench** in the left sidebar.
- [ ] Click **Workflow > Project Workbench** from the top menu.
- [ ] Confirm the same Project Workbench tab opens or focuses cleanly.

## Workbench data

- [ ] Confirm counts load for Needs Action, Quotes, Production, Balances, Diamonds and Follow-ups.
- [ ] Test filters: Action needed, Quotes, Production, Payments, Diamonds, Follow-ups, All projects.
- [ ] Type into search and confirm rows filter.
- [ ] Select rows and confirm the right-side detail panel updates.

## Workflow actions

- [ ] Select a quote row and click the main action button.
- [ ] Confirm Custom Quote workflow opens in a tab.
- [ ] Select a production row and click the main action button.
- [ ] Confirm Production Board opens in a tab.
- [ ] Select a payment/balance row and click the main action button.
- [ ] Confirm Payment & Collection opens in a tab.
- [ ] Select a diamond row and click the main action button.
- [ ] Confirm Diamond Holds opens in a tab.

## Customer message helper

- [ ] Select any row with a suggested message.
- [ ] Click Copy Message.
- [ ] Paste into Notepad and confirm the text copied cleanly.

## Follow-up task creation

- [ ] Select a row.
- [ ] Click Create Follow-Up Task.
- [ ] Confirm success message appears.
- [ ] Open Tasks and confirm the task was created.
- [ ] Confirm customer/job links were added where possible.

## Regression checks

- [ ] Open New Quote.
- [ ] Open Production Board.
- [ ] Open Payment & Collection.
- [ ] Open Diamond Search.
- [ ] Open Diamond Holds.
- [ ] Confirm existing V1.47.11 tab/header UI still looks correct.
- [ ] Publish standalone.
- [ ] Open published OPALNOVA.exe.
