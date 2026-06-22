# Version 1.34 — Customer Relationship Studio

## Purpose
Adds a safer customer relationship workflow layer without changing the database schema. This build focuses on making customer records more useful for custom jewellery work, repeat clients, follow-ups and after-sale care.

## Added
- New **Customer Relationship Studio** navigation item.
- New **Customer Summary Card** tool.
  - Shows customer contact details, preferences, notes, jobs, sales, payments, open follow-ups, next follow-up and last activity.
- New **Create Customer Follow-Up** tool.
  - Creates a linked `BusinessTask` for the selected customer.
  - Pre-fills customer follow-up category, due date, reminder date and preference notes.
  - Opens the normal edit window before saving so the task can be adjusted.
- New **Relationship Report** tool.
  - Shows all customers with job counts, active job counts, sale totals, open follow-ups, last activity and next follow-up.
- Existing **Customer History** report is also surfaced in the new studio for easier access.

## Files changed
- `MainWindow.xaml`
  - Updated version label to V1.34.
  - Added Customer Relationship Studio to the navigation tree.
- `MainWindow.xaml.cs`
  - Added Customer Relationship Studio to the tool section list.
  - Added tool actions and setup handlers.
  - Added handlers for summary card, customer follow-up task creation and relationship report.
- `Services/CustomerRelationshipService.cs`
  - New service for customer summary reports, relationship overview reports and linked customer follow-up task generation.

## Safety notes
- No database schema changes.
- No migration required.
- Existing customer, job, sale, payment and task models are reused.
- The new customer follow-up workflow saves into the existing `BusinessTasks` table.

## Validation performed in this environment
- ZIP extraction succeeded.
- XAML parsing passed.
- MainWindow.xaml.cs brace balance passed.
- CustomerRelationshipService.cs brace balance passed.
- New service uses existing model properties and enum values.

## Not performed here
A full `dotnet build` could not be run in this environment because the .NET SDK is not installed. Please run Build in Visual Studio after extracting.
