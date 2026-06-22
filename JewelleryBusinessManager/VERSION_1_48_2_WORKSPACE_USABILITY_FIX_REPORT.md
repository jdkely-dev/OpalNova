# OPALNOVA V1.48.2 - Workspace Usability Fix

## Changes

- Updated visible and project package version metadata to 1.48.2.
- Removed the leftover hidden "Main Work" sidebar intro block from the main shell XAML.
- Updated the startup status text to match the streamlined toolbar, menu, and tabbed workspace layout.
- Fixed workspace tab close sequencing so closing the last Project Workbench tab does not immediately reopen it.
- Changed the workspace tab close button text to plain `x` and added a duplicate-close guard.
- Reused the safer tab-close refresh path for hosted Production Board, Custom Quotes, Diamond Search, Diamond Holds, and Payment & Collection tabs.
- Fixed Project Workbench hosted-tab initialization by loading rows from the root grid `Loaded` event as well as the standalone window `Loaded` event.
- Changed Project Workbench summary counters to reflect the currently visible filtered/search result rows.
- Kept database schema, quote workflow, inventory reservation, external diamond, production, payment, sales, and reporting logic unchanged.

## Testing Checklist

- [x] `dotnet build .\JewelleryBusinessManager.csproj` succeeds before the patch.
- [x] `dotnet build .\JewelleryBusinessManager.csproj --no-restore` succeeds after the patch.
- [x] `dotnet publish .\JewelleryBusinessManager.csproj -c Release -p:PublishProfile=win-x64-self-contained --no-restore` succeeds.
- [x] Published `OPALNOVA.exe` launches and closes cleanly.
- [x] UI Automation opens Project Hub, changes the filter to `Quotes`, and observes `Filter: Quotes` in the workbench status text.
- [x] UI Automation closes Project Workbench using the tab `x` button and observes `Closed tab: Project Workbench.`
- [x] UI Automation closes Project Workbench using the bottom Close button without a crash.
- [x] Screenshot check confirms the hosted Project Workbench fills the workspace area.
- [x] Screenshot check confirms Project Workbench ComboBox/TextBox backgrounds remain dark.
