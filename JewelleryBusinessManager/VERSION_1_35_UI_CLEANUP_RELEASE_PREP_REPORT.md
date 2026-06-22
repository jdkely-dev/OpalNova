# Version 1.35 — UI Cleanup + Standalone Release Prep

## Purpose
Prepare the working V1.34 build for a cleaner release-candidate experience and make it easier to publish as a standalone Windows application.

## UI cleanup
- Updated main window title to release-candidate wording.
- Updated visible version label to V1.35.
- Changed the sidebar heading from a development-style menu to a clearer Workspace label.
- Renamed the navigation group from Studios to Tools while keeping underlying section names unchanged for compatibility.
- Reworded dashboard and hint text to feel more like an everyday business application than a development prototype.
- Slightly widened the sidebar and search box.
- Adjusted row/header heights and navigation padding for readability.

## Standalone application preparation
- Added app version metadata to the project file.
- Added company/product/description metadata.
- Added a custom application icon at `Assets/AppIcon.ico`.
- Added self-contained win-x64 publish settings.
- Added `Properties/PublishProfiles/win-x64-self-contained.pubxml`.
- Added `publish_release_win_x64.bat`.
- Added `publish_release_win_x64.ps1`.
- Added `README_RELEASE_PREP.md` with release test and publish steps.

## Compatibility notes
- No model/database schema changes were made.
- No existing record names or tool section keys were changed in C# logic.
- Existing V1.34 Customer Relationship Studio code remains intact.

## Final manual check needed
A full Visual Studio / dotnet build should be run on Windows with the matching .NET Desktop SDK installed.
