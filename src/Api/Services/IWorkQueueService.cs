using Trace.Contracts;

namespace Trace.Api.Services;

public interface IWorkQueueService
{
    Task<WorkQueueItem> QueueResearchAsync(string runId, CancellationToken ct = default);
    Task<WorkQueueItem> QueueTenantInvestigationAsync(string runId, string tenantId, string parentWorkItemId, CancellationToken ct = default);
    Task<WorkQueueItem?> EnsureAggregationQueuedAsync(string runId, string parentWorkItemId, CancellationToken ct = default);
    Task<WorkQueueItem?> GetAsync(string runId, string workItemId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkQueueItem>> ListByRunAsync(string runId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkQueueItem>> ListRecoverableAsync(CancellationToken ct = default);
    Task<WorkQueueItem?> TryStartAsync(string runId, string workItemId, string workerId, CancellationToken ct = default);
    Task<WorkQueueItem> MarkProgressAsync(string runId, string workItemId, string progressMessage, CancellationToken ct = default);
    Task<WorkQueueItem> MarkCompletedAsync(string runId, string workItemId, string progressMessage, CancellationToken ct = default);
    Task<WorkQueueItem> MarkFailedAsync(string runId, string workItemId, string errorMessage, CancellationToken ct = default);
}