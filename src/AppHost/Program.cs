var builder = DistributedApplication.CreateBuilder(args);
var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmos");
var useContainerEmulators = builder.Configuration.GetValue("UseContainerEmulators", false);

var api = builder.AddProject<Projects.Trace_Api>("api");

if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    api.WithReference(builder.AddConnectionString("cosmos"));
}
else if (useContainerEmulators)
{
#pragma warning disable ASPIRECOSMOSDB001
    var cosmos = builder.AddAzureCosmosDB("cosmos")
        .RunAsPreviewEmulator(emulator =>
        {
            emulator.WithDataExplorer();
            emulator.WithGatewayPort(8081);
        });
#pragma warning restore ASPIRECOSMOSDB001

    api.WithReference(cosmos)
        .WaitFor(cosmos)
        .WithEnvironment("UseContainerEmulators", "true");
}
else
{
    throw new InvalidOperationException(
        "Trace AppHost requires Cosmos configuration. Set ConnectionStrings__cosmos to an existing Azure Cosmos account connection string or account endpoint, or set UseContainerEmulators=true to opt into the Cosmos emulator path.");
}

builder.AddNpmApp("web", "../../web/app", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("HOST", "0.0.0.0")
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT", port: 5173);

builder.Build().Run();
