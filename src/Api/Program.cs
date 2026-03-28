using Trace.Api.Endpoints;
using Trace.Api.Repositories;
using Trace.Api.Services;
using Trace.Api.Workers;
using Trace.Api.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmos");
if (string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    throw new InvalidOperationException(
        "Trace API requires ConnectionStrings__cosmos. Start the API through AppHost or set ConnectionStrings__cosmos explicitly before running the API directly.");
}

builder.AddAzureCosmosClient(
    "cosmos",
    configureClientOptions: clientOptions =>
    {
        if (ShouldTrustLocalCosmosCertificate(builder.Configuration, cosmosConnectionString))
        {
            clientOptions.HttpClientFactory = static () =>
                new HttpClient(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
        }
    });

builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IInvestigationRepository, InvestigationRepository>();
builder.Services.AddSingleton<ITenantResultRepository, TenantResultRepository>();
builder.Services.AddSingleton<IWorkQueueRepository, WorkQueueRepository>();
builder.Services.AddSingleton<IWorkDispatcher, ChannelWorkDispatcher>();
builder.Services.AddSingleton<IWorkQueueService, WorkQueueService>();
builder.Services.AddSingleton<IResearchModelClient, MockResearchModelClient>();
builder.Services.AddScoped<ICveResearchTool, RequestNormalizationResearchTool>();
builder.Services.AddScoped<ICveResearchTool, KeywordEnrichmentResearchTool>();
builder.Services.AddScoped<ICveResearchWorkflow, CveResearchWorkflow>();
builder.Services.AddScoped<IEvidenceCollector, AzureMockedCollector>();
builder.Services.AddScoped<IEvidenceCollector, DefenderMockedCollector>();
builder.Services.AddScoped<ITenantInvestigationWorkflow, TenantInvestigationWorkflow>();
builder.Services.AddScoped<IFindingCorrelator, FindingCorrelator>();
builder.Services.AddScoped<IWorkItemProcessor, WorkItemProcessor>();
builder.Services.AddSingleton<IInvestigationService, InvestigationService>();
builder.Services.AddSingleton<ITicketService, TicketService>();
builder.Services.AddHostedService<InvestigationBackgroundWorker>();

var entraConfig = builder.Configuration.GetSection("AzureAd");
if (entraConfig.Exists() && !string.IsNullOrWhiteSpace(entraConfig["TenantId"]))
{
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{entraConfig["TenantId"]}/v2.0";
            options.Audience = entraConfig["ClientId"];
        });
    builder.Services.AddAuthorization();
}
else
{
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Trace API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var cosmosService = scope.ServiceProvider.GetRequiredService<ICosmosDbService>();
    await cosmosService.InitializeAsync();

    var runRepo = scope.ServiceProvider.GetRequiredService<IInvestigationRepository>();
    var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantResultRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await SeedDataService.SeedAsync(runRepo, tenantRepo, logger);
}

app.MapInvestigationEndpoints();
app.MapTenantResultEndpoints();
app.MapTicketEndpoints();

app.Run();

public partial class Program { }

static bool ShouldTrustLocalCosmosCertificate(IConfiguration configuration, string cosmosConnectionString)
{
    if (configuration.GetValue("UseContainerEmulators", false))
    {
        return true;
    }

    return cosmosConnectionString.Contains("localhost:8081", StringComparison.OrdinalIgnoreCase)
        || cosmosConnectionString.Contains("127.0.0.1:8081", StringComparison.OrdinalIgnoreCase);
}
