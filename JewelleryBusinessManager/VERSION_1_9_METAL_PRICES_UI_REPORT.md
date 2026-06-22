# Version 1.9 — Metal Prices and UI Polish

## Added
- Metal Prices window for manual precious-metal prices and optional live refresh.
- Gold, silver, platinum and palladium price storage in business settings.
- Currency selection, API key storage, last-updated timestamp and source note.
- Pricing Helper window to estimate metal cost, labour cost, total cost, recommended retail, profit and markup.
- Dashboard gold-per-gram card.
- New Pricing toolbar group with Metal Prices and Pricing Helper actions.
- Minor wording and navigation polish in the main dashboard/action area.

## Notes
- Live prices require the user's own API key and internet access.
- Manual metal prices can be entered without any internet connection.
- Stored prices are used locally by the Pricing Helper and do not alter existing inventory records automatically.

## Validation
Two static validation passes were run using Python-based validators. The environment still cannot run a real Windows WPF build because the .NET SDK is not installed here.
