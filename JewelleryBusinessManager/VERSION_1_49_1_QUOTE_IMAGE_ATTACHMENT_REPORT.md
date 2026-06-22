# OPALNOVA V1.49.1 Quote Image Attachment Polish

## Scope

V1.49.1 continues the V1.49 quote workspace polish with a focused design-image attachment workflow for quote options.

## Changes

- Bumped project and visible app version metadata to 1.49.1.
- Added selected-option image preview in the quote workspace.
- Added attach, open, and remove image actions for quote options.
- Reused existing `QuoteOption.ImagePath`; no schema changes were required.
- Copied attached images into OPALNOVA's app photo folder through `PhotoStorageService`.
- Expanded the option comparison grid with deposit, linked-stock, and image columns.
- Included attached option images in proposal HTML output using embedded data URIs.

## Data Safety

- No database schema changes.
- Existing quote option save/load behavior is preserved.
- Removing an image clears the option link only; it does not delete files from the app photo folder.

## Validation Checklist

- Build succeeds with zero warnings and zero errors.
- Release publish succeeds.
- Published OPALNOVA executable launches.
- Manual quote smoke:
  - attach an image to an option.
  - remove an image from an option.
  - save and reload a quote with an image.
  - preview proposal and confirm the image appears.
