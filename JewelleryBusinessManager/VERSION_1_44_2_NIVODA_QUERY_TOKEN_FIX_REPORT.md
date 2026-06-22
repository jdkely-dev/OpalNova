# OPALNOVA V1.44.2 — Nivoda Query + Token Auth Fix

## Reason for patch
The first live staging test returned:

- `Unknown type "DiamondFilterInput"`
- `Unknown field "diamonds" on type "Query"`

This showed the integration was using a generic GraphQL example rather than Nivoda's real diamond search shape.

## Fixes applied

- Replaced the old `diamonds(filter: ...)` query with Nivoda's documented `diamonds_by_query(...)` query.
- Replaced `DiamondFilterInput` with Nivoda's documented inline `query` object containing:
  - `labgrown`
  - `shapes`
  - `sizes`
- Added proper two-step authentication:
  1. call `authenticate { username_and_password { token } }`
  2. call diamond search with `Authorization: Bearer <token>`
- Removed Basic Authentication from API search calls.
- Changed maximum API search limit to 50 to match Nivoda's documented API limit.
- Mapped Nivoda's nested result structure:
  - `diamonds_by_query.items[].diamond.certificate.carats`
  - `diamonds_by_query.items[].diamond.certificate.certNumber`
  - `diamonds_by_query.items[].price`
- Lab selection is now applied locally after results are returned, avoiding uncertainty around Nivoda's exact lab-filter field name.

## Testing notes
Enter user-supplied Nivoda credentials, then test:

1. Test Connection
2. Search ROUND, 1.0–1.5 ct, lab-grown, IGI/GIA
3. Save one returned result
4. Open External Diamonds and confirm it appears

## Stability
No database schema changes were made.
