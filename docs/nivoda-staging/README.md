# OPALNOVA Nivoda GraphiQL Staging Page

This folder contains the non-secret static GraphiQL staging workbench that can be shared with Nivoda for API review. It is styled to match the OPALNOVA desktop workspace while keeping credentials and customer data out of the public page.

## Hosted URL

After this folder is committed and pushed to `main`, the included GitHub Pages workflow should run automatically when these staging-page files change. It can also be run manually from GitHub Actions:

`Deploy Nivoda staging handoff`

Expected GitHub Pages URL once deployed:

`https://jdkely-dev.github.io/OpalNova/`

The root `docs/index.html` forwards to the Nivoda page. If GitHub Pages is configured to publish directly from the `docs` folder and you want the deep link, use:

`https://jdkely-dev.github.io/OpalNova/nivoda-staging/`

Use whichever URL GitHub shows after deployment. Paste that final URL into OPALNOVA's `Review URL` field in the Nivoda Diamond Search window, save settings, then generate `Staging Handoff` so the report includes the same link.

## Credential Safety

Do not add Nivoda username, password, bearer tokens, account IDs, private supplier data, or customer data to this page. It is intentionally a public-safe integration overview.

## What To Send To Nivoda

- The hosted URL.
- The generated in-app `Nivoda Staging Handoff` report if credentials are available and schema introspection succeeds.
- A note that OPALNOVA is currently a local Windows desktop app and does not expose a public callback URL unless a separate hosted component is approved.
- If using the API bridge, the production API-calling domain will be `https://api.jackthejeweller.com.au`, not this GitHub Pages page and not public Wix browser code.
