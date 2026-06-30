# OPALNOVA V1.77.0 - Recent Work Recall

V1.77.0 continues workspace navigation polish without changing the database schema.

## Added

- Added a dashboard `Recent Work` panel for the current app session.
- Tracks recently opened:
  - hosted workflow tabs
  - generated report previews
  - saved record editor tabs
  - exact quote workspace tabs where a quote id is available
- Recent entries deduplicate automatically and move the latest open to the top.
- Recent entries can be cleared from the dashboard.
- Reopening saved record editors keeps the existing workspace tab unsaved-change prompt behavior.

## Cleaned

- Replaced visible top-toolbar symbol labels and touched preview separators with plain ASCII text.

## Preserved

- No schema changes.
- Existing tab close guards, report preview behavior, record editor saving, quote workflow and dashboard totals remain unchanged.

