# OPALNOVA V1.94.0 Selector Theme Polish Testing Checklist

Use this checklist against the published V1.94 build.

- Launch OPALNOVA and confirm the header shows `Version 1.94.0 - selector theme polish`.
- Open About and confirm it shows `Version 1.94.0 - Selector Theme Polish`.
- Open `Quotes & Proposals`.
- Open the Custom Quote Builder and confirm empty selectors show a muted friendly prompt instead of a blank white or raw object-looking field.
- Open the quote `Valid until` and `Required by` date pickers.
- Confirm the date entry surface and popup calendar stay in the dark OPALNOVA theme.
- Open `Payments & Sales`.
- Open Payment & Collection and confirm its selectors and payment date picker keep the shared dark styling.
- Open `Production`.
- Open Production Board and confirm its stage selector still uses the dark dropdown face and selected values remain readable.
- Open `Supplier Holds & Orders` or Diamond Supplier Studio and confirm hold/arrival date pickers remain readable.
- In Custom Quote Builder, confirm linked stone/material/external diamond rows use readable plain separators.
- In Advanced Search, confirm saved view names still display clearly in the saved-view selector.
- In Stock Movement and Add to Batch workflows, confirm material/batch selector rows remain readable.
- Confirm selector changes still update the workflow as before:
  - quote customer/stock/material selectors,
  - production board filters,
  - payment job/date entry,
  - supplier diamond hold/order dates.
- Confirm no workflow action creates or changes records just by opening a selector or date picker.
- Confirm Debug build, Release publish and published launch smoke have passed before treating this as release-ready.
