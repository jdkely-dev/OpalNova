# OPALNOVA V1.47.4 — Friendly Selector Placeholders

## Purpose

This patch improves selector/input usability after the tabbed UI and theme polish update. Selector fields now show simple prompts such as **Select customer**, **Select existing quote**, **Select stone**, **Select material**, and **Select external diamond** instead of preselecting a record or displaying cluttered object text before the user chooses anything.

## Changed

- Custom Quote Builder selectors now start with friendly placeholder rows.
- New quote customer selector now displays **Select customer** until a customer is chosen.
- Existing quote selector now displays **Select existing quote** until a quote is chosen.
- Owned stone, material, and external diamond selectors now display simple prompts first.
- Dynamic Add/Edit lookup fields now use friendly prompts such as **Select customer**, **Select job**, **Select material**, and **Select stone**.
- Dynamic lookup display labels no longer prefix every option with the raw numeric database ID.
- Tool record selectors now begin with **Select record type/item** style prompts instead of auto-selecting the first record.
- Stock movement, market sale, and production-batch selectors now use clearer first-choice prompts.

## Kept stable

- No database schema changes.
- No quote calculation changes.
- No Nivoda API changes.
- No supplier diamond workflow changes.
- No payment, production, sale, or reporting logic changes.

## Suggested test

1. Open **New Quote**.
2. Confirm Customer says **Select customer** before selection.
3. Confirm Existing Quote says **Select existing quote** before selection.
4. Confirm stone/material/external diamond selectors show friendly prompts first.
5. Open a normal record Add/Edit tab and confirm linked ID fields show simple prompts.
6. Save a quote after selecting a real customer.
7. Try leaving customer unselected and confirm the quote still saves as an unlinked draft if desired.
