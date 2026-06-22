# OPALNOVA V1.16 — Market Event Pro

## Purpose

V1.16 improves the market-stall workflow by adding more practical market preparation, sale recording, reconciliation and reporting tools.

## Added features

- New Market Pro action menu
- Market Prep workflow
- Market Sale window
- Reconcile Market window
- Market Packing List report
- Market Reconciliation report
- Extra market event tracking fields
- Extra market stock tracking fields
- Market dashboard tiles

## New Market Event fields

- Opening Float
- Cash Sales
- Card Sales
- Other Sales
- Travel Cost
- Display Cost
- Other Costs
- Items Packed
- Items Sold
- Items Returned
- Last Reconciled At
- Packing Checklist
- Reconciliation Notes

## New Market Stock fields

- Packed At
- Sold At
- Returned To Stock
- Sale Price
- Payment Method Text
- Sale Id

## Workflow

Recommended market workflow:

1. Create a Market Event.
2. Add jewellery items to the market.
3. Use Market Prep to generate/prepare the packing checklist.
4. Use Packing List to print the stock/packing list.
5. Use Market Sale to record items sold at the market.
6. Use Reconcile Market to enter cash/card totals and market costs.
7. Use Reconciliation Report to review market performance.

## Database safety

V1.16 uses ALTER TABLE checks for new optional columns so existing databases can upgrade without deleting data.

The V1.11.3 database backup/export/restore file-lock fixes are preserved.

## Validation

Two static validation passes were run before packaging.
