# OPALNOVA V1.74.0 - Payment Handover Checklist

V1.74.0 adds a live handover checklist to the Payment & Collection workflow.

## Implemented

- Added a `Handover Checklist` panel to `PaymentCollectionWindow`.
- Checklist items cover payment checked, item condition checked, customer notified or tracking shared, care instructions included, and handover document ready.
- Payment checked is automatically selected when the current job shows no balance owing.
- Checklist state is tied to the selected job and is preserved across same-job refreshes.
- Checklist summaries are included in the existing handover notes path for:
  - pickup / handover reminders.
  - handover confirmation documents.
  - ready for collection / ready to ship notes.
  - collected / shipped completion notes.
  - sale notes created from Payment & Collection.

## Preserved

- No database schema changes.
- Existing payment recording, invoice/receipt generation, handover confirmation generation, sale creation, reminder creation and job completion behavior are preserved.
- The generated handover confirmation document still uses the existing printable checklist and now also receives the live checklist summary through handover notes.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
