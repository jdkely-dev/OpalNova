# OPALNOVA V1.45 — External Diamond Quote Linking

## Purpose
Connect saved external supplier diamonds, including Nivoda search results, into OPALNOVA's Custom Quote & Proposal workflow without treating supplier stock as owned inventory.

## Added
- New `QuoteOptionExternalDiamondLink` model.
- New upgrade-safe SQLite table: `QuoteOptionExternalDiamondLinks`.
- Custom quote option screen now includes an **External supplier diamonds** panel.
- Saved external diamonds can be linked to individual quote options.
- External diamond supplier cost flows into the quote option's stone cost.
- Existing quote markup and GST calculations then calculate the customer price.
- External diamond links can track status:
  - Proposed
  - Customer Interested
  - Hold Requested
  - Hold Confirmed
  - Ordered
  - Received
  - Declined
  - Expired
- Customer proposals now include linked external diamond details such as supplier ID, lab, certificate number, video link and certificate link where available.
- Accepting a quote option updates proposed linked external diamonds to **Customer Interested**.
- Production job creation now carries external diamond allocation details into the job notes.
- External diamonds remain separate from owned stone inventory.

## Data safety
- No existing tables are dropped or replaced.
- Existing owned inventory reservations remain unchanged.
- External diamonds are not deducted from owned stock quantities.
- The new table is additive and created only if missing.

## Recommended testing
Use the included `OPALNOVA_V1_45_EXTERNAL_DIAMOND_QUOTE_LINKING_TESTING_CHECKLIST.md`.
