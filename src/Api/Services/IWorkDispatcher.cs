namespace Trace.Api.Services;

public readonly record struct QueuedWorkReference(string RunId, string WorkItemId);

public interface IWorkDispatcher
{
    ValueTask QueueAsync(QueuedWorkReference work, CancellationToken ct = default);
    IAsyncEnumerable<QueuedWorkReference> ReadAllAsync(CancellationToken ct = default);
}