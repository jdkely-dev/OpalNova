# OPALNOVA V1.39 — Quote Inventory Linking & Reservations

## Added
- Link actual loose stones to each custom quote option.
- Link actual materials with quantity and unit cost.
- Linked inventory costs flow into the live quote price.
- Accepted quote options reserve their linked stock.
- Unique stones cannot be reserved by two accepted options.
- Material availability accounts for quantities reserved by other accepted quotes.
- Reservations can be released without deducting or corrupting stock quantities.
- Accepted allocations are copied into the linked production job notes.
- Existing quote options can be duplicated with proposed inventory links.

## Data safety
This release adds two upgrade-safe tables:
- QuoteOptionStoneLinks
- QuoteOptionMaterialLinks

Existing customer, inventory, quote, job, sale and payment records are not replaced.
Reservations do not directly reduce inventory quantities.

## Suggested test
1. Create a quote with two options.
2. Link a unique stone and material to Option A.
3. Confirm linked costs update the quote total.
4. Save, close and reopen the quote.
5. Accept Option A and confirm links show Reserved.
6. Try reserving the same stone in another quote; OPALNOVA should block it.
7. Try reserving more material than remains available; OPALNOVA should block it.
8. Release reservations and confirm the stock becomes available again.
9. Re-accept the option and create a production job.
10. Confirm the job notes include the allocated stone and material lines.
