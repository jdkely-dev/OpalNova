# Version 1.7 — Dark Interface + Intuitive Navigation

## Built from
Confirmed working Version 1.6 Branding, Settings + Business Defaults package.

## Main goals
- Make the application feel cleaner and more professional.
- Add a dark-mode style suited to long workshop/admin sessions.
- Improve navigation clarity without changing the working database or core workflows.
- Preserve all confirmed working features from V1.0 to V1.6.

## User interface changes
- Added a dark navy/charcoal theme with gold accent colour.
- Added global WPF styles for windows, buttons, text boxes, combo boxes, list boxes, data grids and headings.
- Rebuilt the main window layout with:
  - cleaner header area
  - search and settings in the top right
  - dedicated left navigation panel
  - grouped action toolbar
  - clearer action categories: Records, Workflow, Printouts and Reports
  - card-style dashboard tiles
  - bottom status bar
- Improved Add/Edit record window layout with dark themed header/footer and grouped fields.
- Improved Settings window layout with dark themed header/footer and highlighted settings sections.

## Behaviour changes
No intentional database or workflow behaviour changes were made in this version.

## Features preserved
- Add/edit/delete records
- Search
- Dropdowns
- Photos
- Backups
- CSV exports
- Job workflow
- Create sale
- Add to market
- Printouts
- Reports
- Business branding/settings

## Testing recommendation
After building in Visual Studio, test:
1. App opens and uses dark theme.
2. Left navigation switches sections correctly.
3. Dashboard tiles load correctly.
4. Add/edit forms open and save correctly.
5. Settings window opens and saves correctly.
6. Dropdowns and photo upload still work.
7. Job workflow, sale creation and market workflow still work.
8. Printout/report buttons still work.
9. Backup and CSV export still work.
