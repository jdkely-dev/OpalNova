# Version 1.12 — Opal Parcel Yield & Stone Workflow

## Added

- Parcel Yield button for selected opal parcels or stones linked to a parcel.
- Stone Workflow window to move stones through rough, cutting, polished, selected for design, assigned to jewellery, set, reserved and sold stages.
- Opal Report button for printable opal parcel yield and stone workflow reports.
- Dashboard cards for total opal yield percentage and estimated parcel profit.
- Extra StoneStatus stages while preserving previous stored enum values for existing databases.

## Compatibility

No new database tables were added. Existing opal parcel and stone fields are used for yield calculations and workflow notes.

## Validation focus

- Preserved V1.11.3 file-lock backup/export/restore fixes.
- Checked new button handlers and XAML wiring.
- Checked stone enum numeric compatibility for existing database values.
- Checked no raw interpolated CSS/HTML string issue was reintroduced.
