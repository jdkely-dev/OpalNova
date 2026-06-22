# Version 1.11 Double Validation Report

Two static validation passes were run before packaging.

## Pass 1

- XAML/XML parsed successfully.
- Project file parsed successfully.
- XAML event handlers matched code-behind.
- New production batch views exist.
- New production batch models exist.
- AppDbContext includes DbSets for new models.
- Database bootstrapper includes schema creation for new tables.
- New dashboard card names match code-behind references.
- New toolbar buttons are wired to MainWindow methods.
- C# brace balance scan passed.
- Common missing `System.IO` checks passed.

Result: Blocking errors 0, warnings 0.

## Pass 2

The final ZIP was extracted into a clean folder and the same validation checks were run again.

Result: Blocking errors 0, warnings 0.

## Remaining limitation

This environment does not have the Windows .NET/WPF compiler available, so the final compiler/runtime test should still be performed in Visual Studio on Windows.
