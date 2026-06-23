# OPALNOVA V1.58 Stock Ageing Testing Checklist

## Stock Ageing Report

- Open `Reports`.
- Click `Stock Ageing`.
- Confirm the report opens in Preview / Result.
- Confirm age bands are visible.
- Confirm available stock count and value are shown.
- Confirm slow-moving inventory lists records older than 180 days.

## Reports Studio

- Open `Reports Studio`.
- Click `Stock Ageing`.
- Confirm it opens the same report.
- Confirm the help text is sensible if hover/help is used.

## Inventory Checks

- Confirm sold jewellery is not listed as available ageing stock.
- Confirm sold or set stones are not listed as available loose-stone ageing stock.
- Confirm reserved jewellery/stones still appear because they remain tied up in stock value.
- Confirm no inventory statuses change after generating the report.

## Regression Checks

- Run `Inventory Value`.
- Run `BI Command Report`.
- Run `Export BI Excel`.
- Run a debug build if testing from source.
