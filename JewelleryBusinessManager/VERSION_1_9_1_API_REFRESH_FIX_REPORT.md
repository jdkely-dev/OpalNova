# Version 1.9.1 API Refresh Fix Report

## Issue
Live metal prices did not update reliably because V1.9 only used a GoldAPI.net-style query-string API key flow. Some users have GoldAPI.io keys, which use a different endpoint/authentication style.

## Fixes
- Added API Provider selector: GoldAPI.net or GoldAPI.io.
- GoldAPI.net uses `https://app.goldapi.net/price/METAL/CURRENCY?x-api-key=KEY`.
- GoldAPI.io uses `https://www.goldapi.io/api/METAL/CURRENCY` with `x-access-token`.
- Improved error messages to show provider-specific HTTP/API errors.
- Partial refresh support: if one metal fails but others succeed, successful metals are saved and failed metals keep prior/manual values.
- Manual metal pricing remains available for offline use.

## Validation
Two static validation passes were run on the final package.
