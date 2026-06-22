# OPALNOVA V1.38 — Custom Quote Workflow

## Purpose
This is the first staged upgrade after reviewing a comparable jewellery workflow system. It connects customer quoting, design options, proposal output, acceptance and production-job creation.

## Added
- Custom Workflow Studio in the main Tools navigation.
- Multi-option Custom Quote Builder.
- Customer-linked quote records with unique quote codes and validity dates.
- Live labour, metal, stone, setting, findings, other-cost, markup and GST calculations.
- Recommended-option flag and deposit calculation.
- Professional customer-facing HTML proposal output.
- Accepted option tracking.
- One-button creation or update of a linked production job while preserving quoted price and design details.
- Upgrade-safe SQLite tables for CustomQuotes and QuoteOptions.
- Contextual hover/click mini-guides for the new workflow actions.

## Existing data safety
The upgrade uses CREATE TABLE IF NOT EXISTS and adds no destructive changes to existing tables. Existing OPALNOVA records and application data locations remain unchanged.

## Suggested test
1. Open Tools > Custom Workflow Studio > Custom Quote Builder.
2. Select a customer and enter a title.
3. Add two design options and different costs.
4. Save and preview the proposal.
5. Accept one option.
6. Create the production job and confirm it appears under Jobs.
7. Close and reopen OPALNOVA and confirm the quote remains available.

## Next staged upgrades
- Stone/material selection and stock reservation.
- Configurable payment schedules and deposits.
- Proposal images and richer templates.
- Visual production board.
- Customer approval history and communications.
