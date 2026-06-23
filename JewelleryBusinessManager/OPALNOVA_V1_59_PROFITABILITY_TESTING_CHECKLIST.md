# OPALNOVA V1.59.0 Profitability Testing Checklist

Use this checklist against the published V1.59 build.

## Startup

- Launch OPALNOVA.
- Confirm the header shows `Version 1.59.0 - profitability reporting`.
- Open About and confirm it shows V1.59.0.

## Quick Reports

- Open the main Reports workspace.
- Click Profitability.
- Confirm the report opens in the in-app preview area.
- Confirm Open HTML / Print still works from the preview controls.

## Reports Studio

- Open Reports Studio.
- Click Profitability.
- Confirm the same profitability report opens.
- Hover or select the Profitability tool and confirm the help text describes category/job-type profit and data checks.

## Report Content

- Confirm the summary shows:
  - recorded sales count.
  - recorded sales revenue.
  - recorded sales profit.
  - recorded sales margin.
  - linked stock sales.
  - linked job sales.
  - estimated job profit.
  - potential unsold stock profit.
- Confirm `Recorded Profit by Product / Service Category` appears.
- Confirm `Recorded Profit by Job Type` appears.
- Confirm `Estimated Job Profit by Job Type` appears.
- Confirm `Profit Reporting Data Checks` appears.
- Confirm `Recent Sales Profit Detail` appears.

## Data Checks

- If there are unlinked sales, confirm the unlinked sales count is greater than zero.
- If any sale has sale amount but zero cost of goods, confirm the zero-cost sales check is greater than zero.
- If any job has a price but no cost, confirm the priced-jobs-with-no-cost check is greater than zero.

## Spot Checks

- Pick one known sale and confirm sale amount, cost and profit match the Sales record.
- Pick one known job-linked sale and confirm it appears under the expected job type.
- Pick one known jewellery stock sale and confirm it appears under the expected jewellery type.
- Confirm margin uses profit divided by sale amount, not markup.

## BI Command Report

- Open BI Command Report.
- Confirm the report still opens.
- Confirm it now includes a current-month product/service category profit table.

## Safety

- Confirm no records are added, edited or deleted by generating the report.
- Close and reopen OPALNOVA.
- Confirm existing sales, jobs and inventory records still load normally.
