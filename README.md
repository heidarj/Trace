# Trace — CVE Investigation Platform

**T**enant **R**isk **A**nalysis for **C**VEs &amp; **E**xposure

Trace is a web-first Azure vulnerability investigation platform. Operators submit a CVE ID, kick off an investigation workflow across many customer tenants/subscriptions, persist findings, review results, and create downstream ticket recommendations.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
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
| Orchestration | .NET Aspire 9 |
| Frontend | React 19 + TypeScript + Fluent UI React v9 |
| Database | Azure Cosmos DB for NoSQL |
| Local DB | Cosmos DB Linux emulator (via Aspire) |
| Auth | Microsoft Entra ID / OIDC |
| Tests | xUnit (backend) + Vitest (frontend) |
| Logging | OpenTelemetry with structured logging |

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed design notes on Cosmos persistence, Teams support, MCP tools, and future evolution.

---

## Project Structure

```
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
│       └── Services/            # Business logic
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

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com) |
| Node.js | 20+ | [nodejs.org](https://nodejs.org) |
| npm | 10+ | Comes with Node.js |
| Docker | latest | Required for Cosmos DB emulator |
| .NET Aspire workload | 9.x | `dotnet workload install aspire` |

**Note:** Docker is only required for the local Cosmos DB emulator. If you have a real Azure Cosmos DB account, you can skip Docker and configure the connection string instead.

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

### 3. Install .NET Aspire workload (if not already installed)

```bash
dotnet workload install aspire
```

### 4. (Optional) Configure authentication

For local development, authentication is **not required** — the API runs without it and serves all endpoints.

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
cd src/AppHost
dotnet run
```

Aspire will:
- Start the Cosmos DB Linux emulator (Docker required)
- Start the .NET API
- Start the Vite dev server for the frontend
- Open the Aspire dashboard at `https://localhost:15888`

The Cosmos Data Explorer will be available at `https://localhost:1234/_explorer/index.html`.

### Option B: Run API and web separately (no Docker)

**Start the API:**

```bash
# Set a fallback Cosmos connection string (emulator default key)
cd src/Api
dotnet run
```

The API will use the default Cosmos emulator connection (`https://localhost:8081`) if no connection string is configured.

**Start the frontend:**

```bash
cd web/app
npm run dev
```

The web app will be available at `http://localhost:3000`.
The API Swagger UI will be available at `https://localhost:7001/swagger`.

### Seed data

On first startup, the API automatically seeds fake investigation data so the UI looks useful immediately. Seeding is idempotent — it only runs when the database is empty.

---

## API Reference

Base URL: `https://localhost:7001/api` (local dev)

| Method | Path | Description |
|---|---|---|
| POST | `/investigations` | Start a new CVE investigation |
| GET | `/investigations` | List recent investigation runs |
| GET | `/investigations/{runId}` | Get run details |
| GET | `/investigations/{runId}/tenants` | Get all tenant results for a run |
| GET | `/investigations/{runId}/tenants/{tenantId}` | Get a single tenant result |
| POST | `/investigations/{runId}/tenants/{tenantId}/review` | Approve or reject a result |
| GET | `/tickets` | List ticket recommendations |
| POST | `/tickets` | Create a ticket recommendation from an approved result |

Full Swagger documentation available at `/swagger` when running locally.

---

## Authentication

Authentication is **optional for local development**. When `AzureAd:TenantId` is not configured, the API skips token validation and accepts all requests.

In production:
1. Register the API app in Microsoft Entra ID
2. Configure `AzureAd:TenantId` and `AzureAd:ClientId`
3. The frontend should use MSAL.js to acquire tokens and pass them as `Authorization: Bearer <token>`

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full auth design, including Teams SSO and MCP compatibility.

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

### All tests

```bash
# From repo root
dotnet test tests/Api.Tests/ && cd web/app && npm test
```
