# OPALNOVA V1.47.1 — Tabbed UI Testing Checklist

## Navigation

1. Open OPALNOVA.
2. Confirm the simplified sidebar shows only the main business areas.
3. Use the top menu to open a lesser-used record section such as Suppliers or Purchase Orders.
4. Use the quick toolbar to open New Quote, Production Board, Payments, Diamond Search and Diamond Holds.

## Tabs

1. Open New Quote from the quick toolbar.
2. Open Production Board without closing the quote tab.
3. Open Payment & Collection without closing the other tabs.
4. Switch between the open tabs.
5. Close one tab using its small x button.
6. Close all tabs and confirm the normal main workspace returns.

## Record editing

1. Open Customers.
2. Click Add.
3. Confirm the editor opens as a tab, not a blocking modal window.
4. Save a test customer.
5. Confirm the tab closes and the Customers list refreshes.
6. Select a record and click Edit.
7. Confirm the editor opens as a tab.
8. Change a harmless field, save, and confirm the list refreshes.
9. Open another edit tab and use Cancel to close it.

## Workflow tabs

1. Open Custom Quotes in a tab.
2. Open Production Board in a tab.
3. Open Diamond Search in a tab.
4. Search/save a supplier diamond if needed.
5. Open Diamond Holds in a tab.
6. Confirm none of these tabs lock the main window.

## Regression checks

1. Run the Nivoda supplier search with user-entered credentials.
2. Save an external diamond.
3. Link an external diamond to a quote option.
4. Open supplier diamond hold/order workflow.
5. Open production board and move a job status.
6. Record a payment in Payment & Collection.
7. Create a backup.
8. Publish standalone and confirm OPALNOVA.exe opens.
