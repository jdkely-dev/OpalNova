# OPALNOVA V1.47 — Streamlined Interface and Simplified Navigation

## Purpose
This build focuses on usability rather than new business logic. OPALNOVA already contains many connected workflows, so V1.47 reduces the number of visible navigation sections and moves lesser-used tools/records into a top menu bar.

## Main changes

### Simplified left navigation
The left sidebar now shows only the main daily work areas:

- Dashboard
- Customers
- Quotes & Proposals
- Production
- Payments & Sales
- Inventory
- Diamonds
- Reports
- Settings & Backup

### New top menu bar
A new menu bar gives access to less frequently used areas without crowding the main window:

- Workflow
- Records
- Specialist Tools
- Reports & Data

### New workflow home pages
Added consolidated home sections for:

- Quotes & Proposals
- Production
- Payments & Sales
- Inventory
- Diamonds
- Reports
- Settings & Backup

These group existing OPALNOVA functions into practical business areas while preserving the existing specialist studios underneath.

### Kept stable
No database schema changes were made. The following core workflows were not changed:

- Custom quote workflow
- Nivoda diamond search
- External diamond quote linking
- Supplier diamond hold/order workflow
- Inventory reservation logic
- Production board logic
- Payment and collection workflow
- Reports and CSV export logic

## Validation performed

- MainWindow XAML parses correctly
- App XAML parses correctly
- Click/event handler references checked
- C# brace balance checked
- Project XML version updated
- ZIP integrity checked after packaging

## Notes
This build is designed to make OPALNOVA feel closer to a streamlined business system: daily actions stay visible, while raw records and specialist tools remain accessible through the menu bar.
