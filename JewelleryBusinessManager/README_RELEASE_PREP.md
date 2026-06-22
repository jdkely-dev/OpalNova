# OPALNOVA — V1.37 Final Release Prep

This package is prepared for standalone Windows publishing as `OPALNOVA.exe`.

## Publish

Open the project folder in Visual Studio, or run one of these from the project folder:

```bat
publish_release_win_x64.bat
```

```powershell
.\publish_release_win_x64.ps1
```

The output folder will be:

```text
bin\Release\net10.0-windows\win-x64\publish\
```

## Final checks

Read these before using the app as your live business system:

- `OPALNOVA_FINAL_RELEASE_GUIDE.md`
- `OPALNOVA_FINAL_TESTING_CHECKLIST.md`
- `OPALNOVA_BACKUP_RESTORE_GUIDE.md`
- `OPALNOVA_RELEASE_NOTES_V1_37.md`

## Current help behaviour

Tool Studio action buttons include a small faded circular `?` badge. Hover over the badge to preview a mini guide. Move away to close the preview. Click the badge to pin the guide open, then click the same badge again to close it.

## Stability note

This final release-prep package does not change the database schema or save/load logic from the working V1.36.2 build.

---


## OPALNOVA V1.36.2 Toggle Hover Help

This update changes the faded `?` help badges so they behave more naturally:

- Hover over a faded `?` badge to preview the mini guide.
- Move the mouse away from the badge and the preview closes automatically.
- Click the badge to pin/keep the mini guide open.
- Click the same badge again to close the pinned mini guide.
- The main function button still runs normally when clicked away from the badge.

# OPALNOVA — Release Prep

This build is focused on UI cleanup and preparing the project for a standalone Windows application.

## Recommended release checks

1. Open `JewelleryBusinessManager.csproj` in Visual Studio.
2. Build in `Release` mode.
3. Run the app from Visual Studio and test:
   - Dashboard loads cleanly.
   - Search, Search All, Clear and Refresh work.
   - Add, Edit and Delete work on sample records.
   - Backup works before and after test edits.
   - Customer Relationship Studio still opens.
   - Reports and document previews still open.
4. Create a clean backup of real business data before distributing the app.

## Standalone publish command

From the project folder, run either:

```bat
publish_release_win_x64.bat
```

or:

```powershell
.\publish_release_win_x64.ps1
```

The standalone output folder will be:

```text
bin\Release\net10.0-windows\win-x64\publish\
```

## Notes

- The project is configured for a self-contained `win-x64` publish.
- The app icon is included at `Assets/AppIcon.ico`.
- This does not create an installer yet. Once the published folder is confirmed working, the next step is an installer package such as MSIX or Inno Setup.


## OPALNOVA V1.36.1 Hover Help Badges

This release candidate includes small `?` help buttons:

- Top toolbar `?` opens help for the current page.
- Record workspace `?` explains the Add/Edit/Delete record workflow.
- Tool workspace `?` explains the current studio.
- Each Tool Studio action card has its own `?` mini-guide.

The help window is intentionally small and floating so it can sit beside the main app while you work.


## OPALNOVA V1.36.1 Hover Help Badges
Tool Studio action buttons now include a small faded circular ? badge. Hover the mouse over the badge to open a compact floating mini-guide for that function. Click the main button area to run the function as normal.
