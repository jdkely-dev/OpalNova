# OPALNOVA V1.56 Customer Timeline Testing Checklist

## Customer Timeline

- Open `Customer Relationship Studio`.
- Click `Customer Timeline`.
- Select a customer with quote, job, payment, sale or task history.
- Generate the timeline.
- Confirm the report opens in Preview / Result.
- Confirm timeline rows are ordered newest first.
- Confirm quotes, proposal-sent events, jobs, sales, payments and tasks appear where linked to the customer.

## Customer Summary Card

- Open `Customer Summary Card`.
- Select the same customer.
- Confirm the summary includes quote count and open quote count.
- Confirm recent quotes appear.
- Confirm recent timeline events appear near the bottom of the summary.
- Confirm preferences show ring sizes, preferred metals and preferred stones where entered.

## Follow-Up Workflow

- Open `Create Customer Follow-Up`.
- Select the customer.
- Create a test follow-up, then cancel or delete it if it is not needed.
- Confirm the timeline/summary generation itself did not create tasks automatically.

## Regression Checks

- Open `Customer History` and confirm the older report still works.
- Open `Relationship Report` and confirm it still works.
- Open Dashboard and Alert Centre.
- Run a debug build if testing from source.
