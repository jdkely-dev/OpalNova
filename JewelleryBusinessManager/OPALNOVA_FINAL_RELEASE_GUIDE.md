# OPALNOVA V1.37 Final Release Prep Guide

## What this package is

This is the release-prep package for OPALNOVA, a standalone Windows jewellery business management application.

It is intended to be built/published into a self-contained Windows x64 app named:

```text
OPALNOVA.exe
```

## Recommended final release workflow

1. Open the project in Visual Studio.
2. Select `Release` configuration.
3. Build the project.
4. Run the app from Visual Studio once and check it opens cleanly.
5. Run `publish_release_win_x64.bat` or `publish_release_win_x64.ps1`.
6. Open the published `OPALNOVA.exe` from:

```text
bin\Release\net10.0-windows\win-x64\publish\
```

7. Confirm records save after closing and reopening the published app.
8. Copy the full published folder to your chosen release/storage location.

## Final app checks before relying on it

- Add a customer.
- Add an inventory item.
- Add a job, sale, or task.
- Use several Tool Studio buttons.
- Hover over a faded `?` help badge.
- Click a faded `?` help badge to pin the mini guide.
- Click the same badge again to close it.
- Close and reopen the app.
- Confirm the test records are still there.
- Create a backup from the Safety & Data tools.

## Important data note

The internal project name and some storage paths are intentionally still based on the original project structure. This was left unchanged to avoid hiding existing data or breaking the working app during release prep.

Do not rename folders inside the source project unless you are ready to do a full namespace/storage migration.
