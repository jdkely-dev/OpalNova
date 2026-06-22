# Version 1.30 — Unified Search, Filters & Dropdown Contrast

## Purpose
This release responds to feedback that:
- dropdown menu highlights washed out text in the dark theme
- Search & Views Studio felt superfluous
- searching and filtering should be available directly from the top search bar

## Changes
- Removed Search & Views Studio from the left navigation.
- Added a top-bar quick filter dropdown.
- Added a Search All button beside the main search box.
- Added a Clear button to reset search and filters.
- Pressing Enter in the search box opens global search with the current search text and filter.
- Search still filters the current page as you type.
- Quick filters navigate to the relevant section automatically, such as Low Stock -> Materials or Overdue Tasks -> Tasks.
- Saved Views remain available inside the Global Search window rather than as a separate navigation studio.
- Improved ComboBoxItem styling so selected and highlighted dropdown items use high-contrast gold background with dark text.
- Preserved V1.29 performance improvements and all earlier features.

## Validation
- XAML/XML parsing completed.
- Project XML parsing completed.
- XAML event handlers checked.
- C# brace balance checked.
- Removed Search & Views Studio navigation entries.
- Confirmed new top search controls and handlers are present.
- Confirmed no interpolated raw strings were introduced.
- ZIP packaging completed.

## Suggested Tests
- Build and run.
- Open dropdown menus and confirm selected/highlighted items are easy to read.
- Type in the top search box and confirm the current page filters.
- Press Enter in the search box and confirm global search opens.
- Click Search All and confirm global search opens.
- Choose Low Stock, Jobs Due Soon, At Market, Ready To List, and Overdue Tasks from the top filter.
- Confirm the app navigates to the relevant section and applies the filter.
- Use Clear to reset search and filter.
- Check backup, export bundle, restore and restart persistence.
