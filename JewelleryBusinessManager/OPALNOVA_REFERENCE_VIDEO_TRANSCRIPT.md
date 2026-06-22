# OPALNOVA Reference Video Transcript And Feature Analysis

Source: `C:\Users\jack\Downloads\reaserch vid.mp4`
Created: 2026-06-22
Method: FFmpeg Whisper `base.en` machine transcript plus visual frame review.
Raw transcript: `OPALNOVA_REFERENCE_VIDEO_TRANSCRIPT_RAW.srt`

## Transcript Quality Notes

The raw transcript was generated locally using FFmpeg's `whisper` filter and `ggml-base.en.bin`.

The transcript is usable for feature discovery, but it is not courtroom-accurate. Expect misheard product names and duplicated/overlapping phrases because FFmpeg processed the audio in short queue windows. The main corrections used in this analysis are:

- "Benchflow" is the reference app.
- "Morrison Watson", "Mars and Watson", and similar phrases refer to Morris & Watson.
- "State of Origin" is misheard in the intro.
- "quite/quads" frequently means quote/quotes.
- "library" frequently means labour.

## Chaptered Transcript

### 00:00-02:00 - Intro and end-to-end workflow

The presenter introduces himself as a manufacturing jeweller and co-founder, then frames the walkthrough as an end-to-end Benchflow demonstration: creating a quote, generating a proposal, client acceptance, invoicing, production management, supplier integrations, and the pricing database. The motivation is replacing scattered spreadsheets and subscriptions with one system for custom workshop operations.

Feature signal for OPALNOVA:

- Keep improving OPALNOVA as an end-to-end jeweller workflow rather than isolated modules.
- The product story should connect quote, proposal, acceptance, invoice, production, and reporting.

### 02:00-08:00 - Dashboard onboarding and pricing database

The dashboard is described as the first screen for a new account, with step-by-step onboarding links that take the user directly to the next setup area before they create the first quote.

The pricing database is a major foundation. It includes supplier metal integration with Morris & Watson, daily pricing sync, manual prices, castings, fabricated metals, markups, scrap pricing, natural and lab-grown stones, gemstones, setting costs, chains, findings, labour rates, CAD/manufacturing distinctions, deposits, and custom categories.

Feature signal for OPALNOVA:

- Add a guided setup checklist earlier than originally planned.
- Treat pricing/material/labour setup as quote-readiness, not as hidden admin.
- OPALNOVA already has richer inventory foundations; the next improvement is surfacing readiness and gaps clearly.

### 08:00-12:00 - Client import and quote creation

The client screen supports bulk import from an existing CRM, requiring name, email, and phone number, with optional birthdays and anniversaries. Clients can also be added manually from either the client screen or during quoting.

The quote workflow starts with multiple options. Each option has a title, timeframe, description, images/designs, and a client lookup or creation step.

Feature signal for OPALNOVA:

- Keep client creation/search inside quote creation.
- Add import polish later, but do not let it outrank proposal/production workflow work.
- Quote options should remain visually distinct and easy to compare.

### 12:00-16:00 - Quote metadata, internal notes, measurements, gifts, and live pricing

The quote captures job type and category for later reporting. It also stores internal notes that the client does not see, customer measurements such as ring size, gift/occasion details, due dates, and personal notes for the proposal.

Pricing updates live while labour, CAD, casting cleanup, handmade components, and other line items are entered. The presenter emphasizes that this depends on the pricing database being filled in first.

Feature signal for OPALNOVA:

- OPALNOVA quote fields should separate internal notes from customer-facing proposal text.
- Measurements, occasion dates, and due dates are high-value quote context.
- Live pricing is useful only if the setup state is trustworthy, so setup readiness should be visible.

### 16:00-21:00 - Settings, stones, metals, duplicate options, autosave, and proposal generation

The quote adds setting costs, precious metal costs from supplier feeds, centre stone details, certificates, costs, markups, miscellaneous charges, shipping, insurance, valuations, and scrap credits.

The presenter duplicates an option to create a second metal/version quickly, changes values, and shows the two prices side by side. The quote autosaves during the workflow. A proposal can be generated from the quote with one click and becomes the client-facing approval surface.

Feature signal for OPALNOVA:

- OPALNOVA's option comparison work is aligned with the reference workflow.
- Duplicating options and preserving linked pricing/media is a key usability pattern.
- Proposal generation should remain one action from the quote workspace.

### 21:00-25:00 - Proposal payment schedule, sections, images, discounts, and sending

The proposal supports payment schedules, such as a deposit after acceptance and a final balance before collection. It supports payment timing rules, proposal sections for multiple pieces, optional display of individual stone pricing, design image upload, discounts or complimentary add-ons, and a send flow.

The send flow includes copying a proposal link for text/WhatsApp or sending by email. The email body can be template-driven and editable before sending.

Feature signal for OPALNOVA:

- V1.50 should include proposal send/record, not only prettier output.
- Payment schedule display belongs inside proposal output.
- Proposal templates should cover email subject/message and customer-facing terms.
- Direct email sending can wait; draft/copy/record-sent is the safer first OPALNOVA version.

### 25:00-28:00 - Client-side proposal acceptance and production job creation

The client sees proposal options, images, descriptions, material breakdowns, deposit amount, and terms and conditions. The client selects an option, accepts terms, and the system records acceptance/signing.

