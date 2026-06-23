# OPALNOVA V1.56.0 - Customer Timeline and Profile Polish

## Summary

V1.56.0 improves customer relationship context by adding a customer timeline report and strengthening customer summary cards. It reuses existing quote, proposal, job, sale, payment and task records without changing the database schema.

## Changes

- Added `CustomerRelationshipService.CreateCustomerTimeline()`.
- Added shared customer timeline event generation for quotes, proposal-sent events, jobs, sales, payments and tasks.
- Added `Customer Timeline` to Customer Relationship Studio.
- Added a customer selector setup flow for timeline generation.
- Improved customer summary cards with quote count, open quote count, recent quotes and recent timeline events.
- Updated release notes and About text to V1.56.0.
- Bumped project and visible version metadata to 1.56.0.

## Data Safety

- No database schema changes were introduced.
- No customer records are changed by opening the timeline or summary card.
- Follow-up creation still uses the existing explicit Create Customer Follow-Up action.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
