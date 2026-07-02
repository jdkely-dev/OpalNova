# OPALNOVA V1.95.0 Workflow Control Consolidation Testing Checklist

Use this checklist against the published V1.95 build.

- Launch OPALNOVA and confirm the header shows `Version 1.95.0 - workflow control consolidation`.
- Open About and confirm it shows `Version 1.95.0 - Workflow Control Consolidation`.
- Open Search All and confirm section, filter and saved-view selectors show readable prompts if no value is selected.
- Open Custom Quote Builder and confirm quote, customer, stone, material, supplier diamond and supplier-diamond status selectors remain readable.
- Open Payment & Collection and confirm buttons/text fields keep the dark OPALNOVA look and the payment method selector has a meaningful prompt.
- Open Production Board and confirm toolbar buttons, search field and sort selector still fit and use the dark shared styling.
- Open Alert Centre, Project Workbench and Proposal Pipeline and confirm filter selectors and action buttons keep consistent focus/hover styling.
- Open Inventory Movement, Market Operations, Market Sale, Supplier Holds & Orders, Pricing Helper, Jeweller Tools, Device Capture and DYMO Mini Label.
- Confirm each ComboBox face uses a friendly prompt if no item is selected.
- Confirm selected values still display correctly after choosing an item.
- Confirm normal actions still work:
  - filtering lists,
  - selecting records,
  - recording payment details,
  - changing production sort,
  - choosing inventory movement context,
  - choosing device/label/pricing options.
- Confirm opening selector fields does not create or edit records.
- Confirm Debug build, Release publish and published launch smoke have passed before treating this as release-ready.