After acceptance, the system returns to the proposal section showing sent/viewed/signed state, then automatically creates production work.

Feature signal for OPALNOVA:

- Proposal sent/viewed/accepted state should become visible before building a larger Proposal Centre.
- Acceptance should connect directly to job creation and deposit/payment next actions.
- OPALNOVA should keep manual acceptance recording first, then later add richer digital acceptance.

### 28:00-33:00 - Production board, stages, team allocation, calendar, invoicing, and reporting

The production workflow has stages, editable steps, quote data pulled into the job, actual time tracking, job status, scheduled state, priority, material ordered state, waiting-on flags such as waiting on gemstone, due dates, received parts, photo uploads, bench shots/renders, and team-member allocation per stage.

The presenter also shows contractors, turnaround times, a calendar function with drag-and-drop scheduling, automatic invoice creation after proposal acceptance, manual invoice sending, split payments, branded invoice output, and reporting for quotes, proposals, conversion rate, invoiced jobs, and payments.

Feature signal for OPALNOVA:

- OPALNOVA V1.52 should keep inventory/job completion safe, but stage checklists, waiting statuses, files/photos, and time tracking are also important.
- Calendar/capacity work is valuable but should follow stable production stage data.
- Invoice polish should share design language with proposal output.

### 33:00-37:00 - Settings, branding, team permissions, supplier integrations, and help

Settings include branding colors, quote/proposal wording, team member invites, access levels, terms/wording, watermark overlays for design protection, gross profit guidelines, and supplier integration setup.

The app also has a built-in help system with about 70 articles, a searchable help widget, and a support message channel.

Feature signal for OPALNOVA:

- Proposal/invoice templates should eventually share branding, terms, and watermark settings.
- Help/user guide work belongs after the core quote/proposal/production workflow is stable.
- Supplier integration setup should stay guided, but OPALNOVA must not hardcode credentials.

### 37:00-38:39 - Beta feedback, subscription, and support

The presenter closes by discussing beta feedback, quick feature turnaround, pre-registration, launch timing, premium features, discounts, and support email.

Feature signal for OPALNOVA:

- Useful feedback loop idea, but subscription/billing mechanics are not currently relevant to OPALNOVA's local desktop direction.

## Feature Inventory

High-confidence features identified from the transcript and screen:

- Guided setup/onboarding checklist with direct links to setup tasks.
- Dashboard progress and first-quote/first-proposal milestones.
- Pricing database for metals, stones, settings, chains, findings, labour, markups, deposits, categories, and scrap.
- Supplier metal pricing integration with daily sync.
- Client bulk import from CRM.
- Manual client creation from both client and quote workflows.
- Quote options with title, timeframe, description, images, client, job type, category, notes, measurements, occasion details, labour, materials, stones, certificates, miscellaneous costs, shipping, insurance, scrap credits, and discounts.
- Live quote pricing from setup data.
- Duplicate quote option workflow.
- Autosave during quote creation.
- One-click proposal generation from quote.
- Proposal payment schedule and deposit/final-balance display.
- Proposal sections for multiple pieces.
- Optional individual stone price display.
- Proposal design image upload and watermarking.
- Proposal email template/send flow.
- Copy proposal link for text/WhatsApp.
- Client-facing proposal acceptance with selected option, deposit, terms, and signed/accepted state.
- Production job generation after proposal acceptance.
- Production stages/checklists, status, priority, due date, ordered/received materials, waiting-on flags, photos/renders, team allocation, contractors, time tracking, and calendar scheduling.
- Automatic and manual invoice generation/sending after acceptance.
- Branded invoice output.
- Reporting for quote/proposal counts, conversion rate, invoiced jobs, and payments.
- Branding, team permissions, terms, watermark, GP guideline, supplier setup, and help/support settings.
- Built-in searchable help articles and support message channel.

## OPALNOVA Priority Changes

The transcript strengthens these build priorities:

| Priority | OPALNOVA Work | Reason |
| --- | --- | --- |
| P1 | V1.50 proposal send/record modal | The send step is central to the proposal workflow, not a side feature. |
| P1 | Proposal email templates and copy/open draft | High workflow value without direct email-delivery risk. |
| P1 | Proposal sent/accepted status surface | Needed before a Proposal Centre is worth building. |
| P1 | Guided setup checklist | The reference app makes setup readiness a daily workflow aid. |
| P2 | Payment schedule display in proposal | Strong customer-facing clarity and supports deposits. |
| P2 | Quote measurements/occasion/internal-note polish | Useful quote context and proposal personalization. |
| P2 | Production stage checklist and waiting-on flags | Important after acceptance/job creation workflows are clearer. |
| P2 | Branded invoice/receipt template polish | Should share proposal styling. |
| P3 | Team allocation, contractor, and calendar scheduling | Valuable, but depends on stable production stage data. |
| P3 | Help/user guide/searchable support | Useful after core workflows settle. |

## Implementation Guidance

- Do not copy Benchflow's light SaaS UI. Keep OPALNOVA's dark navy desktop style.
- Do not implement subscription/billing concepts now.
- Do not jump straight to direct SMTP. Build proposal draft/copy/record first.
- Prefer guided setup/readiness over more dashboard clutter.
- Keep pricing/setup, proposal output, invoice output, and production job creation connected through existing OPALNOVA models.
