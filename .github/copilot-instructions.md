# Trace Copilot Instructions

Trace is a web-first Azure CVE investigation platform. Its investigation execution should be built around Microsoft Agent Framework rather than ad hoc prompt-driven logic.

## Stack
- Backend: .NET 10 Minimal API
- Investigation orchestration: Microsoft Agent Framework
- Local composition: .NET Aspire 9
- Frontend: React 19 + TypeScript + Fluent UI React v9
- Database: Azure Cosmos DB for NoSQL
- Tests: xUnit for backend, Vitest for frontend

## Repo shape
- `src/Contracts` — shared strongly typed DTOs and enums
- `src/ServiceDefaults` — OpenTelemetry, health checks, service discovery
- `src/AppHost` — Aspire host for API, frontend, and Cosmos emulator
- `src/Api` — endpoints, services, repositories, seed data, and workflow seams
- `web/app` — React app
- `tests/Api.Tests` — backend unit tests

## Product intent
- Keep the app web-first.
- Do not make Microsoft Teams the primary shell.
- `POST /investigations` should start an Agent Framework-driven investigation workflow.
- Use one research and enrichment stage first, then one tenant-scoped investigation workflow per tenant, then a final aggregation and report stage.
- Keep MCP, Slack, and ServiceNow as thin trigger or report adapters over the same backend and workflow services.
- Favor deterministic evidence collection and typed persistence over freeform LLM conclusions.

## Workflow model
- The research stage should produce a typed `InvestigationContext`.
- `InvestigationContext` should carry normalized CVE IDs, affected products, version ranges, KB or fix data, exploitation status, detection hints, and collector queries or filters.
- Tenant workflows must consume that typed payload, not raw prompt text.
- Prefer names such as `CveResearchWorkflow`, `TenantInvestigationWorkflow`, `AzureEvidenceCollector`, `DefenderEvidenceCollector`, `FindingCorrelator`, and `ReportComposer` when adding the workflow layer.
- Use sequential orchestration for research, concurrent fan-out for tenants, and checkpointing or resume support when runs become long-lived.

## Evidence guidance
- Use Azure Lighthouse and Azure Resource Graph for Azure ARM and control-plane evidence only.
- Treat Defender for Endpoint, TVM, and XDR as a separate auth and collection path using a multitenant Entra app plus tenant-specific tokens.
- Prefer Defender endpoints that directly provide software inventory and vulnerability or device exposure evidence.
- Use the LLM to normalize or enrich CVE research when useful, but keep final findings grounded in typed evidence.

## Coding expectations
- Prefer small, explicit changes.
- Use strongly typed DTOs for API contracts and workflow handoffs.
- Keep business logic in services and workflows, not endpoints.
- Keep data access in repositories or thin Cosmos data access helpers.
- Do not introduce extra projects or enterprise layering without a clear need.
- Reuse the existing domain language:
  - investigation run
  - investigation context
  - tenant result
  - finding
  - evidence artifact
  - review decision
  - ticket recommendation
- When workflow-specific aggregate contracts are added, prefer names such as `TenantFinding` and `GlobalFindingReport`.

## Cosmos guidance
- `InvestigationRuns` stores one document per run for summary and recent-run views.
- `RunTenantData` stores per-run tenant summaries and related records with a `documentType` discriminator.
- Preserve optimistic concurrency for review actions.
- Do not create a new Cosmos container per record type unless an access pattern clearly requires it.

## Frontend guidance
- Build purpose-specific UI, not raw JSON viewers.
- Use Fluent UI React heavily: tables, cards, badges, dialogs, tabs, progress indicators, toolbars.
- Keep routing and layout suitable for a normal browser first.
- It should remain easy to host the same app in a Teams tab later.
- Surface investigation progress, findings, and evidence as structured product UI, not prompt transcripts.

## Local development
- Prefer Aspire for local startup.
- Local happy path should not require real Azure credentials.
- Seed data or deterministic local collectors should keep the UI useful on first run.

## Useful commands
- `dotnet build`
- `dotnet test tests/Api.Tests/`
- `cd /home/runner/work/Trace/Trace/web/app && npm test`
- `cd /home/runner/work/Trace/Trace/web/app && npm run build`
- `cd /home/runner/work/Trace/Trace/src/AppHost && dotnet run`

## Avoid
- Do not pass CVE research downstream as freeform prompt blobs.
- Do not let LLM output directly determine final findings, verdicts, or review state.
- Do not treat Lighthouse as a general data-plane or Defender auth solution.
- Do not add Teams-first scaffolding.
- Do not add real ServiceNow, Slack, or MCP adapters unless explicitly requested.
- Do not move business logic into controllers or endpoints.
- Do not replace typed models with loosely typed payloads.
