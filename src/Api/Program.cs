using Trace.Api.Endpoints;
using Trace.Api.Repositories;
using Trace.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register CosmosClient - use Aspire integration if available, otherwise fall back to direct connection
var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmos");
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton(new Microsoft.Azure.Cosmos.CosmosClient(cosmosConnectionString));
}
else
{
    // Local dev fallback: connects to the Cosmos DB emulator on localhost.
    // The emulator uses a self-signed certificate and a well-known public key,
    // so DangerousAcceptAnyServerCertificateValidator is intentional here.
    // This branch is only reached when no "cosmos" connection string is configured
    // (i.e., never in production, where Aspire injects the real connection string).
    builder.Services.AddSingleton(new Microsoft.Azure.Cosmos.CosmosClient(
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b5UZy208fP/p+KFsVL0kAMVKHh4Zh==;",
        new Microsoft.Azure.Cosmos.CosmosClientOptions { HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }) }));
}

builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IInvestigationRepository, InvestigationRepository>();
builder.Services.AddSingleton<ITenantResultRepository, TenantResultRepository>();
builder.Services.AddSingleton<IInvestigationService, InvestigationService>();
builder.Services.AddSingleton<ITicketService, TicketService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

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

app.UseCors();
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
