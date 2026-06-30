# OPALNOVA V1.76.0 - Production Stage Checklist

V1.76.0 continues production workflow polish without changing the database schema.

## Added

- Added `DocumentExportService.CreateProductionStageChecklist(...)`.
- Added Stage Checklist actions in:
  - Production Board
  - Production studio
  - Production & Opal Studio
  - Documents Studio
- The generated checklist reviews:
  - current stage and due date
  - customer contact readiness
  - linked quote and accepted option context
  - payment position and linked payment records
  - material, stone and supplier diamond links
  - waiting flags for customer, supplier, stone/material and payment blockers
  - open linked tasks
  - linked job photos/files
  - design notes, customer approval notes and internal bench notes

## Preserved

- No schema changes.
- Existing production board movement and job completion behavior remain unchanged.
- Existing quote, payment, reservation, photo and task records are read only in the checklist output.

