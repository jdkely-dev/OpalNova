# OPALNOVA V1.75.0 - Customer Communication Templates

V1.75.0 expands Customer Relationship Studio with customer-specific message starters and relationship value guidance.

## Implemented

- Added a `Communication Templates` action to Customer Relationship Studio.
- Added `CustomerRelationshipService.CreateCustomerCommunicationTemplates(...)`.
- Generated templates use existing customer preferences, quotes, jobs, sales, tasks and value guidance.
- Template types include quote/proposal check-in, production update, handover message, after-care check-in, repeat-customer prompt and open follow-up context.
- Customer summary cards and timelines now include lifetime value, value guidance, suggested next step and repeat follow-up suggestions.
- Customer relationship report now includes lifetime value, value guidance and suggested next step columns.
- Created customer follow-up tasks now include value guidance and a relevant message starter in follow-up notes.

## Preserved

- No database schema changes.
- No direct email sending was added.
- Existing customer summary, timeline, customer history, relationship report and follow-up creation entry points are preserved.
- Existing quote, job, sale, payment and task records remain unchanged.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
