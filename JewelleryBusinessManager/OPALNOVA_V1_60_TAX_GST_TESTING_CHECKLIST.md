# OPALNOVA V1.60.0 Tax / GST Testing Checklist

Use this checklist against the published V1.60 build.

## Startup

- Launch OPALNOVA.
- Confirm the header shows `Version 1.60.0 - tax and GST summary`.
- Open About and confirm it shows V1.60.0.

## Settings

- Open Settings.
- Confirm the GST/tax registered checkbox, tax label and rate still load.
- Confirm you can close Settings without changing anything.

## Quick Reports

- Open the main Reports workspace.
- Click Tax / GST Summary.
- Confirm the report opens in the in-app preview area.
- Confirm Open HTML / Print still works from the preview controls.

## Reports Studio

- Open Reports Studio.
- Click Tax / GST Summary.
- Confirm the same report opens.
- Hover or select the tool and confirm the help text describes tax estimates, payments and data checks.

## Report Content

- Confirm the report shows:
  - tax setting.
  - estimate method.
  - financial year date range.
  - current outstanding job balances.
  - Tax Period Summary.
  - Financial Year Sales by Location.
  - Financial Year Payments by Method.
  - Tax / Payment Data Checks.
  - Financial Year Sales Detail.

## Period Checks

- Compare current month sales total against the Monthly Sales report.
- Compare financial-year sales by location against known Sales records.
- Compare payment method totals against known Payment records.
- Confirm GST/tax estimate is zero when GST registration is disabled.
- If GST registration is enabled, confirm the tax estimate is calculated from the gross sale total using the configured rate.

## Data Checks

- If any payments are not linked to a job or sale, confirm the unlinked payment count is greater than zero.
- If any sale has zero amount, confirm the zero-amount sale count is greater than zero.
- If any active job has balance owing, confirm the open balance count is greater than zero.

## Safety

- Confirm generating the report does not add, edit or delete records.
- Close and reopen OPALNOVA.
- Confirm existing sales, payments, jobs and settings still load normally.
