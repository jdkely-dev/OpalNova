# OPALNOVA V1.35.1 — Icon Path Fix

## Reason for patch
Visual Studio raised a `System.Windows.Markup.XamlParseException` on startup because `MainWindow.xaml` referenced `Icon="Assets/AppIcon.ico"` while the icon had been included as content without being copied to the build output folder.

That made WPF look for the icon at runtime beside the executable, for example:

`bin/Debug/net10.0-windows/win-x64/Assets/AppIcon.ico`

If that folder was missing, the app failed before the main window opened.

## Fix applied
- Changed the window icon reference to a WPF pack URI: `pack://application:,,,/Assets/AppIcon.ico`.
- Changed `Assets/AppIcon.ico` from `Content` to embedded WPF `Resource` in the project file.
- Kept `<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>` so the published `OPALNOVA.exe` still uses the custom icon.
- Updated visible version text to `Version 1.35.1 — OPALNOVA icon path fix + release prep`.
- Changed the small header logo letter from `J` to `O` for OPALNOVA branding.

## Intended result
The app should no longer crash on startup because of a missing `Assets/AppIcon.ico` runtime file.

## Notes
No database schema changes were made. No core business logic was changed.
