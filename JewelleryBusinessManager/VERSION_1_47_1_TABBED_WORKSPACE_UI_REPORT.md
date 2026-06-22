# OPALNOVA V1.47.1 — Tabbed Workspace UI Streamline

## Purpose
This build continues the V1.47 interface consolidation work and adds a tabbed workspace layer so major workflows and record editing no longer lock the main OPALNOVA window.

## Main changes

- Added a title/menu/toolbar structure for faster navigation.
- Kept the main sidebar greatly simplified around daily workflow areas:
  - Dashboard
  - Customers
  - Quotes & Proposals
  - Production
  - Payments & Sales
  - Inventory
  - Diamonds
  - Reports
  - Settings & Backup
- Moved lesser-used records, specialist tools and admin features into top menu groups.
- Added a quick toolbar for:
  - New Quote
  - Production Board
  - Payments
  - Diamond Search
  - Diamond Holds
  - Reports
  - Backup
- Added a main-window tabbed workspace system.
- Record Add/Edit now opens in a tab instead of a blocking modal editor.
- Major workflow windows now open in tabs:
  - Custom Quotes
  - Production Board
  - Payment & Collection
  - Diamond Search
  - Diamond Holds
- Tabs can be switched back and forth without locking the main app.
- Each tab has its own close button.

## Safety notes

- No database schema changes were made.
- No quote calculation logic was changed.
- No Nivoda search/query logic was changed.
- No supplier diamond hold/order logic was changed.
- No production status logic was changed.
- No payment/sale creation logic was changed.
- The changes are interface/navigation focused.

## Implementation notes

Existing WPF workflow windows are hosted inside a main-window tab by detaching their content and keeping their existing event handlers. Record editors gained a hosted-tab mode so Save and Cancel can close the tab cleanly without using DialogResult.

## Validation performed

- MainWindow XAML parsed as XML.
- All MainWindow XAML handlers were checked against MainWindow.xaml.cs.
- Project XML version values were updated.
- Modified C# files passed brace-balance checks.
- ZIP integrity passed.

## Final check still required

A full Visual Studio build/publish should be run on Windows because this environment does not have the .NET SDK installed.
