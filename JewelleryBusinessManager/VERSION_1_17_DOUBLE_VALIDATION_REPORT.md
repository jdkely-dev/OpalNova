# V1.17 Double Validation Report

Two static validation passes were run before packaging.

## Pass 1

- XAML/XML parsing: passed
- Project file parsing: passed
- XAML event handlers: passed
- Purchase order model files: present
- Purchase order service file: present
- DbContext DbSet coverage: passed
- Database bootstrap schema coverage: passed
- Data bundle CSV export coverage: passed
- Dashboard tile references: passed
- Raw interpolated HTML/CSS strings: none found
- Previous MaterialTransaction UnitCost compile-risk check: passed

Blocking errors: 0

## Pass 2

A fresh extraction of the final ZIP was validated again.

- ZIP integrity: passed
- XAML/XML parsing: passed
- Event handler matching: passed
- Purchase order schema and service checks: passed
- New UI menu and dashboard references: passed
- Raw interpolated string check: passed

Blocking errors: 0
