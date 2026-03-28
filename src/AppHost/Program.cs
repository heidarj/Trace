var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataExplorer();
    });

var traceDb = cosmos.AddCosmosDatabase("tracedb");

var api = builder.AddProject<Projects.Trace_Api>("api")
    .WithReference(traceDb)
    .WaitFor(traceDb);

var web = builder.AddNpmApp("web", "../../web/app", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT", port: 3000)
    .PublishAsDockerFile();

builder.Build().Run();
