# OPALNOVA V1.72.0 Job Payment History Testing Checklist

Use this checklist after launching the V1.72.0 build.

- [ ] Confirm the main header shows `Version 1.72.0 - job payment history`.
- [ ] Open About and confirm it shows `Version 1.72.0 - Job Payment History`.
- [ ] Open Jobs and edit a saved job that has one or more recorded payments.
- [ ] Confirm the lower job editor area shows `Job Payment History`.
- [ ] Confirm Total, Paid, Balance and Payments match Payment & Collection for the same job.
- [ ] Confirm the ledger rows show date, amount, method, reference and notes.
- [ ] Confirm the linked customer and sale-created state are readable.
- [ ] Edit and save a normal job field, then reopen the job and confirm the payment history still displays.
- [ ] Open a saved job with no recorded payment rows and confirm the empty payment state is clear.
- [ ] Open Payment & Collection, record a test payment on a job, then reopen that job editor and confirm the new payment appears.
- [ ] Confirm invoices, receipts, payment summary, handover confirmation and job completion actions still work as before.
- [ ] Confirm no database migration prompt or schema reset occurs.
