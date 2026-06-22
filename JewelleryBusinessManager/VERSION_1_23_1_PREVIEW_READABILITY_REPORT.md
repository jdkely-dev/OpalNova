# Version 1.23.1 — Preview Panel Readability Upgrade

## Focus
This release improves the right-side record preview panel so selected records are quicker and easier to scan.

## Changes
- Added summary metric chips at the top of the preview panel.
- Reduced dense field lists from 12 key details to 8 high-value details.
- Changed preview detail rows from cramped two-column rows to stacked label/value cards.
- Added larger values and uppercase mini-labels for faster scanning.
- Grouped Key Details, Linked Records and Recent Activity into clearer cards.
- Improved preview header spacing and subtitle readability.
- Preserved quick actions: Edit, Add Photo, Trace and Scan Label.

## Examples of new summary chips
- Jewellery: Status, Retail, Profit, Code.
- Jobs: Status, Due Date, Balance, Quote.
- Materials: On Hand, Reorder, Unit Cost, Category.
- Stones: Status, Carats, Value, Code.
- Tasks: Status, Priority, Due Date, Category.

## Validation
Static validation checked:
- XAML/XML parsing.
- Project file parsing.
- MainWindow event-handler references.
- C# brace balance.
- DetailSummaryPanel XAML/code-behind wiring.
- No invalid OnlineListing property references.
- No raw interpolated string regressions.

Validation result: 0 blocking errors, 0 warnings.
