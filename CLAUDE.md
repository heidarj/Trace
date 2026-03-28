# CLAUDE.md

This repository is **Trace**, a lean starter for a web-first Azure vulnerability investigation platform with investigation execution centered on Microsoft Agent Framework.

## What this repo is optimizing for
- Simple structure over speculative architecture
- Web-first user experience
- Agent Framework-backed investigation orchestration
- Typed `InvestigationContext` handoffs between stages
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
- Workflow direction:
  - `CveResearchWorkflow`
  - `TenantInvestigationWorkflow`
  - `AzureEvidenceCollector`
  - `DefenderEvidenceCollector`
  - `FindingCorrelator`
  - `ReportComposer`

## Important conventions
- API contracts and workflow handoffs should stay strongly typed.
- Endpoint files define routes only; services and workflows own business logic.
- Repositories own Cosmos queries and persistence behavior.
- Review actions should preserve optimistic concurrency behavior.
- Research output must be a typed `InvestigationContext`, not prompt prose passed through the stack.
- Azure evidence and Defender evidence are separate collection paths with separate auth models.
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

When workflow-specific correlated output contracts are added, prefer names such as:
- `TenantFinding`
- `GlobalFindingReport`

## Investigation shape
- Research and enrich the CVE once before tenant fan-out.
- Launch one tenant-scoped workflow per onboarded tenant.
- Consume structured context in collectors and correlators.
- Use Azure Lighthouse and ARM-compatible services for control-plane evidence.
- Use a multitenant Entra app and tenant-specific tokens for Defender for Endpoint, TVM, and XDR APIs.
- Use checkpointing when the orchestration needs to resume long-running work.

## Frontend expectations
- Use Fluent UI React components instead of plain HTML where practical.
- Build domain-specific screens for investigations, run details, tenant details, review queue, and tickets.
- Avoid raw JSON in the normal flow.
- Preserve the browser-first layout and routing model.
- Show workflow results and evidence, not raw agent transcripts.

## Future-facing constraints
- Teams support is a later host surface, not the main app shell.
- MCP is a later thin layer over backend services and workflow entry points.
- ServiceNow and Slack are later adapters or trigger surfaces.
- Do not make APIM a required dependency.
- Do not add real Azure or Defender collectors unless asked.
- Do not let freeform LLM output determine final findings.

## Validation
For code changes, prefer the existing commands:
- `dotnet build`
- `dotnet test tests/Api.Tests/`
- `cd /home/runner/work/Trace/Trace/web/app && npm run build`
- `cd /home/runner/work/Trace/Trace/web/app && npm test`

For doc-only changes, lightweight verification is enough.
