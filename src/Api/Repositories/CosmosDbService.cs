using Microsoft.Azure.Cosmos;

namespace Trace.Api.Repositories;

public class CosmosDbService : ICosmosDbService
{
    private readonly CosmosClient _client;
    private readonly ILogger<CosmosDbService> _logger;
    private const string DatabaseId = "tracedb";
    private const string InvestigationRunsContainer = "InvestigationRuns";
    private const string RunTenantDataContainer = "RunTenantData";
    private const string WorkQueueContainer = "WorkQueue";

    public CosmosDbService(CosmosClient client, ILogger<CosmosDbService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Cosmos DB database and containers...");

        var dbResponse = await _client.CreateDatabaseIfNotExistsAsync(DatabaseId, cancellationToken: cancellationToken);
        var db = dbResponse.Database;

        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(InvestigationRunsContainer, "/id"),
            cancellationToken: cancellationToken);

        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(RunTenantDataContainer, "/runId"),
            cancellationToken: cancellationToken);

        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(WorkQueueContainer, "/runId"),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Cosmos DB initialization complete.");
    }
}
