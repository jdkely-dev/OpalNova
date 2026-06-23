# OPALNOVA V1.63.0 - Text Encoding and Copy Cleanup

V1.63.0 is a narrow release-readiness checkpoint focused on generated documents and visible metadata.

## What Changed

- Bumped visible/project version metadata to 1.63.0.
- Standardized generated report and support-document headings to use simple ASCII separators.
- Updated the database health check title, device setup notes, production batch report title, tax registration copy, inventory audit report title, opal yield report title, and stone workflow report title.
- Replaced the remaining old internal traceability report heading with OPALNOVA branding.
- Updated the built-in user guide metadata, release notes, About text, roadmap, forward plan, and test checklist.

## What Did Not Change

- No database schema changes.
- No workflow behavior changes.
- No stock, quote, payment, customer, production, or report calculations changed.
- Existing UI symbols and compact record-display separators were left alone where they are part of normal app presentation.

## Validation

- Debug build should complete with zero warnings and zero errors.
- Release publish should complete for `win-x64-self-contained`.
- Published `OPALNOVA.exe` should launch and close cleanly.
