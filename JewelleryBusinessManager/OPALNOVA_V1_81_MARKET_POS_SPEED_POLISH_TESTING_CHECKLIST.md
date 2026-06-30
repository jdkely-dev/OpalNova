# OPALNOVA V1.81.0 Market POS Speed Polish Testing Checklist

Use this checklist after launching the V1.81.0 build.

- [ ] Confirm the main header shows `Version 1.81.0 - market POS speed polish`.
- [ ] Open About and confirm it shows `Version 1.81.0 - Market POS Speed Polish`.
- [ ] Open Release Notes and confirm V1.81.0 appears above V1.80.0.
- [ ] Open Hardware & POS Studio or Market Operations Studio.
- [ ] Select a market event and confirm the register shows stock rows with readable packed, sold and returned state.
- [ ] Search by stock code, item name or state and confirm the register filters without layout issues.
- [ ] Select an unsold, not-returned market stock item and preview it on the customer display.
- [ ] Record a market sale and confirm the status message reports the sold item and amount.
- [ ] Reopen or refresh the market register and confirm the row now shows sold state and the payment method.
- [ ] Open Record Market Sale and confirm already-returned stock does not appear in the active sale selector.
- [ ] Try returning an unsold market stock item and confirm the row changes to returned state.
- [ ] Confirm sold market stock cannot be returned using the quick return action.
- [ ] Open Market Reconcile for the same event and confirm the guidance shows recorded stock sales, payment breakdown and difference.
- [ ] Adjust cash/card/other totals and confirm the reconciliation guidance updates before saving.
- [ ] Save reconciliation and confirm the market remains available with the same stock rows.
- [ ] Open a packing list report and confirm packed state is shown with readable `Yes`/`No` text.
- [ ] Confirm existing jewellery inventory records still open normally.
- [ ] Confirm existing sales reports still open normally after the market sale.
- [ ] Close OPALNOVA cleanly.
