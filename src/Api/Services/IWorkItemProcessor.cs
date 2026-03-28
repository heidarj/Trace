namespace Trace.Api.Services;

public interface IWorkItemProcessor
{
    Task ProcessAsync(QueuedWorkReference work, string workerId, CancellationToken ct = default);
}