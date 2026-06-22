# OPALNOVA V1.44.1 — Nivoda Staging Test Helper

## Purpose

This historical patch originally added a staging credential helper. That helper has since been removed in V1.48.3 so credentials are user-entered only.

## Added

- Historical note: a staging helper was added here for development testing.
- Current state: V1.48.3 replaced this with a **Use Default Endpoints** button that resets API URLs only and never fills credentials.
- Main visible version text updated to V1.44.1.
- Project version updated to 1.44.1.

## Security note

V1.48.3 removes credential filling from source code. Users must enter Nivoda credentials through the app settings/search flow.

## Kept stable

- No database schema changes.
- No quote workflow changes.
- No inventory workflow changes.
- No production board changes.
- No payment workflow changes.
