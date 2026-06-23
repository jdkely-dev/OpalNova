# OPALNOVA V1.57 Invoice and Receipt Testing Checklist

## Job Invoice / Receipt

- Open `Documents Studio`.
- Click `Invoice / Receipt`.
- Select a job with a balance owing.
- Generate the document and confirm it shows `Customer Invoice`, total, payments recorded and balance due.
- Select a fully paid job if available.
- Generate the document and confirm it shows receipt-style paid status.
- Confirm payment history rows include date, amount, method, reference and notes.

## Sale Receipt

- Open `Invoice / Receipt`.
- Select a sale.
- Generate the receipt.
- Confirm sale amount, payment method, item/job context and customer details are clear.

## Deposit / Payment Receipts

- Open `Deposit Receipt`.
- Select a job with a deposit.
- Confirm deposit, total job amount and remaining balance are clear.
- Select a payment record.
- Confirm payment amount, method, reference and related job/sale context are clear.

## Regression Checks

- Open Payment & Collection.
- Generate invoice/receipt from the handover workflow.
- Record a test payment only if using test data.
- Confirm no payment or sale records are changed merely by generating documents.
- Run a debug build if testing from source.
