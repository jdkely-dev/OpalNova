# OPALNOVA V1.53 External Diamond Testing Checklist

## Before Testing

- Create an OPALNOVA backup first.
- Use a test saved external diamond where possible.
- Do not use real supplier orders unless intentionally testing live business data.

## Supplier Diamond Workflow

- Open Diamond Holds / Supplier Diamond Workflow.
- Confirm the grid includes an `Owned Stone` column.
- Select an external diamond.
- Confirm selected detail shows status, quote/customer context, and owned stone code if already converted.

## Receive And Convert

- Select a saved or ordered supplier diamond.
- Click `Mark Received`.
- Confirm the row status changes to `Received`.
- Click `Convert To Owned Stone`.
- Confirm OPALNOVA asks before creating owned inventory.
- Confirm the conversion success message shows a stone code.
- Confirm the row now shows `Converted To Owned Inventory`.
- Confirm the `Owned Stone` column shows the created stone code.

## Inventory Verification

- Open Stones.
- Search for the new stone code.
- Confirm stone type is lab-grown or natural diamond.
- Confirm carat, shape, colour, clarity, cut/pattern, and estimated value are sensible.
- Confirm status is `Loose`.
- Confirm notes include source system, supplier diamond ID, certificate, and external diamond marker.

## Duplicate Safety

- Return to Supplier Diamond Workflow.
- Select the same converted supplier diamond.
- Click `Convert To Owned Stone` again.
- Confirm OPALNOVA reports the existing owned stone rather than creating a duplicate.
- Open Stones and confirm only one matching stone was created for the supplier diamond.

## Quote Link Verification

- If the supplier diamond was linked to a quote option, open the quote.
- Confirm the linked external diamond remains visible.
- Confirm the workflow status no longer appears as an active hold/order task once converted.

## Regression Checks

- Open Alert Centre and confirm converted diamonds are not shown as active supplier-diamond alerts.
- Open Project Workbench and confirm no crash or duplicate supplier diamond action is created.
- Open Diamond Search and confirm saved records still open correctly.
- Run debug build if testing from source.
