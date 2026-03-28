# AGENTS.md

## Repository
Trace is a production-minded starter for investigating CVE exposure across Azure customer tenants.

## Primary goals
- Start an investigation from a CVE ID
- Persist run summaries, tenant results, findings, reviews, and ticket recommendations
- Provide a polished web UI first
- Keep the backend easy to expose later through MCP tools

## Tech stack
- .NET 10
- .NET Aspire 9
- React 19 + TypeScript
- Fluent UI React v9
- Azure Cosmos DB for NoSQL

## Working rules for agents
- Make focused, minimal changes.
- Keep the repository simple.
- Prefer updating the existing projects instead of adding new ones.
- Preserve strong typing in backend contracts and frontend models.
- Keep business logic in services and persistence in repositories.
- Keep local development working without real Azure credentials.

## Key directories
- `src/Api`
- `src/AppHost`
- `src/Contracts`
- `src/ServiceDefaults`
- `web/app`
- `tests/Api.Tests`

## Persistence model
- `InvestigationRuns`: one document per run for landing page and summary views
- `RunTenantData`: run-scoped tenant summaries and related detail records, using a `documentType` discriminator

## UX rules
- Web app is the primary product surface.
- Teams compatibility should remain possible, but do not optimize for Teams first.
- Use Fluent UI patterns, not debug-centric raw object dumps.

## Future boundaries
- MCP support should stay easy to add as a thin layer.
- ServiceNow and Slack remain optional later adapters.
- No premature microservice split.

## Common commands
- Build backend: `dotnet build`
- Test backend: `dotnet test tests/Api.Tests/`
- Build frontend: `cd /home/runner/work/Trace/Trace/web/app && npm run build`
- Test frontend: `cd /home/runner/work/Trace/Trace/web/app && npm test`
- Run full app locally: `cd /home/runner/work/Trace/Trace/src/AppHost && dotnet run`
