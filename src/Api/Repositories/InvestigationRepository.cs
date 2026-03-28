using Microsoft.Azure.Cosmos;
using Trace.Contracts;

namespace Trace.Api.Repositories;

public class InvestigationRepository : IInvestigationRepository
{
    private readonly CosmosClient _client;
    private readonly ILogger<InvestigationRepository> _logger;
    private Container? _container;

    private const string DatabaseId = "tracedb";
    private const string ContainerName = "InvestigationRuns";

    public InvestigationRepository(CosmosClient client, ILogger<InvestigationRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    private Container Container =>
        _container ??= _client.GetContainer(DatabaseId, ContainerName);

    public async Task<InvestigationRun> CreateAsync(InvestigationRun run, CancellationToken ct = default)
    {
        var doc = ToDocument(run);
        var response = await Container.CreateItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
        return FromDocument(response.Resource);
    }

    public async Task<InvestigationRun?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await Container.ReadItemAsync<InvestigationRunDocument>(id, new PartitionKey(id), cancellationToken: ct);
            return FromDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<InvestigationRun>> ListRecentAsync(int limit = 20, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT TOP @limit * FROM c ORDER BY c._ts DESC")
            .WithParameter("@limit", limit);

        var results = new List<InvestigationRun>();
        using var feed = Container.GetItemQueryIterator<InvestigationRunDocument>(query);
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            results.AddRange(page.Select(FromDocument));
        }
        return results;
    }

    public async Task<InvestigationRun> UpdateAsync(InvestigationRun run, CancellationToken ct = default)
    {
        var doc = ToDocument(run);
        var response = await Container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Id), cancellationToken: ct);
        return FromDocument(response.Resource);
    }

    private static InvestigationRunDocument ToDocument(InvestigationRun run) => new()
    {
        Id = run.Id,
        CveId = run.CveId,
        Title = run.Title,
        Description = run.Description,
        Status = run.Status.ToString(),
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        TotalTenants = run.TotalTenants,
        TenantsCompleted = run.TenantsCompleted,
        FindingsCount = run.FindingsCount,
        CreatedBy = run.CreatedBy
    };

    private static InvestigationRun FromDocument(InvestigationRunDocument doc) => new(
        doc.Id,
        doc.CveId,
        doc.Title,
        doc.Description,
        Enum.Parse<InvestigationStatus>(doc.Status),
        doc.StartedAt,
        doc.CompletedAt,
        doc.TotalTenants,
        doc.TenantsCompleted,
        doc.FindingsCount,
        doc.CreatedBy
    );
}

internal class InvestigationRunDocument
{
    public string Id { get; set; } = string.Empty;
    public string CveId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int TotalTenants { get; set; }
    public int TenantsCompleted { get; set; }
    public int FindingsCount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
