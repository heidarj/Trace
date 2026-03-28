# Trace — CVE Investigation Platform

**T**enant **R**isk **A**nalysis for **C**VEs &amp; **E**xposure

Trace is a web-first Azure vulnerability investigation platform whose intended execution model is built around Microsoft Agent Framework. An operator submits a CVE ID, the API kicks off a workflow that researches and normalizes the CVE, fans out tenant-scoped evidence collection, persists typed findings, supports review, and produces ticket recommendations or downstream reports.

The current repository already contains the web app, API, Cosmos persistence, and seeded demo flows. The long-term product direction is for investigation execution itself to be Agent Framework-backed rather than a thin synchronous API stub.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Investigation Workflow](#investigation-workflow)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Codespaces and Devcontainer](#codespaces-and-devcontainer)
- [Local Setup](#local-setup)
- [Running the Application](#running-the-application)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Running Tests](#running-tests)
- [Architecture Notes](#architecture-notes)

---

## Architecture Overview

| Layer | Technology |
|---|---|
| Backend API | .NET 10 Minimal API |
| Investigation orchestration | Microsoft Agent Framework |
| Local composition | .NET Aspire 9 |
| Frontend | React 19 + TypeScript + Fluent UI React v9 |
| Database | Azure Cosmos DB for NoSQL |
| Local DB | Cosmos DB Linux emulator (via Aspire) |
| Web auth | Microsoft Entra ID / OIDC |
| Azure control-plane evidence | Azure Lighthouse + Azure Resource Graph |
| Defender evidence | Multitenant Entra app + tenant-scoped Defender API tokens |
| Tests | xUnit (backend) + Vitest (frontend) |
| Logging | OpenTelemetry with structured logging |

See [ARCHITECTURE.md](ARCHITECTURE.md) for the detailed design notes on Agent Framework orchestration, typed `InvestigationContext` handoffs, Cosmos persistence, Teams support, MCP tools, and future evolution.

---

## Investigation Workflow

Trace is designed around a three-stage investigation model:

1. **CVE research and enrichment**

  A research agent runs first and normalizes the submitted CVE into a typed `InvestigationContext`. That context should hold normalized CVE IDs, affected products, version ranges, KB or fix data, exploitation status, detection hints, and the queries or filters downstream collectors should run.

2. **Concurrent tenant investigations**

  One tenant-scoped workflow runs per onboarded tenant. Each `TenantInvestigationWorkflow(tenantId, context)` consumes the typed `InvestigationContext`, not prompt text, and gathers evidence independently.

3. **Correlation and reporting**

  Tenant-local evidence is correlated into findings, then aggregated into a final report that can feed the web UI now and later Slack, ServiceNow, Teams, or MCP surfaces.

Preferred logical components as the workflow layer is implemented:

- `CveResearchWorkflow`
- `TenantInvestigationWorkflow`
- `AzureEvidenceCollector`
- `DefenderEvidenceCollector`
- `FindingCorrelator`
- `ReportComposer`

Important design rules:

- Use Microsoft Agent Framework orchestration shapes deliberately: sequential research first, concurrent tenant fan-out second, and checkpointing when long-running investigations need resumability.
- Let the LLM help with research and normalization when useful, but make the collectors and correlators consume structured data and persist typed evidence.
- Do not treat Azure Lighthouse as a universal auth path. It is appropriate for ARM and control-plane collection, not for Defender data-plane APIs.

---

## Project Structure

```text
/
├── Trace.sln
├── Directory.Build.props        # Shared .NET build properties
├── src/
│   ├── Contracts/               # Trace.Contracts – shared DTOs and enums
│   ├── ServiceDefaults/         # Trace.ServiceDefaults – OpenTelemetry, health checks
│   ├── AppHost/                 # Trace.AppHost – Aspire orchestration host
│   └── Api/                     # Trace.Api – .NET 10 Web API
│       ├── Endpoints/           # Minimal API route groups
│       ├── Repositories/        # Cosmos DB data access
│       └── Services/            # Business logic and workflow orchestration seams
├── web/
│   └── app/                     # React + TypeScript + Fluent UI web app
│       └── src/
│           ├── api/             # Typed API client
│           ├── components/      # Shared UI components (badges, etc.)
│           ├── pages/           # Page components
│           └── types/           # TypeScript domain models
└── tests/
   └── Api.Tests/               # xUnit unit tests for the API
```

Keep the repo lean. When the Agent Framework layer is added, prefer fitting the workflow and collector components into the existing projects unless a real runtime boundary justifies a new project.

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com) |
| Node.js | 20+ | [nodejs.org](https://nodejs.org) |
| npm | 10+ | Comes with Node.js |
| Docker | latest | Optional. Only needed when you explicitly enable the Cosmos emulator path |

**Note:** Codespaces and the provided devcontainer are designed to use an existing Cosmos connection string by default. The emulator remains opt-in.

---

## Codespaces and Devcontainer

Use the AppHost as the default entry point in browser-based VS Code and Codespaces:

```bash
dotnet run --project src/AppHost/Trace.AppHost.csproj
```

Recommended Codespaces secrets:

- `ConnectionStrings__cosmos` - recommended and normally required in Codespaces. This can be a full Cosmos DB connection string or an account endpoint value supported by `AddConnectionString("cosmos")`.
- `AzureAd__TenantId` - optional for local auth testing.
- `AzureAd__ClientId` - optional for local auth testing.
- `AzureAd__ClientSecret` - optional for local auth testing.

Optional Codespaces secret:

- `UseContainerEmulators=true` - only set this when you have explicitly enabled a local container runtime and want to use the Cosmos emulator path.

Expected development ports:

- `18888` - Aspire dashboard frontend
- `5173` - React/Vite frontend
- `5000` - API HTTP
- `5001` - API HTTPS

The frontend proxies `/api` to the backend in development, so normal browser usage only needs the forwarded frontend URL on port `5173`.

Cosmos selection works like this:

1. If `ConnectionStrings__cosmos` is set, AppHost uses `AddConnectionString("cosmos")` and passes that reference to the API.
2. If no connection string is set and `UseContainerEmulators=true`, AppHost uses the Cosmos emulator path.
3. If neither is configured, AppHost fails fast with a startup message telling you exactly which setting to provide.

If AppHost reports missing `CliPath` or `DashboardPath`:

1. Re-run `dotnet restore` from the repository root.
2. Verify `src/AppHost/Trace.AppHost.csproj` includes the `Aspire.AppHost.Sdk` entry and still references `Aspire.Hosting.AppHost`.
3. Rebuild or reopen the devcontainer if the restore ran before the AppHost SDK entry existed.
4. On older local machines outside Codespaces, install or repair the Aspire workload if the .NET SDK is still missing Aspire runtime assets.

---

## Local Setup

### 1. Clone and restore

```bash
git clone https://github.com/heidarj/Trace.git
cd Trace
dotnet restore
```

### 2. Install frontend dependencies

```bash
cd web/app
npm install
cd ../..
```

### 3. (Optional) Configure authentication

For local development, authentication is **not required**. The API runs without token validation when Entra configuration is missing, and the happy path should remain usable without real Azure credentials.

For production, set these values in `src/Api/appsettings.json` or user secrets:

```json
{
  "AzureAd": {
   "TenantId": "your-tenant-id",
   "ClientId": "your-api-client-id"
  }
}
```

---

## Running the Application

### Option A: Via Aspire (recommended)

```bash
dotnet run --project src/AppHost/Trace.AppHost.csproj
```

Aspire will:
- Start the .NET API on `http://localhost:5000` and `https://localhost:5001`
- Start the Vite dev server on `http://localhost:5173`
- Expose the Aspire dashboard frontend on port `18888`
- Use `ConnectionStrings__cosmos` when it is configured
- Start the Cosmos emulator only when `UseContainerEmulators=true`

In normal development, use the frontend URL on port `5173`. The Vite dev server proxies `/api` requests to the backend API, so you do not need a separate browser origin for the API.

### Option B: Run API and web separately (no Docker)

**Start the API:**

```bash
export ConnectionStrings__cosmos="<your-cosmos-connection-string>"
cd src/Api
dotnet run
```

The API expects `ConnectionStrings__cosmos` when it is run directly.

**Start the frontend:**

```bash
cd web/app
npm run dev
```

The web app will be available at `http://localhost:5173`.
The API Swagger UI will be available at `https://localhost:5001/swagger`.

If you explicitly enable `UseContainerEmulators=true` and provide a local container runtime, the AppHost emulator path will also expose the Cosmos gateway on `https://localhost:8081`.

### Seed data

On first startup, the API automatically seeds fake investigation data so the UI looks useful immediately. Seeding is idempotent; it only runs when the database is empty.

That seed path exists to keep local development productive while the Agent Framework execution path is implemented. The intended end state is for `POST /investigations` to start the research workflow and tenant fan-out described above.

---

## API Reference

Base URL: `https://localhost:5001/api` (local dev)

| Method | Path | Description |
|---|---|---|
| POST | `/investigations` | Start a new CVE investigation and kick off the investigation workflow |
| GET | `/investigations` | List recent investigation runs |
| GET | `/investigations/{runId}` | Get run details |
| GET | `/investigations/{runId}/tenants` | Get all tenant results for a run |
| GET | `/investigations/{runId}/tenants/{tenantId}` | Get a single tenant result |
| POST | `/investigations/{runId}/tenants/{tenantId}/review` | Approve or reject a result |
| GET | `/tickets` | List ticket recommendations |
| POST | `/tickets` | Create a ticket recommendation from an approved result |

Full Swagger documentation is available at `/swagger` when running locally.

---

## Authentication

Authentication is **optional for local development**. When `AzureAd:TenantId` is not configured, the API skips token validation and accepts all requests.

The full design has three distinct auth concerns:

1. **Web app authentication**

  The browser client should use Entra ID and MSAL.js to acquire a bearer token for the API.

2. **Azure control-plane collection**

  Cross-tenant inventory and posture evidence should use Azure Lighthouse and ARM-compatible APIs such as Azure Resource Graph. This is the right path for subscription-scoped management data.

3. **Defender evidence collection**

  Defender for Endpoint, TVM, and XDR access should use a multitenant Entra app with customer admin consent and tenant-specific access tokens. Do not assume Lighthouse covers these APIs.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the fuller auth and orchestration design, including Teams SSO and future MCP compatibility.

---

## Running Tests

### Backend tests (xUnit)

```bash
dotnet test tests/Api.Tests/
```

### Frontend tests (Vitest)

```bash
cd web/app
npm test
```

### Frontend lint

```bash
cd web/app
npm run lint
```

### All tests

```bash
dotnet test tests/Api.Tests/ && cd web/app && npm test
```

---

## Architecture Notes

The short version:

- Keep the product web-first even as Slack, ServiceNow, Teams, and MCP become alternate trigger or report surfaces.
- Treat Microsoft Agent Framework as the core investigation engine.
- Pass a typed `InvestigationContext` from research into tenant workflows instead of raw prompt text.
- Keep findings evidence-based and strongly typed even when an LLM assists with research or summarization.
- Preserve the simple repo shape until a real runtime boundary requires something larger.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the detailed version.
