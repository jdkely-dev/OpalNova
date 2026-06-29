# OPALNOVA V1.73.0 Hosted Editor Unsaved Change Guard Testing Checklist

Use this checklist after launching the V1.73.0 build.

- [ ] Confirm the main header shows `Version 1.73.0 - hosted editor unsaved-change guard`.
- [ ] Open About and confirm it shows `Version 1.73.0 - Hosted Editor Unsaved Change Guard`.
- [ ] Open an existing customer, job, stone, material, sale or task in a workspace editor tab.
- [ ] Change a field, then click the tab close button and confirm OPALNOVA prompts Save, Discard or Cancel.
- [ ] Choose Cancel and confirm the tab stays open with the edited value still visible.
- [ ] Click close again, choose Discard and confirm the tab closes without saving the edited value.
- [ ] Reopen the same record, change a field, click close, choose Save and confirm the record is saved and the tab closes.
- [ ] Open a record, make no changes, close the tab and confirm no unsaved-change prompt appears.
- [ ] Use the editor Save button on a changed record and confirm it still saves and closes normally.
- [ ] Use the editor Cancel button on a changed record and confirm the same unsaved-change prompt appears.
- [ ] Open Custom Quote Builder, make a quote change, and confirm the existing quote unsaved-change prompt still works.
- [ ] Confirm Payment & Collection, Proposal Pipeline, Project Workbench and Alert Centre tabs still close normally.
- [ ] Confirm no database migration prompt or schema reset occurs.
