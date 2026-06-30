# OPALNOVA V1.91.0 Nivoda Staging Readiness Testing Checklist

Use this checklist against the published V1.91 build before sending Nivoda setup details.

- Launch OPALNOVA and confirm the header shows `Version 1.91.0 - Nivoda staging readiness`.
- Open About and confirm it shows `Version 1.91.0 - Nivoda Staging Readiness`.
- Open `Diamonds`, then click `Nivoda Staging Handoff`.
- Confirm a handoff report opens without entering credentials and clearly says schema was not checked.
- Open `Diamond Supplier Studio`, then click `Nivoda Staging Handoff`.
- Open `Nivoda Diamond Search`.
- Confirm the API settings section has `Environment`, `Review URL`, `Endpoint`, `Username`, `Password`, and `GraphiQL`.
- Click `Use Default Endpoints` and confirm it resets URLs only, not username/password.
- Enter only a non-secret review URL if you have one, save settings, and regenerate the handoff report.
- If Nivoda staging credentials are available, enter them, click `Test Connection`, then generate `Staging Handoff`.
- Confirm the handoff report lists authentication/schema status and discovered query/mutation fields.
- Confirm no username or password appears in the generated handoff report.
- Open `docs/nivoda-staging/index.html` locally and confirm it is a non-secret static page ready for external hosting.
- Confirm `.github/workflows/nivoda-staging-pages.yml` exists for GitHub Pages deployment after push.
- Confirm `docs/index.html` exists and links/redirects to `docs/nivoda-staging/`.
- After pushing to `main`, confirm the `Deploy Nivoda staging handoff` workflow runs automatically, or run it manually from GitHub Actions if needed.
- Paste the final GitHub Pages URL into the Nivoda `Review URL` field, save settings, and regenerate `Staging Handoff`.
- Confirm search still works with valid Nivoda credentials and saved external diamonds remain separate from owned inventory.
- Confirm Supplier Holds & Orders still uses local hold/order tracking until API mutations are confirmed.
