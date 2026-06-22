# OPALNOVA V1.54 Excel Export Testing Checklist

## Before Testing

- Create an OPALNOVA backup first.
- Use a copy of real data or low-risk test data where possible.
- Close any previously opened OPALNOVA-generated Excel files before exporting again.

## Reports Workspace

- Open `Reports`.
- Confirm `Export BI Excel` appears near `Export BI CSV`.
- Click `Export BI Excel`.
- Confirm the Preview / Result page opens a business intelligence Excel export launcher.
- Click the workbook link if your Windows file associations allow it.

## Reports Studio

- Open `Reports Studio`.
- Confirm `Export BI Excel` appears beside the CSV export action.
- Run the export from Reports Studio.
- Confirm a new export folder is created in the OPALNOVA printout/report location.

## Workbook Contents

- Open `OPALNOVA_Business_Intelligence.xls` in Excel or a compatible spreadsheet app.
- Confirm these sheets exist: Summary, Sales, Outstanding Balances, Quotes, Inventory Value, Reserved Inventory, Tasks, External Diamonds.
- Confirm currency/number columns appear as spreadsheet numbers where practical.
- Confirm dates are readable.
- Confirm blank optional fields do not show raw null text.

## Regression Checks

- Run `BI Command Report` and confirm the HTML report still opens.
- Run `Export BI CSV` and confirm the CSV bundle is still generated.
- Open Dashboard and confirm report/export changes did not affect normal dashboard counts.
- Open Alert Centre and confirm alerts still load.
- Run a debug build if testing from source.
