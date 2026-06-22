# OPALNOVA V1.52 Job Completion Testing Checklist

## Before Testing

- Create an OPALNOVA backup first.
- Use a test customer, quote, job, material, and stone where possible.
- Note the starting material quantity before completion.
- Note the starting stone status before completion.

## Setup Test Data

- Create or open a test customer.
- Create a custom quote with one option.
- Link one material quantity to the quote option.
- Link one loose stone to the quote option.
- Accept the option so the material and stone links become reserved.
- Create a production job from the accepted quote.
- Confirm the job appears on the Production Board.

## Production Board Completion

- Open Production Board.
- Select the test job.
- Click `Complete Selected`.
- Confirm the completion checklist opens.
- Confirm the job title, customer, quote code, and accepted option are shown.
- Confirm the reserved material appears with quantity and current stock.
- Confirm the reserved stone appears with current status.
- Confirm material consumption and stone setting are checked by default.
- Click `Complete Job`.
- Confirm the app shows a completion summary.
- Confirm the job moves to Completed.

## Inventory Results

- Open Materials and confirm the material quantity decreased by the reserved amount.
- Open Material Transactions and confirm a negative movement was created.
- Confirm the transaction is linked to the completed job.
- Open Stones and confirm the reserved stone status changed to `SetInJewellery`.
- Open Custom Quotes or reports and confirm the accepted option links now show consumed/resolved reservation state.

## Payment & Collection Completion

- Create or choose another test job with accepted quote reservations.
- Open Payment & Collection.
- Select the job.
- Click `Mark Collected / Complete` or `Mark Shipped / Complete`.
- Confirm the same completion checklist opens.
- Complete the checklist.
- Confirm job completion, material movement, and stone status changes match the Production Board path.

## Outstanding Balance Check

- Test a job with a balance owing.
- Attempt completion.
- Confirm OPALNOVA warns about the outstanding balance.
- Confirm the checklist requires `Allow completion with outstanding balance`.
- Complete only if this is sample data.

## Negative Stock Check

- Test with a material quantity lower than the reserved quantity.
- Attempt completion.
- Confirm OPALNOVA warns that stock will go below zero.
- Confirm the checklist requires `Allow material stock to go below zero`.
- Cancel unless intentionally testing negative stock behavior.

## Release Without Consumption

- Open the completion checklist for a reserved job.
- Untick material consumption or stone setting.
- Keep `Release any unconsumed reservation lines` ticked.
- Complete the job.
- Confirm unconsumed reservation links are marked released, not consumed.
- Confirm material quantity does not change for unticked material consumption.

## Regression Checks

- Open Project Workbench and Alert Centre after completion.
- Confirm completed jobs no longer appear as active production alerts.
- Open Payment & Collection and filter Completed.
- Confirm completed jobs still display payment history.
- Run a debug build if testing from source.
