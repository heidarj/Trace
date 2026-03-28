using Microsoft.Azure.Cosmos;
using Trace.Contracts;

namespace Trace.Api.Repositories;

public class WorkQueueRepository : IWorkQueueRepository
{
    private readonly CosmosClient _client;
    private readonly ILogger<WorkQueueRepository> _logger;
    private Container? _container;

    private const string DatabaseId = "tracedb";
    private const string ContainerName = "WorkQueue";

    public WorkQueueRepository(CosmosClient client, ILogger<WorkQueueRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    private Container Container =>
        _container ??= _client.GetContainer(DatabaseId, ContainerName);

    public async Task<WorkQueueItem> CreateAsync(WorkQueueItem item, CancellationToken ct = default)
    {
        var document = ToDocument(item);
        var response = await Container.CreateItemAsync(document, new PartitionKey(item.RunId), cancellationToken: ct);
        return FromDocument(response.Resource);
    }

    public async Task<WorkQueueItem?> GetAsync(string runId, string id, CancellationToken ct = default)
    {
        try
        {
            var response = await Container.ReadItemAsync<WorkQueueItemDocument>(id, new PartitionKey(runId), cancellationToken: ct);
            return FromDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<WorkQueueItem>> ListByRunAsync(string runId, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.runId = @runId AND c.documentType = 'workItem'")
            .WithParameter("@runId", runId);

        var results = new List<WorkQueueItem>();
        using var feed = Container.GetItemQueryIterator<WorkQueueItemDocument>(query, requestOptions: new() { PartitionKey = new PartitionKey(runId) });
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            results.AddRange(page.Select(FromDocument));
        }

        return results;
    }

    public async Task<IReadOnlyList<WorkQueueItem>> ListPendingOrExpiredAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var query = new QueryDefinition(@"
SELECT * FROM c
WHERE c.documentType = 'workItem'
  AND (
    c.status = 'Pending'
    OR (c.status = 'Running' AND (NOT IS_DEFINED(c.leaseExpiresAt) OR IS_NULL(c.leaseExpiresAt) OR c.leaseExpiresAt < @now))
  )")
            .WithParameter("@now", now);

        var results = new List<WorkQueueItem>();
        using var feed = Container.GetItemQueryIterator<WorkQueueItemDocument>(query);
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            results.AddRange(page.Select(FromDocument));
        }

        return results;
    }

    public async Task<WorkQueueItem?> TryLeaseAsync(string runId, string id, string workerId, DateTimeOffset leaseExpiresAt, CancellationToken ct = default)
    {
        try
        {
            var existing = await Container.ReadItemAsync<WorkQueueItemDocument>(id, new PartitionKey(runId), cancellationToken: ct);
            var document = existing.Resource;
            var now = DateTimeOffset.UtcNow;

            if (document.Status is nameof(WorkItemStatus.Completed) or nameof(WorkItemStatus.Failed))
            {
                return null;
            }

            if (document.Status == nameof(WorkItemStatus.Running)
                && document.LeaseExpiresAt.HasValue
                && document.LeaseExpiresAt.Value > now)
            {
                return null;
            }

            document.Status = WorkItemStatus.Running.ToString();
            document.StartedAt ??= now;
            document.LeaseExpiresAt = leaseExpiresAt;
            document.WorkerId = workerId;
            document.AttemptCount += 1;
            document.LastUpdatedAt = now;

            var options = new ItemRequestOptions { IfMatchEtag = existing.Resource.ETag };
            var response = await Container.ReplaceItemAsync(document, document.Id, new PartitionKey(runId), options, ct);
            return FromDocument(response.Resource);
        }
        catch (CosmosException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.NotFound
            || ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed
            || ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return null;
        }
    }

    public async Task<WorkQueueItem> UpdateAsync(WorkQueueItem item, CancellationToken ct = default)
    {
        var existing = await Container.ReadItemAsync<WorkQueueItemDocument>(item.Id, new PartitionKey(item.RunId), cancellationToken: ct);
        var document = ToDocument(item);
        document.ETag = existing.Resource.ETag;

        var options = new ItemRequestOptions { IfMatchEtag = document.ETag };
        var response = await Container.ReplaceItemAsync(document, document.Id, new PartitionKey(item.RunId), options, ct);
        return FromDocument(response.Resource);
    }

    private static WorkQueueItemDocument ToDocument(WorkQueueItem item) => new()
    {
        Id = item.Id,
        RunId = item.RunId,
        WorkType = item.WorkType.ToString(),
        Status = item.Status.ToString(),
        CreatedAt = item.CreatedAt,
        TenantId = item.TenantId,
        ParentWorkItemId = item.ParentWorkItemId,
        StartedAt = item.StartedAt,
        CompletedAt = item.CompletedAt,
        LeaseExpiresAt = item.LeaseExpiresAt,
        WorkerId = item.WorkerId,
        AttemptCount = item.AttemptCount,
        ProgressMessage = item.ProgressMessage,
        ErrorMessage = item.ErrorMessage,
        LastUpdatedAt = item.LastUpdatedAt
    };

    private static WorkQueueItem FromDocument(WorkQueueItemDocument document) => new(
        document.Id,
        document.RunId,
        Enum.Parse<WorkItemType>(document.WorkType),
        Enum.Parse<WorkItemStatus>(document.Status),
        document.CreatedAt,
        document.TenantId,
        document.ParentWorkItemId,
        document.StartedAt,
        document.CompletedAt,
        document.LeaseExpiresAt,
        document.WorkerId,
        document.AttemptCount,
        document.ProgressMessage,
        document.ErrorMessage,
        document.LastUpdatedAt
    );
}

internal class WorkQueueItemDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "workItem";
    public string RunId { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? TenantId { get; set; }
    public string? ParentWorkItemId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public string? WorkerId { get; set; }
    public int AttemptCount { get; set; }
    public string? ProgressMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public string? ETag { get; set; }
}