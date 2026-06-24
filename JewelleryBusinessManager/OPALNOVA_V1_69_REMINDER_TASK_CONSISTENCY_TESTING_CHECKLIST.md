# OPALNOVA V1.69.0 Reminder Task Consistency Testing Checklist

Use this checklist after launching the V1.69.0 build.

- [ ] Confirm the main header shows `Version 1.69.0 - reminder task consistency`.
- [ ] Open About and confirm it shows `Version 1.69.0 - Reminder Task Consistency`.
- [ ] Open Payment & Collection and select a job with a balance owing.
- [ ] Click Create Balance Follow-Up twice and confirm the second click prevents a duplicate open task.
- [ ] Click Create Pickup Reminder twice for the same job and confirm the second click prevents a duplicate open task.
- [ ] Open Project Workbench, select an active row and create a follow-up.
- [ ] Try creating the same Project Workbench follow-up again and confirm OPALNOVA prevents a duplicate open task.
- [ ] Open Proposal Pipeline, select a proposal and create a follow-up.
- [ ] Try creating the same Proposal Pipeline follow-up again and confirm OPALNOVA prevents a duplicate open task.
- [ ] Open Supplier Holds & Orders, select an external diamond and create a reminder task.
- [ ] Try creating the same supplier diamond reminder again and confirm OPALNOVA prevents a duplicate open task.
- [ ] Confirm newly created reminder/follow-up tasks appear in the dashboard/work queue where expected.
- [ ] Confirm existing quote, proposal, payment and supplier workflows still open normally.
