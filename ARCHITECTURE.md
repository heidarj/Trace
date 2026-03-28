# Trace — Architecture Notes

This document explains the key design decisions in the Trace platform and how it can evolve.

---

## 1. Why the repo is intentionally simple

Trace v1 is a **starter**, not a finished product. The structure reflects what is actually needed now:

| Project | Why it exists |
|---|---|
| `Trace.Contracts` | Shared strongly-typed DTOs eliminate duplication between API and tests. |
| `Trace.ServiceDefaults` | Aspire convention: one place to configure OpenTelemetry, health checks, and service discovery for all services. |
| `Trace.AppHost` | Aspire convention: the composition root that wires services, infrastructure, and dev tooling together. |
| `Trace.Api` | The single deployable backend. No premature split into microservices. |
| `web/app` | The single deployable frontend. |
| `tests/Api.Tests` | Unit tests for business logic. Integration tests can be added here or in a separate project when needed. |

**What was intentionally NOT added:**
- No extra abstraction layers (no MediatR, no CQRS scaffolding) — the service/repository split is sufficient for v1.
- No separate projects for domain, application, infrastructure layers — one `Api` project is clean enough at this scale.
- No dead stub files — every file exists because it is used.

---

## 2. Cosmos DB persistence design

### Containers

**`InvestigationRuns`** — partition key `/id`

One document per investigation run. Optimized for:
- Recent-run landing page (ORDER BY `_ts DESC`)
- Summary views (small document, all summary fields present)
- Direct point reads by run ID

**`RunTenantData`** — partition key `/runId`

Stores multiple document types within a single partition per run, identified by a `documentType` discriminator:

| `documentType` | Description |
|---|---|
| `tenantSummary` | One per tenant per run. Aggregate verdict, review status, findings count. |
| `finding` | One per finding. Resource ID, type, evidence. |
| `evidence` | Optional separate evidence artifact (can also be embedded in finding). |
| `review` | Audit trail of review decisions (future: could append rather than update). |
| `ticket` | Outbound ticket recommendation created from an approved result. |

This design avoids creating a separate container per document type while still supporting efficient per-run fan-out queries (all documents for a run are in the same partition).

**Optimistic concurrency:** Review and approval actions on `tenantSummary` documents use ETag-based optimistic concurrency (`If-Match` header) to prevent lost updates when multiple operators act concurrently.

### Hot query paths

| View | Query pattern | Container |
|---|---|---|
| Landing page (recent runs) | `SELECT TOP N * FROM c ORDER BY c._ts DESC` | `InvestigationRuns` |
| Run details (tenant list) | `WHERE c.runId = @runId AND c.documentType = 'tenantSummary'` | `RunTenantData` |
| Tenant result detail | Point read by `runId:tenantId:summary` | `RunTenantData` |
| Findings for tenant | `WHERE c.runId = @runId AND c.tenantId = @tenantId AND c.documentType = 'finding'` | `RunTenantData` |
| Ticket list | `WHERE c.documentType = 'ticket' ORDER BY c._ts DESC` | `RunTenantData` (cross-partition) |

### Future query patterns and projections

The following patterns are expected to be needed in v2+ but are **not required for v1 local dev**:

- **Month-based landing page:** A composite index or Global Secondary Index (GSI) on `startedAt` month would serve this efficiently. In v1, the landing page uses recent-run ordering.

- **By-CVE summaries:** A GSI on `cveId` in `InvestigationRuns` would allow "all runs for CVE-2024-12345" without a cross-partition scan. Currently, this is handled by a client-side filter on the full recent-run list.

- **Review queue (cross-run pending reviews):** Currently computed by fan-out across recent runs. A projection or materialized view of pending-review records would improve scalability at high volume.

- **Ticket queue:** Currently a cross-partition scan on `RunTenantData`. A dedicated `Tickets` container becomes justified when the ticket volume warrants it.

**Preview GSI note:** Azure Cosmos DB hierarchical partition keys and composite partition keys are in preview. The current design does not require them, and they are not a hard dependency for local development.

---

## 3. Auth design

### v1: Web app OIDC

The web app uses **Microsoft Entra ID / OIDC** for authentication. The flow:

