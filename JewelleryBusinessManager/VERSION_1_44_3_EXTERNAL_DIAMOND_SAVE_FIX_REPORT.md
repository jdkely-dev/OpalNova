# OPALNOVA V1.44.3 — External Diamond Save Fix

## Purpose
Fixes the first Nivoda integration save path after search results began returning from the API.

## Fixes
- Improved result selection handling in the Nivoda Diamond Supplier window.
- Automatically selects the first returned diamond after a successful search.
- Save now falls back to the current grid item or first result if WPF selection has not visibly locked onto a row.
- Replaced the save operation with a direct SQLite insert/update for external supplier diamonds.
- Ensures the ExternalDiamonds table and indexes exist immediately before save.
- Performs duplicate detection using SupplierDiamondId or CertificateNumber.
- Shows a success message after the selected external diamond is saved.
- Keeps external diamonds separate from owned stone inventory.

## Stability
- No quote workflow changes.
- No production board changes.
- No payment/collection changes.
- No physical inventory deduction changes.
- Nivoda search/query logic from V1.44.2 is preserved.

## Test
1. Open Diamond Supplier Studio.
2. Enter user-supplied Nivoda credentials or use saved credentials.
3. Run a diamond search.
4. Click a result row, or leave the first row selected.
5. Click Save Selected External Diamond.
6. Open Saved External Diamond Records and confirm the record appears.
