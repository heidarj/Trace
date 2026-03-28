# CLAUDE.md

This repository is **Trace**, a lean starter for a web-first Azure vulnerability investigation platform.

## What this repo is optimizing for
- Simple structure over speculative architecture
- Web-first user experience
- Clean future path to Teams tab hosting
- Clean future path to MCP tool exposure
- Deterministic, typed investigation data

## Architecture summary
- `.NET 10` API in `src/Api`
- `Aspire 9` AppHost in `src/AppHost`
- Shared contracts in `src/Contracts`
- Shared service defaults in `src/ServiceDefaults`
- `React + TypeScript + Fluent UI` frontend in `web/app`
- `Cosmos DB` persistence with:
  - `InvestigationRuns`
  - `RunTenantData`

## Important conventions
- API contracts should stay strongly typed.
- Endpoint files define routes only; services own business logic.
- Repositories own Cosmos queries and persistence behavior.
- Review actions should preserve optimistic concurrency behavior.
- Keep the repo lean; do not add extra layers or projects unless needed by a real use case.

## Domain vocabulary
Prefer the existing names already used in contracts and services:
- `CveInvestigationRequest`
- `InvestigationRun`
- `InvestigationRunSummary`
- `InvestigationContext`
- `TenantInvestigationResult`
- `Finding`
- `EvidenceArtifact`
- `ReviewDecision`
- `TicketRecommendation`
- `InvestigationStatus`
- `ExposureVerdict`
- `FindingType`

## Frontend expectations
- Use Fluent UI React components instead of plain HTML where practical.
- Build domain-specific screens for investigations, run details, tenant details, review queue, and tickets.
- Avoid raw JSON in the normal flow.
- Preserve the browser-first layout and routing model.

## Future-facing constraints
- Teams support is a later host surface, not the main app shell.
- MCP is a later thin layer over backend services.
- ServiceNow and Slack are later adapters.
- Do not make APIM a required dependency.
- Do not add real Azure scanning integrations unless asked.

## Validation
For code changes, prefer the existing commands:
- `dotnet build`
- `dotnet test tests/Api.Tests/`
- `cd /home/runner/work/Trace/Trace/web/app && npm run build`
- `cd /home/runner/work/Trace/Trace/web/app && npm test`

For doc-only changes, lightweight verification is enough.
