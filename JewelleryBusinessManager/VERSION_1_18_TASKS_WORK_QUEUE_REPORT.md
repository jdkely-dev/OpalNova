# Version 1.18 — Tasks, Reminders & Work Queue

## Added

- New `Tasks` section under `Tasks & Reminders` navigation.
- New task/reminder model with due dates, reminders, priorities, statuses and linked business records.
- New toolbar menu: `Tasks`.
- New actions:
  - New Task
  - Work Queue
  - Complete Task
  - Create Follow Ups
  - Task Report
- New dashboard tile group: `Tasks & Reminders`.
- Automatic suggested follow-up task creation for:
  - Overdue jobs
  - Jobs due within 7 days
  - Low-stock materials
  - Upcoming markets
  - Online listings needing work
  - Purchase orders expected soon
- Printable HTML Work Queue report.
- Printable HTML Task & Reminder report.
- Tasks included in export bundle CSVs and health check counts.

## Database compatibility

Existing databases are upgraded safely using `CREATE TABLE IF NOT EXISTS BusinessTasks` during startup. No destructive migration is performed.

## Validation

Two static validation passes were performed. These checked:

- ZIP/project structure
- XAML XML parsing
- XAML event-handler wiring
- Task model and enum presence
- DbContext and manual SQLite upgrade schema
- Task dashboard tile bindings
- Task menu action handlers
- Task service report generation
- No raw interpolated string literals
- No obvious newline-in-string issues
- Data safety export/health-check integration

## Recommended test workflow

1. Build and run.
2. Open `Tasks`.
3. Use `Tasks → New Task`.
4. Link the task to a customer, job, jewellery item, stone, market, batch or purchase order.
5. Use `Tasks → Work Queue`.
6. Use `Tasks → Complete Task`.
7. Use `Tasks → Create Follow Ups`.
8. Use `Tasks → Task Report`.
9. Check dashboard task tiles.
10. Test backup, export bundle, restore and restart persistence.
