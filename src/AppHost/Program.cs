// Trace AppHost – Aspire orchestration entry point.
// Wires up the API, web frontend, and Cosmos DB for local development.
//
// Cosmos DB notes:
//   - In local dev (macOS/Linux), the Cosmos Linux-based preview emulator is used.
//     It is started automatically by Aspire when running locally.
//   - The emulator exposes a Data Explorer at https://localhost:1234/_explorer/index.html
//   - Cloud resources are NOT required for local startup.
//   - To switch to a real Azure Cosmos account, set the connection string in
//     user secrets or environment variables and remove the emulator configuration.

var builder = DistributedApplication.CreateBuilder(args);

// ── Cosmos DB ──────────────────────────────────────────────────────────────────
// Local dev: use the Azure Cosmos DB Linux emulator (preview) via Aspire.
// The emulator is only started when no external connection string is provided.
// Production: provide "ConnectionStrings:cosmos" via environment / Key Vault.
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(emulator =>
    {
        // WithDataExplorer is a preview API (ASPIRECOSMOSDB001).
        // Suppressed here intentionally – it is useful for local dev and the risk is
        // low (a local-only tool endpoint). Remove if the preview API is removed in
        // a future Aspire release.
#pragma warning disable ASPIRECOSMOSDB001
        emulator.WithDataExplorer();
#pragma warning restore ASPIRECOSMOSDB001
    });

var traceDb = cosmos.AddCosmosDatabase("tracedb");

// ── API ────────────────────────────────────────────────────────────────────────
var api = builder.AddProject<Projects.Trace_Api>("api")
    .WithReference(traceDb)
    .WaitFor(traceDb);

// ── Web frontend ───────────────────────────────────────────────────────────────
// The React app is served via Vite dev server during local development.
// In production, it should be built and served statically or via a CDN.
// TODO (Teams tab): When adding Teams tab support, the same web app can be
//   hosted inside a Teams tab with minimal changes. See ARCHITECTURE.md.
var web = builder.AddNpmApp("web", "../../web/app", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT", port: 3000)
    .PublishAsDockerFile();

builder.Build().Run();
