# OPALNOVA V1.74.0 Payment Handover Checklist Testing Checklist

Use this checklist after launching the V1.74.0 build.

- [ ] Confirm the main header shows `Version 1.74.0 - payment handover checklist`.
- [ ] Open About and confirm it shows `Version 1.74.0 - Payment Handover Checklist`.
- [ ] Open Payment & Collection.
- [ ] Select a job with no balance owing and confirm `Payment checked` is automatically ticked.
- [ ] Select a job with a balance owing and confirm `Payment checked` is not automatically ticked.
- [ ] Tick and untick handover checklist items and confirm the summary text updates.
- [ ] Enter collection / shipping notes and generate a handover confirmation.
- [ ] Confirm the generated handover confirmation includes the notes and handover checklist summary.
- [ ] Create a pickup / handover reminder and confirm its follow-up notes include the checklist summary.
- [ ] Mark a job ready for collection or ready to ship and confirm the job internal notes include the checklist summary.
- [ ] Create a sale from Payment & Collection and confirm the sale notes include the checklist summary.
- [ ] Mark a job collected or shipped through the completion checklist and confirm the completion notes include the checklist summary.
- [ ] Confirm Payment History, Record Payment, invoices/receipts and thank-you follow-up still work as before.
- [ ] Confirm no database migration prompt or schema reset occurs.
