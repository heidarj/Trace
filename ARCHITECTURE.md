# Trace — Architecture Notes

This document explains the key design decisions in Trace and how the platform is intended to evolve.

---

## 1. Why the repo is intentionally simple

Trace is meant to stay lean while the core workflow is proven out. The structure reflects what is actually needed now:

| Project | Why it exists |
|---|---|
| `Trace.Contracts` | Shared strongly typed DTOs eliminate duplication between API, tests, and future workflow stages. |
| `Trace.ServiceDefaults` | Aspire convention: one place to configure OpenTelemetry, health checks, and service discovery. |
| `Trace.AppHost` | Aspire convention: the composition root wiring services, infrastructure, and dev tooling together. |
| `Trace.Api` | The single deployable backend and the place where workflow seams start. |
| `web/app` | The single deployable frontend and primary operator experience. |
| `tests/Api.Tests` | Unit tests for business logic and workflow orchestration seams. |

**What is intentionally not added yet:**
- No speculative microservice split.
- No extra abstraction layers without a real use case.
- No dead stub files just to advertise future plans.

---

## 2. Investigation orchestration with Microsoft Agent Framework

The intended execution model is Agent Framework-backed and follows a clean three-stage split:

1. **Research and enrichment first**

	The API starts a `CveResearchWorkflow` or equivalent research agent stage. That stage gathers CVE intelligence and produces a typed `InvestigationContext`.

2. **Tenant-scoped investigation fan-out second**

	One `TenantInvestigationWorkflow(tenantId, context)` runs per onboarded tenant. Those workflows should execute concurrently when appropriate and remain isolated from each other.

3. **Correlation and reporting last**

	Tenant-local evidence is correlated into findings and then aggregated into a single report for the web UI and future adapter surfaces.

Agent Framework is a good fit because it provides the orchestration shapes this workflow needs:

- Sequential execution for the upfront CVE research stage
- Concurrent execution for tenant fan-out
- Checkpointing when investigations need to be resumable or long-running

Preferred logical components:

- `CveResearchWorkflow`
- `TenantInvestigationWorkflow`
- `AzureEvidenceCollector`
- `DefenderEvidenceCollector`
- `FindingCorrelator`
- `ReportComposer`

The research stage may use LLM reasoning where it helps, but the handoff into tenant workflows must be a typed payload, not freeform prompt text.

---

## 3. The core contract: InvestigationContext

The most important contract in the orchestration model is `InvestigationContext`. It is the normalized, structured output of CVE research and the typed input to every downstream tenant workflow.

At minimum, it should carry:

- Normalized CVE IDs
- Affected products
- Version ranges
- KB or fix data
- Exploitation status
- Detection hints
- The queries, filters, or selectors each collector should run

This is the key architectural rule: downstream workflows should consume structured context, not "whatever the model said last time." The LLM can help build the context, but findings and verdicts should still come from typed evidence collection and correlation.

When workflow-specific reporting contracts are introduced, prefer names such as `TenantFinding` and `GlobalFindingReport` for correlated tenant output and final aggregation.

---

## 4. Azure and Defender evidence model

Trace should treat Azure control-plane evidence and Defender evidence as distinct collection paths.

### Azure evidence

`AzureEvidenceCollector` should use Azure Lighthouse plus ARM-compatible APIs such as Azure Resource Graph for cross-tenant inventory and posture evidence:

- Subscription and resource inventory
- Defender for Cloud posture metadata exposed through ARM surfaces
- Arc inventory
- Tags, image metadata, extension state, and similar management-plane evidence

Azure Lighthouse is appropriate here because it is designed for ARM-supported cross-tenant management. It should not be treated as a universal auth story for all external systems.

### Defender evidence

`DefenderEvidenceCollector` should use its own auth path:

- Register a multitenant Entra app
- Obtain admin consent from each customer tenant
- Acquire a tenant-specific token for that customer tenant
- Call Defender for Endpoint, TVM, or XDR APIs in that tenant context

Useful early endpoints are the ones that directly answer vulnerability exposure questions, such as:

- Devices by vulnerability
- Vulnerable software or version listings
- Software inventory endpoints

This usually gives stronger evidence than trying to infer everything from ARM metadata.

---

## 5. Cosmos DB persistence design

### Containers

**`InvestigationRuns`** — partition key `/id`

One document per investigation run. Optimized for:
- Recent-run landing page views
- Summary cards and progress snapshots
- Direct point reads by run ID

