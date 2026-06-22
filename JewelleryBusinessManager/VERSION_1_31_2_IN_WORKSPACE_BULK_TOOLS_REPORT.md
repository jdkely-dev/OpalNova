# Version 1.31.2 — In-Workspace Bulk Tools

## Purpose

This patch changes bulk cleanup actions so they behave like proper in-app workspace tools instead of relying only on pop-up dialogs.

## Changes

- Bulk Status Update now opens inside the Data Cleanup Studio preview/work area.
- The right-side panel now contains a selected-record summary, a status dropdown, and an Apply button.
- Bulk Add Selected To Market now opens inside the preview/work area with a market dropdown and selected-stock summary.
- Added an interactive tool panel host to the workspace preview area while preserving HTML/report preview support.
- Kept Data Quality, Duplicate Finder, and Missing Data reports opening as previews.

## Validation

- MainWindow.xaml parses as XML.
- App.xaml parses as XML.
- C# brace balance scan passed.
- ToolInputScrollViewer/ToolInputPanel are referenced and defined.
- No database schema changes were made.
