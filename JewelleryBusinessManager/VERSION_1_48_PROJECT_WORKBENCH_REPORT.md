# OPALNOVA V1.48 — Project Workbench + Guided Workflow

## Purpose

This build returns OPALNOVA development to functional workflow improvements after the V1.47 UI cleanup pass. The goal is to make OPALNOVA feel more complete and efficient than BenchFlow by placing the entire client/job/diamond/payment workflow into one guided command screen.

## Added

### Project Workbench

A new Project Workbench has been added and can be opened from:

- the main sidebar as **Project Workbench**;
- the quick toolbar as **Project Hub**;
- the top Workflow menu.

The Project Workbench combines active business work from:

- custom quotes;
- quote options;
- accepted quotes;
- linked production jobs;
- payments and balances;
- external/Nivoda supplier diamonds;
- supplier diamond holds and orders;
- customer follow-up tasks.

### Next-action workflow

Each row receives a recommended next action such as:

- finish/send proposal;
- refresh expired quote;
- convert accepted quote to production;
- request or confirm external diamond hold;
- check supplier diamond expiry;
- move production forward;
- record balance/payment;
- arrange pickup or shipping;
- follow up with a customer.

### Priority scoring

Items are grouped by priority:

- Urgent;
- High;
- Medium;
- Low.

Urgent and high-priority rows appear in the default **Action needed** filter.

### Dashboard-style counts

The Project Workbench shows live counts for:

- needs action;
- quotes;
- production;
- balances;
- diamonds;
- follow-ups.

### Customer message helper

For selected rows, OPALNOVA now creates a draft customer message. This can be copied to the clipboard and adapted before sending.

### Follow-up task creation

A selected project row can create a linked follow-up task without leaving the workbench. The task is linked to customer/job where possible and shown on the normal dashboard/work queue.

### Workflow shortcut panel

The right side of the workbench opens the main workflow tabs:

- Quotes;
- Production;
- Payments;
- Diamond Holds;
- Diamond Search;
- Customers;
- Jobs.

## Kept stable

No database schema changes were made.

No changes were made to:

- quote calculations;
- Nivoda API search;
- external diamond saving;
- external diamond quote linking;
- supplier hold/order logic;
- production status movement;
- payment/collection logic;
- reports;
- backups.

## Why this helps OPALNOVA compete with BenchFlow

BenchFlow's strength is that it guides the user through quote, acceptance, production and invoice stages. OPALNOVA now goes further by adding supplier diamond risks, balances, customer follow-ups and owned workflow context into one local command screen.

The new workbench reduces navigation burden and makes OPALNOVA's broader feature set easier to use.

## Validation completed in this package

- New XAML parsed successfully.
- Existing XAML parsed successfully.
- MainWindow event wiring for Project Workbench was added.
- New ProjectWorkbenchWindow event handlers were checked.
- C# brace balance passed for modified files.
- Project XML version updated to 1.48.0.

A full Visual Studio build is still required on Windows because this environment does not include the .NET SDK.
