# OPALNOVA V1.78.0 Payment Schedule Guidance Testing Checklist

Use this checklist after launching the V1.78.0 build.

- [ ] Confirm the main header shows `Version 1.78.0 - payment schedule guidance`.
- [ ] Open About and confirm it shows `Version 1.78.0 - Payment Schedule Guidance`.
- [ ] Open Custom Quote Builder and select or create a quote option with a total price.
- [ ] Confirm the quote total area shows deposit, final balance and remaining-payment guidance.
- [ ] Preview or generate a proposal and confirm the proposal contains a Payment schedule section with Deposit and Final balance rows.
- [ ] Use the proposal send workflow and confirm the draft message still opens/copies normally.
- [ ] Open Payment & Collection and select a job with a quote/final price.
- [ ] Confirm the Payment Schedule panel shows deposit target, final balance target, paid amount, remaining amount and due guidance.
- [ ] Record a small test payment only if using test data, then confirm the schedule remaining amount updates after reload.
- [ ] Generate a job payment summary and confirm the schedule table appears before payment history.
- [ ] Generate an invoice/receipt and confirm existing totals, paid amount and balance still look correct.
- [ ] Copy a balance reminder and confirm it still uses the current job total, paid amount and balance.
- [ ] Confirm opening schedule guidance does not create, edit or delete any payment records.
- [ ] Confirm no database migration prompt or schema-change behavior appears.
- [ ] Close the app cleanly.
