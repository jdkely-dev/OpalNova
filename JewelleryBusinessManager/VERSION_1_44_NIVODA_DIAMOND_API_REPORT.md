# OPALNOVA V1.44 — Nivoda Diamond Supplier API Integration

## Purpose
Adds the first external diamond supplier integration using the Nivoda GraphQL endpoint configured in OPALNOVA:

- Endpoint: `https://intg-customer-staging.nivodaapi.net/api/diamonds`
- GraphiQL explorer: `https://intg-customer-staging.nivodaapi.net/api/diamonds-graphiql`
- Authentication: user-entered username and password through the app's Nivoda settings/search flow

## Added

- New **Diamond Supplier Studio** tool section.
- New **Nivoda Diamond Search** window.
- API settings panel for endpoint, GraphiQL URL, username and password.
- Test Connection button using a minimal GraphQL `__typename` query.
- Search filters for shape, lab-grown/natural, carat range and labs.
- Result grid for supplier diamond ID, shape, carat, colour, clarity, cut, lab, certificate and pricing.
- Save selected supplier result as an **External Diamond** record.
- Saved external diamond register.
- Open video link and copy certificate number tools.
- Default external diamond markup and currency settings.
- Contextual mini-guide entries for the new diamond supplier workflow.

## Data safety

External supplier diamonds are stored separately from owned OPALNOVA stone inventory. Saving a search result does **not** reserve, buy, receive, or deduct any stock.

## New table

`ExternalDiamonds`

This table tracks Nivoda/supplier results with status values such as `Search Result` and `Saved`. Later patches can add hold, ordered and received workflows.

## Notes

The first search query uses the supplied sample structure and robust result mapping. If Nivoda's current GraphQL schema differs, OPALNOVA will display the exact GraphQL error so the query field/filter names can be adjusted quickly from the GraphiQL explorer.

## Suggested next patch

V1.44.1 / V1.45 should link selected external diamonds to custom quote options, then add hold/request/order/received statuses.
