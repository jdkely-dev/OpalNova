# OPALNOVA V1.46 — Supplier Diamond Hold & Order Workflow

## Purpose

V1.46 adds a practical workflow for external supplier diamonds after they have been saved from Nivoda and linked to a quote option.

The goal is to prevent customer-approved supplier stones from being forgotten before they are held, ordered or received.

## Added

- New **Supplier Holds & Orders** tool.
- Available from:
  - Custom Workflow Studio
  - Diamond Supplier Studio
  - Dashboard Command Centre
- External diamond workflow statuses:
  - Customer Interested
  - Hold Requested
  - Hold Confirmed
  - Hold Expiring
  - Order Requested
  - Ordered
  - Received
  - Declined
  - Released
  - Expired
- New external diamond tracking fields:
  - supplier reference
  - hold requested date
  - hold confirmed date
  - hold expiry date
  - order requested date
  - ordered date
  - expected arrival date
  - received date
  - released date
- Reminder task creation from a selected external diamond.
- Status changes sync to linked quote-option external diamond links.
- Dashboard tiles for:
  - diamond holds expiring
  - diamonds ordered but not received
  - accepted external diamonds not ordered
  - quote options using external diamonds
- Existing saved external diamond records remain separate from owned stone inventory.

## Database safety

This build adds columns to the existing `ExternalDiamonds` table using upgrade-safe column checks. Existing records are preserved.

No owned stone, material, quote, production, payment or sale logic was changed.

## Testing recommended

Use the included `OPALNOVA_V1_46_SUPPLIER_DIAMOND_WORKFLOW_TESTING_CHECKLIST.md`.
