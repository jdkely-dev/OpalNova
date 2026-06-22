# OPALNOVA V1.47 — Streamlined UI Testing Checklist

## 1. Startup
- Open OPALNOVA in Visual Studio.
- Confirm the title/version shows V1.47.
- Confirm the left navigation is simplified.
- Confirm the top menu bar is visible.

## 2. Main left navigation
Open each left navigation item:

- Dashboard
- Customers
- Quotes & Proposals
- Production
- Payments & Sales
- Inventory
- Diamonds
- Reports
- Settings & Backup

Confirm each opens without errors.

## 3. Top menu bar
Use the top menu to open:

- a raw record section such as Suppliers or Purchase Orders
- a specialist tool such as Pricing Studio or Market Studio
- Reports Studio
- Settings & Backup
- Create Backup
- User Guide

Confirm hidden tools are still accessible.

## 4. Critical workflow regression
Test the main working flows still open:

- Custom Quote Builder
- Nivoda Diamond Search
- Saved External Diamonds
- Supplier Holds & Orders
- Production Board
- Payment & Collection
- BI Command Report
- Create Backup

## 5. Publish test
- Run the Windows x64 publish script.
- Open the published OPALNOVA.exe.
- Confirm the simplified navigation and top menu work in the published app.

## 6. What to report back
Report any of the following:

- a menu item that does nothing
- a main left section that opens the wrong screen
- a missing important tool
- a workflow action that used to be easy but is now too hidden
- any build or publish error
