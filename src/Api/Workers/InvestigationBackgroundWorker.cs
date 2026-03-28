using System.Collections.Concurrent;
using Trace.Api.Services;

namespace Trace.Api.Workers;

public class InvestigationBackgroundWorker : BackgroundService
{
    private const int MaxConcurrency = 4;

    private readonly IWorkDispatcher _dispatcher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvestigationBackgroundWorker> _logger;
    private readonly SemaphoreSlim _concurrencyGate = new(MaxConcurrency, MaxConcurrency);
    private readonly ConcurrentDictionary<string, Task> _activeTasks = new();
    private readonly string _workerId = $"{Environment.MachineName}:{Environment.ProcessId}";

    public InvestigationBackgroundWorker(
        IWorkDispatcher dispatcher,
        IServiceScopeFactory scopeFactory,
        ILogger<InvestigationBackgroundWorker> logger)
    {
        _dispatcher = dispatcher;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueRecoverableWorkAsync(stoppingToken);

        await foreach (var work in _dispatcher.ReadAllAsync(stoppingToken))
        {
            await _concurrencyGate.WaitAsync(stoppingToken);

            var task = ProcessWorkAsync(work, stoppingToken);
            var key = $"{work.RunId}:{work.WorkItemId}";
            _activeTasks[key] = task;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_activeTasks.Count > 0)
        {
            await Task.WhenAll(_activeTasks.Values);
        }
    }

    private async Task RequeueRecoverableWorkAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var workQueue = scope.ServiceProvider.GetRequiredService<IWorkQueueService>();
        var recoverable = await workQueue.ListRecoverableAsync(ct);

        foreach (var item in recoverable)
        {
            _logger.LogInformation("Re-queueing recoverable work item {RunId}/{WorkItemId}", item.RunId, item.Id);
            await _dispatcher.QueueAsync(new QueuedWorkReference(item.RunId, item.Id), ct);
        }
    }

    private async Task ProcessWorkAsync(QueuedWorkReference work, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IWorkItemProcessor>();
            await processor.ProcessAsync(work, _workerId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Stopping work processing for {RunId}/{WorkItemId}", work.RunId, work.WorkItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {RunId}/{WorkItemId}", work.RunId, work.WorkItemId);
        }
        finally
        {
            _activeTasks.TryRemove($"{work.RunId}:{work.WorkItemId}", out _);
            _concurrencyGate.Release();
        }
    }
}