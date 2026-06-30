# OPALNOVA V1.88.0 Practical Jeweller Tools Testing Checklist

Use this checklist against the published V1.88 build.

## Startup

- Launch `OPALNOVA.exe`.
- Confirm the header shows `Version 1.88.0 - practical jeweller tools`.
- Open About and confirm it shows `Version 1.88.0 - Practical Jeweller Tools`.

## Entry Points

- Open `Pricing Studio`, then click `Jeweller Tools`.
- Open `Hardware & POS Studio`, then click `Jeweller Tools`.
- Click `Search All`, search `ring size`, and open the Jeweller Tools workflow result.
- Confirm each path opens the Jeweller Tools window.

## Ring Size Reference

- Confirm the ring-size grid shows AU/UK, US, EU, inside diameter and inside circumference columns.
- Confirm common sizes from H through Z are visible or reachable by scrolling.

## Metal Weight Estimator

- Choose `Sterling Silver`.
- Enter length `50`, width `4`, thickness `1.5`.
- Click `Calculate`.
- Confirm the result shows an estimated gram weight and density.
- Change the metal to `18ct Gold` and confirm the estimated weight changes.

## Stone Carat Estimator

- Choose `Round`.
- Enter length `7`, width `7`, depth `4.2`.
- Click `Calculate`.
- Confirm the result shows an estimated carat weight.
- Change the shape to `Oval` or `Emerald / Rectangle` and confirm the estimate changes.

## Copy And Safety

- Click `Copy Results`.
- Paste into a scratch text field and confirm the summary is copied.
- Confirm no database records are created or changed by opening and using Jeweller Tools.
- Confirm Pricing Helper and Camera & Scale Capture still open normally.
- Close the app cleanly.

