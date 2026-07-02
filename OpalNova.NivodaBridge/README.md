# OPALNOVA Nivoda API Bridge

This project is the planned fixed production-domain layer for Nivoda.

If deployed at `https://api.jackthejeweller.com.au`, that deployed domain is the production URL/domain to give Nivoda because live Nivoda API calls will run from that server instead of from Wix browser code or the OPALNOVA desktop app.

## Intended Flow

```text
Wix website backend / Velo
  -> https://api.jackthejeweller.com.au/nivoda/search
  -> Nivoda GraphQL API

OPALNOVA desktop app, later
  -> https://api.jackthejeweller.com.au/nivoda/search
  -> Nivoda GraphQL API
```

## Why This Exists

- Keeps Nivoda credentials off the public Wix frontend.
- Gives Nivoda one stable production domain to review or allowlist.
- Lets Wix and the desktop app share the same Nivoda integration rules.
- Keeps hold/order mutations gated until Nivoda confirms account-specific schema and payloads.

## Required Environment Variables

Set these in the bridge host. Do not commit real values.

| Variable | Purpose |
| --- | --- |
| `NIVODA_ENDPOINT` | Nivoda GraphQL endpoint. Use staging until Nivoda confirms production. |
| `NIVODA_USERNAME` | Nivoda API username. |
| `NIVODA_PASSWORD` | Nivoda API password. |
| `OPALNOVA_ALLOWED_ORIGINS` | Comma-separated allowed web origins: `https://jackthejeweller.com.au,https://www.jackthejeweller.com.au`. |
| `OPALNOVA_BRIDGE_API_KEY` | Optional shared secret required in `X-OPALNOVA-BRIDGE-KEY`. Recommended for Wix backend and desktop calls. |

For the Azure App Service setup checklist, see `AZURE_APP_SERVICE_SETUP.md`.

## Endpoints

### `GET /health`

Returns whether the bridge is online and whether credentials are configured. It does not call Nivoda.

### `POST /nivoda/schema`

Authenticates to Nivoda and returns query/mutation field names. Requires `X-OPALNOVA-BRIDGE-KEY` when `OPALNOVA_BRIDGE_API_KEY` is configured.

### `POST /nivoda/search`

Searches Nivoda diamonds using a narrow request body:

```json
{
  "shape": "ROUND",
  "isLabGrown": true,
  "minCarat": 1.0,
  "maxCarat": 1.5,
  "labsCsv": "IGI,GIA",
  "limit": 20
}
```

Returns normalized diamond rows plus raw JSON for diagnostics.

## Wix Usage

Use Wix backend/Velo code to call this bridge, not public browser JavaScript with secrets. The Wix page should call your Wix backend, then the Wix backend calls:

```text
POST https://api.jackthejeweller.com.au/nivoda/search
X-OPALNOVA-BRIDGE-KEY: stored-in-wix-secrets
```

The customer-facing page should receive only safe diamond display data.

## What To Tell Nivoda

After deployment, tell Nivoda:

```text
Our production API-calling domain for Nivoda will be:
https://api.jackthejeweller.com.au

The customer website is:
https://jackthejeweller.com.au

Nivoda credentials are stored server-side only. Browser clients do not call Nivoda directly.
```

Use `https://www.jackthejeweller.com.au` as the website domain instead if Wix is configured to force the `www` host.
