# Version 1.24.3 Customer Display Window Lifecycle Fix

Fixed a WPF runtime crash when reopening the market customer display after it had been closed.

## Cause

WPF windows cannot be shown again after they have been closed. The market operations window kept a reference to a closed CustomerDisplayWindow and later called Show() on it.

## Fix

- Added CustomerDisplay_Closed handler to clear the stored window reference.
- Added EnsureCustomerDisplayWindow() helper to create a fresh customer display when needed.
- Open Customer Display now creates a new window if the old one was closed.
- UpdateCustomerDisplay now only updates when the display exists and is visible.

## Validation

- XAML parsed successfully.
- Project file parsed successfully.
- Customer display event handler lifecycle checked.
- No interpolated raw strings found.
- C# brace balance checked.
