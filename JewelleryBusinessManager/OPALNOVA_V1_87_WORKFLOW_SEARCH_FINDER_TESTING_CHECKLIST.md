# OPALNOVA V1.87.0 Workflow Search Finder Testing Checklist

Use this checklist against the published V1.87 build.

## Startup

- Launch `OPALNOVA.exe`.
- Confirm the header shows `Version 1.87.0 - workflow search finder`.
- Open About and confirm it shows `Version 1.87.0 - Workflow Search Finder`.

## Search All Basics

- Click `Search All`.
- Confirm the window subtitle mentions records and workflow actions.
- Confirm the Section dropdown includes `Custom Quotes`, `Quote Options`, `External Diamonds` and `Workflow Actions`.
- Confirm existing saved views still load.

## Record Search Coverage

- Search for a known custom quote code or title.
- Confirm matching custom quote rows appear and open back to the Custom Quotes section.
- Search for a known quote option name.
- Confirm matching quote option rows appear and open back to the Quote Options section.
- Search for a known external diamond certificate, shape or supplier id.
- Confirm matching external diamond rows appear and open back to the External Diamonds section.

## Workflow Action Search

- Set Section to `Workflow Actions`.
- Search `backup` and open the matching workflow result.
- Confirm OPALNOVA navigates to Settings & Backup.
- Search `diamond` and open the supplier diamond result.
- Confirm OPALNOVA navigates to Diamond Supplier Studio.
- Search `market` and open the market result.
- Confirm OPALNOVA navigates to Market Studio.

## New Filters

- Choose `Custom Quotes` and `Proposal Follow-Up Due`; confirm the search runs without error.
- Choose `External Diamonds` and `Supplier Holds Expiring`; confirm the search runs without error.
- Confirm older filters such as `Low Stock`, `Open Jobs` and `Overdue Tasks` still behave as before.

## Regression Checks

- Save a normal record search as a saved view.
- Apply the saved view.
- Delete the saved view if it was only for testing.
- Close Search All and confirm the main workspace remains responsive.
- Close the app cleanly.

