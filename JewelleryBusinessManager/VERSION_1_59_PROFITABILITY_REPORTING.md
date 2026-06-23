# OPALNOVA V1.59.0 - Profitability Reporting

## Build Scope

V1.59.0 adds a read-only profitability report to help compare what is actually making money across product/service categories and job types.

## Changes

- Added `DocumentExportService.CreateProfitabilityReport()`.
- Added Profitability actions in the quick Reports workspace and Reports Studio.
- Added product/service category profit grouping for:
  - linked jewellery stock sales.
  - linked job-work sales.
  - unlinked sales grouped by sale location.
- Added recorded profit by job type for sales linked to jobs.
- Added estimated job profit by job type using existing job price and cost fields.
- Added recent sales profit detail for the latest 40 sale records.
- Added data-quality checks for unlinked sales, missing links, zero-cost sales, jobs without prices and priced jobs without costs.
- Added the product/service category profit table into the BI command report for the current month.
- Updated visible version text, release notes, About text, roadmap and handoff notes.

## Data Safety

- No database schema changes.
- No sale, job, inventory, payment or customer records are modified.
- The report uses existing recorded values:
  - `Sale.SaleAmount`
  - `Sale.CostOfGoods`
  - `Sale.Profit`
  - `JewelleryItem.Type`
  - `Job.Type`
  - `Job.FinalPrice` or `Job.QuoteAmount`
  - `Job.MaterialCost` and `Job.LabourCost`

## Known Interpretation Notes

- Unlinked sales cannot be assigned to a jewellery category or job type.
- Sales with zero cost of goods can overstate profit.
- Job profit is estimated unless sale/payment records are fully linked and costs are maintained.
- The report is designed for operational review, not formal accounting advice.
