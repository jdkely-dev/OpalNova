# OPALNOVA V1.60.0 - Tax and GST Summary

## Build Scope

V1.60.0 adds a read-only Tax / GST Summary report for bookkeeping review using existing sales, payments, job balances and business tax settings.

## Changes

- Added `DocumentExportService.CreateTaxSummaryReport()`.
- Added Tax / GST Summary actions in quick Reports and Reports Studio.
- Added period summaries for:
  - current month.
  - financial quarter to date.
  - financial year to date.
  - last 12 months.
- Added estimated tax component from recorded sale totals when the business is marked tax/GST registered.
- Added financial-year sales by sale location.
- Added financial-year payments by payment method.
- Added current job balance summaries for jobs received in each period.
- Added tax/payment data checks for zero-amount sales, unlinked payments, missing sale/job links and open job balances.
- Updated visible version text, release notes, About text, roadmap and handoff notes.

## Data Safety

- No database schema changes.
- No sale, payment, job, inventory, customer or settings records are modified.
- The report reads existing values only:
  - `Sale.SaleDate`
  - `Sale.SaleAmount`
  - `Sale.CostOfGoods`
  - `Payment.PaymentDate`
  - `Payment.Amount`
  - `Job.BalanceOwing`
  - `BusinessSettings.GstRegistered`
  - `BusinessSettings.GstRatePercent`
  - `BusinessSettings.TaxLabel`

## Interpretation Notes

- Tax/GST is estimated as the tax-inclusive component of recorded sale totals.
- If the business is not marked registered in Settings, the tax component is shown as zero.
- Current job balances are current balances on matching jobs; OPALNOVA does not reconstruct historical period-end balances.
- This report is for operational bookkeeping review, not formal tax advice.
