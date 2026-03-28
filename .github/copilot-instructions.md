# Trace Copilot Instructions

Trace is a web-first Azure CVE investigation platform.

## Stack
- Backend: .NET 10 Minimal API
- Orchestration: .NET Aspire 9
- Frontend: React 19 + TypeScript + Fluent UI React v9
- Database: Azure Cosmos DB for NoSQL
- Tests: xUnit for backend, Vitest for frontend

## Repo shape
- `src/Contracts` — shared strongly typed DTOs and enums
- `src/ServiceDefaults` — OpenTelemetry, health checks, service discovery
- `src/AppHost` — Aspire host for API, frontend, and Cosmos emulator
- `src/Api` — endpoints, services, repositories, seed data
- `web/app` — React app
- `tests/Api.Tests` — backend unit tests

## Product intent
- Keep the app web-first.
- Do not make Microsoft Teams the primary shell.
- Keep MCP support as a future thin surface over existing backend services.
- Treat ServiceNow and Slack as future adapters, not core dependencies.
- Favor deterministic evidence collection and persistence over LLM-generated findings.

## Coding expectations
- Prefer small, explicit changes.
- Use strongly typed DTOs only for API contracts.
- Keep business logic in services, not endpoints.
- Keep data access in repositories or thin Cosmos data access helpers.
- Do not introduce extra projects or enterprise layering without a clear need.
- Reuse the existing domain language:
  - investigation run
  - tenant result
  - finding
  - evidence artifact
  - review decision
  - ticket recommendation

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

## Local development
- Prefer Aspire for local startup.
- Local happy path should not require real Azure credentials.
- Seed data should keep the UI useful on first run.

## Useful commands
- `dotnet build`
- `dotnet test tests/Api.Tests/`
- `cd /home/runner/work/Trace/Trace/web/app && npm test`
- `cd /home/runner/work/Trace/Trace/web/app && npm run build`
- `cd /home/runner/work/Trace/Trace/src/AppHost && dotnet run`

## Avoid
- Do not add Teams-first scaffolding.
- Do not add real Defender, ARG, ServiceNow, Slack, or MCP implementations unless explicitly requested.
- Do not move business logic into controllers/endpoints.
- Do not replace typed models with loosely typed payloads.