1. User navigates to the web app in a browser.
2. The frontend redirects to Entra for login (using MSAL.js — to be wired up in a future sprint).
3. The frontend passes the Entra access token to the API as a `Bearer` token.
4. The API validates the token using `JwtBearer` middleware.

### Designed for future extensibility

The auth surface is structured so the same backend can later support three flows **without re-architecture**:

| Flow | How |
|---|---|
| **Browser OIDC (web app)** | `JwtBearer` middleware + MSAL.js on the frontend. Already wired. |
| **Teams SSO (hosted tab)** | Teams provides an SSO token that can be exchanged for an Entra access token on-behalf-of. The API bearer validation remains identical. Only the frontend token acquisition changes. |
| **MCP OAuth surface** | The API services are designed as thin, composable units. A future MCP server can call `InvestigationService` methods directly and front the endpoint with an OAuth-compatible token validator. |

---

## 4. Teams tab support (future)

The web app is built as a **standard browser-first React app**. Adding Teams tab support later requires:

1. **Teams app manifest:** Add a manifest with a `staticTab` pointing to the web app URL.
2. **Teams JS SDK:** Wrap the app with `@microsoft/teams-js` and call `app.initialize()` on startup.
3. **Teams SSO:** Replace the MSAL OIDC redirect flow with `authentication.getAuthToken()` from the Teams SDK, then exchange for an Entra token on-behalf-of.
4. **Theme sync:** Optionally read `app.getContext()` to apply Teams light/dark/high-contrast theme.

The React component tree and routing are already Teams-compatible — no page restructuring is needed. The `FluentProvider` swap is a one-line change in `App.tsx`.

**What to NOT do now:** Do not add a Teams manifest, Teams-specific shell, or Teams-first routing. The comment in `App.tsx` marks the exact location where Teams initialization would go.

---

## 5. MCP tool surface (future)

The backend services (`InvestigationService`, `TicketService`) are shaped so a later MCP server can expose them as tools with minimal wrapping:

| MCP Tool | Backend method |
|---|---|
| `start_cve_investigation` | `InvestigationService.StartInvestigationAsync` |
| `get_investigation_status` | `InvestigationService.GetRunAsync` |
| `list_recent_investigations` | `InvestigationService.ListRecentRunsAsync` |
| `get_tenant_result` | `InvestigationService.GetTenantResultAsync` |

**Implementation path:**
1. Add a new `Trace.McpServer` project (or endpoint group in the existing API).
2. Register the existing services via DI.
3. Expose methods as MCP tool handlers.
4. Front with an OAuth-compatible token validator (APIM optional — not required for v1).

The services do not contain any HTTP-specific code, making them safe to reuse from an MCP context.

---

## 6. Ticketing abstraction

The `TicketService` creates internal `TicketRecommendation` records without coupling to any external system. To add ServiceNow or Jira support:

1. Define an `IExternalTicketProvider` interface with a `SubmitAsync(TicketRecommendation)` method.
2. Implement `ServiceNowTicketProvider` or `JiraTicketProvider`.
3. Inject into `TicketService` and call `SubmitAsync` after creating the local record.
4. Store the returned `externalTicketId` on the record.

The domain model (`TicketRecommendation`) has `ExternalTicketId` and `ExternalSystem` fields already included for this purpose.

---

## 7. Workflow philosophy

**v1:** Deterministic evidence collection and persistence.
- No LLM/agent features in the core finding flow.
- All findings are structured data with explicit types.
- The "investigation" in v1 is a scaffolding for real Azure Resource Graph / Defender collectors to be plugged in later.

**v2+ (future):**
- Azure Resource Graph collector → populates findings with real resource data.
- Microsoft Defender integration → populates evidence artifacts.
- Optional LLM enrichment → summarizes findings, suggests remediation (never used for core finding classification).
- Background processing → Durable Functions or Azure Service Bus for long-running scans.

---

## 8. Logging and correlation

All services use `ILogger<T>` with structured log properties. Correlation IDs flow through OpenTelemetry trace context, configured in `ServiceDefaults`. In production, logs should be routed to Azure Monitor / Application Insights by setting `OTEL_EXPORTER_OTLP_ENDPOINT`.
