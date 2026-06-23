# OPALNOVA V1.61.0 Visual Charts Testing Checklist

Use this checklist against the published V1.61 build.

## Startup

- Launch OPALNOVA.
- Confirm the header shows `Version 1.61.0 - visual report charts`.
- Open About and confirm it shows V1.61.0.

## Quick Reports

- Open the main Reports workspace.
- Click Visual Charts.
- Confirm the report opens in the in-app preview area.
- Confirm Open HTML / Print still works from the preview controls.

## Reports Studio

- Open Reports Studio.
- Click Visual Charts.
- Confirm the same visual chart report opens.
- Hover or select the tool and confirm the help text describes sales, quote conversion, inventory, payment and balance charts.

## Chart Sections

- Confirm the report shows:
  - Sales by Month.
  - Profit by Month.
  - Quote Conversion by Month.
  - Inventory Value Snapshot.
  - Payments Received by Month.
  - Outstanding Balances by Job Status.

## Spot Checks

- Compare Sales by Month against the Monthly Sales report for the current month.
- Compare Payments Received by Month against known Payment records.
- Compare Quote Conversion by Month against the Quote Conversion report.
- Compare Inventory Value Snapshot against the Inventory Value report.
- Compare Outstanding Balances by Job Status against the Outstanding Balances report.

## Empty / Small Data

- Confirm the report still renders if a chart has no records.
- Confirm chart labels and values do not overlap.
- Confirm the report remains readable when printed or opened in a browser.

## Safety

- Confirm generating the report does not add, edit or delete records.
- Close and reopen OPALNOVA.
- Confirm existing reports and records still load normally.
