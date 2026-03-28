using Trace.Contracts;

namespace Trace.Api.Repositories;

public interface IWorkQueueRepository
{
    Task<WorkQueueItem> CreateAsync(WorkQueueItem item, CancellationToken ct = default);
    Task<WorkQueueItem?> GetAsync(string runId, string id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkQueueItem>> ListByRunAsync(string runId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkQueueItem>> ListPendingOrExpiredAsync(DateTimeOffset now, CancellationToken ct = default);
    Task<WorkQueueItem?> TryLeaseAsync(string runId, string id, string workerId, DateTimeOffset leaseExpiresAt, CancellationToken ct = default);
    Task<WorkQueueItem> UpdateAsync(WorkQueueItem item, CancellationToken ct = default);
}