# OPALNOVA Reference Walkthrough Analysis

Source: `C:\Users\jack\Downloads\reaserch vid.mp4`
Analyzed: 2026-06-22

Companion transcript: `OPALNOVA_REFERENCE_VIDEO_TRANSCRIPT.md`
Raw machine transcript: `OPALNOVA_REFERENCE_VIDEO_TRANSCRIPT_RAW.srt`

## What The Walkthrough Shows

The reference app is a web-based jewellery workflow app with a restrained, work-focused layout. The audio transcript confirms it is an end-to-end custom jewellery workflow covering quote creation, proposal generation, client acceptance, invoicing, production management, supplier integrations, pricing setup, reporting, settings, and help.

The video mostly shows these useful patterns:

- a dashboard/home screen built around onboarding progress and next setup actions.
- a pricing database that makes quote setup explicit before the first quote.
- a quote workflow with multiple options, live pricing, images, measurements, internal notes, and option duplication.
- a proposal workflow that treats sending the proposal as a simple guided action, not a separate complex workflow.
- a client-side proposal acceptance path that feeds production, invoicing, and reporting.

## Relevant UI Patterns

### Guided Dashboard

The dashboard opens with:

- a direct welcome heading.
- a milestone/success banner after the first quote and proposal are created.
- clear CTA buttons such as "Create another quote" and "Stay on dashboard".
- a "Getting Started" checklist with progress count and progress bar.
- checklist rows with completed states and action buttons.

OPALNOVA already has a rich dashboard and Project Hub, but the reference app makes the first-run path clearer. The lesson is not to add more dashboard tiles; it is to add a focused setup/progress checklist for new or incomplete workspaces.

### Simple Workflow Navigation

The left navigation is split into workflow and admin areas:

- Workflow: Dashboard, Clients, Quotes, Proposals, Invoices, Production, Reports.
- Admin: Pricing Database, Settings, Help/Support, Billing.

OPALNOVA already has more desktop-oriented modules. The useful idea is a clearer separation between daily workflow modules and setup/admin modules, not necessarily copying the exact navigation.

### Proposal Send Modal

The proposal flow uses a modal-style send action with:

- recipient field.
- subject field.
- message body.
- an include/review PDF style option.
- Cancel and Send action buttons.

For OPALNOVA, this should shape V1.50. Premium proposal output should include a send/record workflow:

- generate proposal output.
- prepare recipient, subject, and message from templates.
- open an email draft or copy/send package first.
- record proposal as sent.
- create follow-up automatically.

Direct SMTP/email delivery can wait until settings and reliability are handled.

### End-To-End Acceptance Flow

The transcript describes a path from quote to proposal, client option selection, terms acceptance, signed/accepted proposal state, production job creation, and invoice generation. OPALNOVA already has quote-to-job and payment foundations, but the status surfaces should be clearer:

- quote created.
- proposal prepared.
- proposal sent.
- proposal viewed or manually confirmed.
- option accepted.
- deposit/invoice required.
- production job created.

### Production Workflow Signals

The reference app includes production stages, editable step lists, status, priority, due date, material ordered/received flags, waiting-on-stone style warnings, bench photo/renders, team allocation, contractors, time tracking, and calendar scheduling.

For OPALNOVA, this supports keeping V1.52 focused on safe job completion and stock movement, then expanding production with stage checklists and waiting-state visibility.

### Supplier/Pricing Setup

The setup checklist includes supplier/pricing database connection work. For OPALNOVA this maps to:

- business profile complete.
- labour rates configured.
- GST/settings configured.
- supplier/Nivoda credentials entered.
- pricing/material defaults reviewed.
- backup health checked.

## Development Plan Impact

The video changes the plan in three ways:

- V1.50 should become Premium Proposal Output and Send Workflow, not only prettier HTML/PDF.
- Guided first-run setup should move earlier, likely V1.50.x or V1.51, because it is high-impact and mostly reuses existing settings/task/dashboard foundations.
- A future Proposal Centre is useful, but it should follow the proposal send/record workflow rather than precede it.

## What Not To Copy

- Do not copy the light web UI style; OPALNOVA should keep its dark desktop theme.
- Do not add billing/subscription concepts unless this becomes a commercial SaaS product.
- Do not separate every concept into more navigation items yet; OPALNOVA already has many modules.
- Do not implement direct email sending before a reliable draft/record workflow exists.
