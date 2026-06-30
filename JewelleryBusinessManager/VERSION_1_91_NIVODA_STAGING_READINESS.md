# OPALNOVA V1.91.0 - Nivoda Staging Readiness

V1.91.0 prepares OPALNOVA for a proper Nivoda API setup review without hardcoding or exposing credentials.

## Implemented

- Added Nivoda environment and optional external review URL fields to the Diamond Supplier API window.
- Added a `Staging Handoff` action in the Nivoda supplier window.
- Added `Nivoda Staging Handoff` actions in the Diamonds workspace and Diamond Supplier Studio.
- Added `NivodaDiamondApiService.CreateDiagnosticsAsync(...)` to authenticate with user-entered credentials and introspect the accessible GraphQL query/mutation schema.
- Added `NivodaStagingHandoffService.CreateHandoffReportAsync(...)`.
- Generated handoff reports include endpoint, GraphiQL URL, review URL, credential status, authentication/schema status, diamond-related fields and hold/order candidate mutations.
- Added a ready-to-host static handoff page at `docs/nivoda-staging/index.html`.
- Added `docs/index.html` as a Pages root entry point that redirects to the Nivoda staging handoff.
- Added a GitHub Pages deployment workflow at `.github/workflows/nivoda-staging-pages.yml` for the handoff page.
- Kept live hold/order API actions gated until Nivoda confirms account-specific mutation names and required payloads.
- Started no-schema customer segment guidance in existing customer relationship outputs.

## Notes

- Nivoda username/password stay local and user-entered only.
- Generated handoff reports deliberately omit credentials.
- OPALNOVA is still a local Windows desktop app, so any external staging link must be a hosted handoff page or private build/share link.
- The GitHub Pages URL is only available after these local changes are committed/pushed and the Pages workflow completes.
- This is not a whole-number milestone, so it should remain uncommitted unless explicitly requested.

## Validation

- Debug build succeeded with zero warnings and zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` launched and closed cleanly.
