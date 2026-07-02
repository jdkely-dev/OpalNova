# OPALNOVA Nivoda Wix Bridge Setup

This is the simple secure setup for using Nivoda from Wix.

## The Shape

```text
Wix page
  -> Wix backend function
  -> OPALNOVA API bridge
  -> Nivoda
```

The public Wix page must not store or send Nivoda credentials. It should only call your Wix backend function.

## What You Already Added

You added the Wix backend function. That is the secure place for Wix to call the OPALNOVA bridge because it can read Wix Secrets.

## Wix Secrets

In Wix Secrets Manager, keep:

- `OPALNOVA_NIVODA_BRIDGE_URL`
  - `https://api.jackthejeweller.com.au`
- `OPALNOVA_BRIDGE_API_KEY`
  - The long random bridge password/key you created.

Do not put either value in public page code.

## Bridge Host Secrets

Wherever the bridge is deployed, keep:

- `NIVODA_ENDPOINT`
- `NIVODA_USERNAME`
- `NIVODA_PASSWORD`
- `OPALNOVA_ALLOWED_ORIGINS`
  - `https://jackthejeweller.com.au,https://www.jackthejeweller.com.au`
- `OPALNOVA_BRIDGE_API_KEY`
  - Must match the Wix secret.

## What To Build Later On Wix

You only need a visible Wix page when you want customers to search diamonds.

A simple test page can have:

- shape dropdown
- min carat input
- max carat input
- search button
- repeater/table for results

The page calls the Wix backend function. The backend function calls the bridge.

## What To Give Nivoda

After the bridge is deployed:

```text
Production API-calling domain:
https://api.jackthejeweller.com.au

Customer website:
https://jackthejeweller.com.au

The Wix frontend does not call Nivoda directly. Calls go through a server-side OPALNOVA API bridge.
```

Use `https://www.jackthejeweller.com.au` as the customer website instead if Wix forces the `www` host.