**`RunTenantData`** — partition key `/runId`

Stores multiple document types within a single run partition via a `documentType` discriminator:

| `documentType` | Description |
|---|---|
| `tenantSummary` | One per tenant per run. Aggregate verdict, review status, findings count. |
| `finding` | One per finding. Resource ID, type, evidence, confidence, and source metadata as needed. |
| `evidence` | Optional separate evidence artifact when embedding is not enough. |
| `review` | Review decisions or review history. |
| `ticket` | Outbound ticket recommendation created from an approved result. |

This design keeps run-scoped reads efficient without creating a separate container per record type.

**Optimistic concurrency:** Review and approval actions on `tenantSummary` documents should keep using ETag-based optimistic concurrency to prevent lost updates.

### Hot query paths

| View | Query pattern | Container |
|---|---|---|
| Landing page (recent runs) | `SELECT TOP N * FROM c ORDER BY c._ts DESC` | `InvestigationRuns` |
| Run details (tenant list) | `WHERE c.runId = @runId AND c.documentType = 'tenantSummary'` | `RunTenantData` |
| Tenant result detail | Point read by `runId:tenantId:summary` | `RunTenantData` |
| Findings for tenant | `WHERE c.runId = @runId AND c.tenantId = @tenantId AND c.documentType = 'finding'` | `RunTenantData` |
| Ticket list | `WHERE c.documentType = 'ticket' ORDER BY c._ts DESC` | `RunTenantData` |

### Future projections

These patterns may later justify projections or indexes, but they are not required for the local-dev path:

- Month-based landing page summaries
- By-CVE summary views
- Cross-run review queue projections
- A dedicated ticket queue if ticket volume warrants it

---

## 6. Auth design

There are three separate auth concerns in Trace.

### Browser auth

The web app uses Microsoft Entra ID and MSAL.js to acquire a bearer token for the API. The API validates that token with `JwtBearer` middleware.

### Azure control-plane auth

Azure Lighthouse plus ARM-compatible permissions provide the cross-tenant management-plane access used by Azure evidence collectors.

### Defender API auth

Defender evidence collection uses a multitenant Entra app with customer-tenant consent and tenant-specific tokens. This is separate from Lighthouse and should remain modeled separately in code.

The API and workflow layer should be structured so these auth surfaces can coexist without leaking one model into another.

---

## 7. Teams tab support (future)

The web app remains a standard browser-first React app. Teams is a later host surface.

Adding Teams support later should require:

1. A Teams app manifest with a hosted tab target
2. Teams SDK initialization in the frontend shell
3. Swapping browser OIDC acquisition for Teams SSO token acquisition and on-behalf-of exchange
4. Optional theme synchronization

Do not add a Teams-first shell, routing model, or manifest-heavy setup now.

---

## 8. MCP tool surface (future)

MCP is still a future thin surface, but the architecture should make it easy to expose workflow entry points later.

Likely MCP tools map naturally to:

- `start_cve_investigation`
- `get_investigation_status`
- `list_recent_investigations`
- `get_tenant_result`

As Agent Framework orchestration is introduced, the MCP layer should remain a thin wrapper around the same application services or workflow entry points rather than a separate implementation.

---

## 9. Ticketing and adapter surfaces

The internal `TicketService` should own the local `TicketRecommendation` record. ServiceNow and Slack are still later adapters or trigger surfaces, not core dependencies.

The longer-term execution model is:

1. Trigger from the web app first
2. Optionally trigger later from Slack or ServiceNow
3. Run research, tenant fan-out, and aggregation
4. Return a composed result to the web UI and later external adapters

That keeps the product centered on one workflow engine with multiple entry and exit points.

---

## 10. Workflow philosophy

Trace should use agent-led orchestration without turning the core investigation into unstructured chat output.

- Agent Framework owns sequencing, fan-out, and checkpointing.
- The research stage can use dynamic reasoning to normalize CVE data.
- Tenant workflows should consume typed `InvestigationContext` and produce typed evidence.
- Findings, verdicts, reviews, and ticket recommendations should remain structured and auditable.
- LLM-generated summaries are helpful, but they are not the source of truth for exposure status.

---

## 11. Logging and correlation

All services and workflow stages should use `ILogger<T>` with structured log properties. Correlation IDs should flow through OpenTelemetry trace context via `ServiceDefaults`. In production, logs can be exported to Azure Monitor or Application Insights through `OTEL_EXPORTER_OTLP_ENDPOINT`.
