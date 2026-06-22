# Version 1.33 — Unified Workspace Tool Conversion

This version converts more tools into the two-page workspace pattern introduced in V1.32.

## Main change

Tools that benefit from choosing records or options now open on the Setup / Inputs page first. The user can select records from dropdowns/lists before running the tool, and generated output still opens on the Preview / Result page.

## Converted tool areas

- Inventory Studio: Stock Movement, Change Status, Trace Selected
- Purchasing Studio: Mark Ordered, Receive Purchase Order, Purchase Order Printout
- Production & Opal Studio: Add To Batch, Batch Progress, Batch Report, Parcel Yield, Stone Workflow
- Market Studio: Market Prep, Market Sale, Reconcile Market, Packing List, Reconciliation Report
- Online Selling Studio: Create Listing, Generate Content, Listing Checklist
- Tasks Studio: New Task and Complete Task
- Codes & Labels Studio: Selected Scan Label and Label Sheet
- Documents Studio: Job Card, Stock Label, Quote, Invoice/Receipt, Deposit Receipt, Repair Form, Agreement, Payment Summary, Customer History
- Hardware & POS Studio: DYMO Mini Label and Camera & Scale Capture

## Preserved behaviour

Existing one-click tools, global reports and safety tools remain available. Bulk Status Update and Bulk Add Selected To Market keep their multi-select in-workspace panels.

## Validation

Static validation checked:

- ZIP integrity
- Project XML parsing
- App and MainWindow XAML parsing
- Tool action handler references
- C# brace balance
- Setup/Input tab wiring
- No new interpolated raw string blocks
