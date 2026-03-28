# AGENTS.md

## Repository
Trace is a production-minded starter for investigating CVE exposure across Azure customer tenants. The intended investigation engine is Microsoft Agent Framework.

## Primary goals
- Start an investigation from a CVE ID
- Kick off a research-first, tenant-fan-out workflow
- Persist run summaries, tenant results, findings, reviews, and ticket recommendations
- Provide a polished web UI first
- Keep the backend easy to expose later through MCP tools and adapter surfaces

## Tech stack
- .NET 10
- Microsoft Agent Framework
- .NET Aspire 9
- React 19 + TypeScript
- Fluent UI React v9
- Azure Cosmos DB for NoSQL

## Workflow shape
- Run one CVE research and enrichment stage first.
- Produce a typed `InvestigationContext` from that stage.
- Fan out one `TenantInvestigationWorkflow` per tenant, concurrently when appropriate.
- Keep tenant workflows isolated and aggregate them later through a correlator or report composer.
- Use checkpointing when long-running investigations need resumability.

## Evidence rules
- Use Lighthouse and Azure Resource Graph for Azure ARM and control-plane evidence.
- Use a multitenant Entra app plus tenant-specific tokens for Defender for Endpoint, TVM, and XDR access.
- Do not pass CVE research into tenant workflows as prompt prose. Pass structured data.
- Do not let freeform LLM output stand in for typed evidence.

## Working rules for agents
- Make focused, minimal changes.
- Keep the repository simple.
- Prefer updating the existing projects instead of adding new ones.
- Preserve strong typing in backend contracts and frontend models.
- Keep business logic in services or workflows and persistence in repositories.
- Keep local development working without real Azure credentials.
- Prefer workflow and contract names such as `CveResearchWorkflow`, `TenantInvestigationWorkflow`, `AzureEvidenceCollector`, `DefenderEvidenceCollector`, `FindingCorrelator`, `ReportComposer`, `InvestigationContext`, `TenantFinding`, and `GlobalFindingReport` when they fit the change.

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
- Show findings, evidence, and workflow state as structured UI, not agent transcript output.

## Future boundaries
- MCP support should stay easy to add as a thin layer.
- ServiceNow and Slack remain optional later adapters or trigger surfaces.
- No premature microservice split.

## Common commands
- Build backend: `dotnet build`
- Test backend: `dotnet test tests/Api.Tests/`
- Build frontend: `cd /home/runner/work/Trace/Trace/web/app && npm run build`
- Test frontend: `cd /home/runner/work/Trace/Trace/web/app && npm test`
- Run full app locally: `cd /home/runner/work/Trace/Trace/src/AppHost && dotnet run`

## Tool usage policy

Prefer available built-in tools, MCP tools, and IDE-integrated capabilities over ad hoc shell commands.

When deciding how to gather information or make changes, use this priority order:
1. Official documentation tools / MCP servers
2. Built-in IDE or agent tools for search, edits, refactors, diagnostics, and testing
3. Repository source code and tests
4. Command-line tools only when no better tool exists

Do not default to shell commands for tasks that are already supported by a built-in tool.
Examples:
- Prefer documentation search/fetch tools over grepping package caches
- Prefer built-in file edit / patch capabilities over `sed`, `awk`, `perl`, Python rewrite scripts, or manual here-doc file rewrites
- Prefer IDE/project search tools over broad recursive grep when possible
- Prefer built-in rename/refactor/navigation tools over text substitution when symbol-aware operations are available
- Prefer configured test/debug/build tasks over handcrafted shell pipelines when equivalent tasks already exist

## Editing policy

When changing files:
1. Make the smallest targeted edit possible
2. Use tool-based patching/editing instead of shell-based text manipulation
3. Preserve formatting and surrounding code style
4. Avoid rewriting entire files for small changes
5. After edits, run the most relevant validation step available

Never use shell commands to edit files if a built-in editing or patch tool can perform the change more safely and precisely.

Avoid:
- `sed -i`
- `perl -pi`
- Python scripts that rewrite files for small edits
- `cat > file` / here-doc rewrites for existing files unless a full replacement is truly intended

## Research policy

Before implementing code that depends on a library, framework, SDK, API, or cloud platform:
1. Research authoritative sources first
2. Summarize the sources used
3. Note version-specific caveats
4. Only inspect local caches or generated artifacts if authoritative docs are missing or contradictory

Do not use local package caches, XML docs, `obj/`, `bin/`, or decompiled output as the first source of truth.

## Reasoning policy for tools

Before using the command line, ask:
- Is there already a built-in tool for this?
- Is there an MCP server for this?
- Is there a safer symbol-aware or structure-aware way to do this?
- Is this shell step actually necessary, or just familiar?

If a safer higher-level tool exists, use it instead of the command line.

## Transparency

When choosing a lower-priority approach such as manual shell inspection or cache digging, explain why the preferred tools were not sufficient.
Do not silently fall back to brittle command-line manipulation.

## Efficiency guardrails

Do not spend multiple steps manually digging through local caches, generated files, or package internals before trying authoritative documentation or configured MCP tools.

Stop and switch approach if the current method is low-signal, brittle, or clearly inferior to an available tool.

## Todo policy

When Todos have been listed out in the plan mode prior to the ongoing implementation, make sure to tick them off as you finish them.