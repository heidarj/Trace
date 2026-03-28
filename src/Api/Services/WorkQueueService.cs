using Microsoft.Azure.Cosmos;
using Trace.Api.Repositories;
using Trace.Contracts;

namespace Trace.Api.Services;

public class WorkQueueService : IWorkQueueService
{
    private static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(15);

    private readonly IWorkQueueRepository _repository;

    public WorkQueueService(IWorkQueueRepository repository)
    {
        _repository = repository;
    }

    public async Task<WorkQueueItem> QueueResearchAsync(string runId, CancellationToken ct = default) =>
        await CreateIfMissingAsync(new WorkQueueItem(
            ResearchWorkId,
            runId,
            WorkItemType.Research,
            WorkItemStatus.Pending,
            DateTimeOffset.UtcNow,
            ProgressMessage: "Queued CVE research.",
            LastUpdatedAt: DateTimeOffset.UtcNow),
            ct);

    public async Task<WorkQueueItem> QueueTenantInvestigationAsync(string runId, string tenantId, string parentWorkItemId, CancellationToken ct = default) =>
        await CreateIfMissingAsync(new WorkQueueItem(
            TenantWorkId(tenantId),
            runId,
            WorkItemType.TenantInvestigation,
            WorkItemStatus.Pending,
            DateTimeOffset.UtcNow,
            TenantId: tenantId,
            ParentWorkItemId: parentWorkItemId,
            ProgressMessage: "Queued tenant investigation.",
            LastUpdatedAt: DateTimeOffset.UtcNow),
            ct);

    public async Task<WorkQueueItem?> EnsureAggregationQueuedAsync(string runId, string parentWorkItemId, CancellationToken ct = default)
    {
        var existing = await _repository.GetAsync(runId, AggregationWorkId, ct);
        if (existing is not null)
        {
            return existing.Status == WorkItemStatus.Pending ? existing : null;
        }

        var workItems = await _repository.ListByRunAsync(runId, ct);
        var tenantItems = workItems.Where(item => item.WorkType == WorkItemType.TenantInvestigation).ToList();
        if (tenantItems.Count == 0)
        {
            return null;
        }

        if (tenantItems.Any(item => item.Status is WorkItemStatus.Pending or WorkItemStatus.Running))
        {
            return null;
        }

        return await CreateIfMissingAsync(new WorkQueueItem(
            AggregationWorkId,
            runId,
            WorkItemType.Aggregation,
            WorkItemStatus.Pending,
            DateTimeOffset.UtcNow,
            ParentWorkItemId: parentWorkItemId,
            ProgressMessage: "Queued aggregation.",
            LastUpdatedAt: DateTimeOffset.UtcNow),
            ct);
    }

    public async Task<WorkQueueItem?> GetAsync(string runId, string workItemId, CancellationToken ct = default) =>
        await _repository.GetAsync(runId, workItemId, ct);

    public async Task<IReadOnlyList<WorkQueueItem>> ListByRunAsync(string runId, CancellationToken ct = default) =>
        await _repository.ListByRunAsync(runId, ct);

    public async Task<IReadOnlyList<WorkQueueItem>> ListRecoverableAsync(CancellationToken ct = default) =>
        await _repository.ListPendingOrExpiredAsync(DateTimeOffset.UtcNow, ct);

    public async Task<WorkQueueItem?> TryStartAsync(string runId, string workItemId, string workerId, CancellationToken ct = default) =>
        await _repository.TryLeaseAsync(runId, workItemId, workerId, DateTimeOffset.UtcNow.Add(DefaultLeaseDuration), ct);

    public async Task<WorkQueueItem> MarkProgressAsync(string runId, string workItemId, string progressMessage, CancellationToken ct = default)
    {
        var existing = await GetRequiredAsync(runId, workItemId, ct);
        return await _repository.UpdateAsync(existing with
        {
            ProgressMessage = progressMessage,
            LastUpdatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    public async Task<WorkQueueItem> MarkCompletedAsync(string runId, string workItemId, string progressMessage, CancellationToken ct = default)
    {
        var existing = await GetRequiredAsync(runId, workItemId, ct);
        return await _repository.UpdateAsync(existing with
        {
            Status = WorkItemStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            LeaseExpiresAt = null,
            WorkerId = null,
            ProgressMessage = progressMessage,
            LastUpdatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    public async Task<WorkQueueItem> MarkFailedAsync(string runId, string workItemId, string errorMessage, CancellationToken ct = default)
    {
        var existing = await GetRequiredAsync(runId, workItemId, ct);
        return await _repository.UpdateAsync(existing with
        {
            Status = WorkItemStatus.Failed,
            CompletedAt = DateTimeOffset.UtcNow,
            LeaseExpiresAt = null,
            WorkerId = null,
            ErrorMessage = errorMessage,
            ProgressMessage = errorMessage,
            LastUpdatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    private async Task<WorkQueueItem> CreateIfMissingAsync(WorkQueueItem item, CancellationToken ct)
    {
        var existing = await _repository.GetAsync(item.RunId, item.Id, ct);
        if (existing is not null)
        {
            return existing;
        }

        try
        {
            return await _repository.CreateAsync(item, ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return await GetRequiredAsync(item.RunId, item.Id, ct);
        }
    }

    private async Task<WorkQueueItem> GetRequiredAsync(string runId, string workItemId, CancellationToken ct)
    {
        return await _repository.GetAsync(runId, workItemId, ct)
            ?? throw new KeyNotFoundException($"Work item not found: {runId}/{workItemId}");
    }

    private const string ResearchWorkId = "research";
    private const string AggregationWorkId = "aggregation";

    private static string TenantWorkId(string tenantId) => $"tenant:{tenantId}";
}