using Trace.Api.Repositories;
using Trace.Api.Workflows;
using Trace.Contracts;

namespace Trace.Api.Services;

public class WorkItemProcessor : IWorkItemProcessor
{
    private readonly IWorkQueueService _workQueue;
    private readonly IWorkDispatcher _dispatcher;
    private readonly IInvestigationRepository _runRepository;
    private readonly ITenantResultRepository _tenantRepository;
    private readonly ICveResearchWorkflow _researchWorkflow;
    private readonly ITenantInvestigationWorkflow _tenantWorkflow;
    private readonly IFindingCorrelator _findingCorrelator;
    private readonly ILogger<WorkItemProcessor> _logger;

    public WorkItemProcessor(
        IWorkQueueService workQueue,
        IWorkDispatcher dispatcher,
        IInvestigationRepository runRepository,
        ITenantResultRepository tenantRepository,
        ICveResearchWorkflow researchWorkflow,
        ITenantInvestigationWorkflow tenantWorkflow,
        IFindingCorrelator findingCorrelator,
        ILogger<WorkItemProcessor> logger)
    {
        _workQueue = workQueue;
        _dispatcher = dispatcher;
        _runRepository = runRepository;
        _tenantRepository = tenantRepository;
        _researchWorkflow = researchWorkflow;
        _tenantWorkflow = tenantWorkflow;
        _findingCorrelator = findingCorrelator;
        _logger = logger;
    }

    public async Task ProcessAsync(QueuedWorkReference work, string workerId, CancellationToken ct = default)
    {
        var leased = await _workQueue.TryStartAsync(work.RunId, work.WorkItemId, workerId, ct);
        if (leased is null)
        {
            return;
        }

        try
        {
            switch (leased.WorkType)
            {
                case WorkItemType.Research:
                    await ProcessResearchAsync(leased, ct);
                    break;
                case WorkItemType.TenantInvestigation:
                    await ProcessTenantAsync(leased, ct);
                    break;
                case WorkItemType.Aggregation:
                    await ProcessAggregationAsync(leased, ct);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported work type: {leased.WorkType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work item {RunId}/{WorkItemId} failed", leased.RunId, leased.Id);
            await _workQueue.MarkFailedAsync(leased.RunId, leased.Id, ex.Message, ct);
            await HandleRunOrTenantFailureAsync(leased, ex.Message, ct);

            if (leased.WorkType == WorkItemType.TenantInvestigation)
            {
                var aggregation = await _workQueue.EnsureAggregationQueuedAsync(leased.RunId, leased.Id, ct);
                if (aggregation is not null)
                {
                    await _dispatcher.QueueAsync(new QueuedWorkReference(aggregation.RunId, aggregation.Id), ct);
                }
            }
        }
    }

    private async Task ProcessResearchAsync(WorkQueueItem work, CancellationToken ct)
    {
        var run = await GetRequiredRunAsync(work.RunId, ct);
        var tenants = await _tenantRepository.ListByRunAsync(work.RunId, ct);

        await _workQueue.MarkProgressAsync(work.RunId, work.Id, "Research started.", ct);
        await _runRepository.UpdateAsync(run with
        {
            Status = InvestigationStatus.Running,
            CurrentStage = WorkflowStage.Research,
            ProgressMessage = "Researching and normalizing CVE context.",
            LastCheckpointAt = DateTimeOffset.UtcNow
        }, ct);

        var context = await _researchWorkflow.ExecuteAsync(run, tenants.Select(result => result.TenantId).ToList(), ct);
        await _tenantRepository.SaveContextAsync(context, ct);

        await _workQueue.MarkCompletedAsync(work.RunId, work.Id, "Research completed.", ct);
        await _runRepository.UpdateAsync(run with
        {
            Status = InvestigationStatus.Running,
            CurrentStage = WorkflowStage.TenantFanOut,
            ProgressMessage = $"Research complete. Queued {tenants.Count} tenant investigations.",
            LastCheckpointAt = DateTimeOffset.UtcNow
        }, ct);

        foreach (var tenant in tenants)
        {
            await _tenantRepository.UpdateAsync(tenant with
            {
                CurrentStage = WorkflowStage.Queued,
                ProgressMessage = "Queued for tenant investigation."
            }, ct);

            var tenantWork = await _workQueue.QueueTenantInvestigationAsync(work.RunId, tenant.TenantId, work.Id, ct);
            await _dispatcher.QueueAsync(new QueuedWorkReference(tenantWork.RunId, tenantWork.Id), ct);
        }
    }

    private async Task ProcessTenantAsync(WorkQueueItem work, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(work.TenantId))
        {
            throw new InvalidOperationException("Tenant work items require a tenant ID.");
        }

        var run = await GetRequiredRunAsync(work.RunId, ct);
        var tenant = await _tenantRepository.GetAsync(work.RunId, work.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant result not found: {work.RunId}/{work.TenantId}");
        var context = await _tenantRepository.GetContextAsync(work.RunId, ct)
            ?? throw new KeyNotFoundException($"Investigation context not found: {work.RunId}");

        await _workQueue.MarkProgressAsync(work.RunId, work.Id, $"Collecting evidence for tenant {tenant.TenantName}.", ct);
        await _tenantRepository.UpdateAsync(tenant with
        {
            Status = InvestigationStatus.Running,
            CurrentStage = WorkflowStage.TenantFanOut,
            ProgressMessage = "Collecting tenant evidence.",
            StartedAt = tenant.StartedAt == default ? DateTimeOffset.UtcNow : tenant.StartedAt
        }, ct);

        var result = await _tenantWorkflow.ExecuteAsync(run, tenant, context, ct);
        foreach (var finding in result.Findings)
        {
            await _tenantRepository.AddFindingAsync(finding, ct);
        }

        await _tenantRepository.UpdateAsync(tenant with
        {
            Verdict = result.Verdict,
            Status = InvestigationStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            FindingsCount = result.Findings.Count,
            CurrentStage = WorkflowStage.Completed,
            ProgressMessage = result.ProgressMessage
        }, ct);
        await _workQueue.MarkCompletedAsync(work.RunId, work.Id, result.ProgressMessage, ct);

        var aggregation = await _workQueue.EnsureAggregationQueuedAsync(work.RunId, work.Id, ct);
        if (aggregation is not null)
        {
            await _dispatcher.QueueAsync(new QueuedWorkReference(aggregation.RunId, aggregation.Id), ct);
        }
    }

    private async Task ProcessAggregationAsync(WorkQueueItem work, CancellationToken ct)
    {
        var run = await GetRequiredRunAsync(work.RunId, ct);
        await _workQueue.MarkProgressAsync(work.RunId, work.Id, "Aggregating tenant findings.", ct);
        await _runRepository.UpdateAsync(run with
        {
            Status = InvestigationStatus.Running,
            CurrentStage = WorkflowStage.Aggregation,
            ProgressMessage = "Aggregating tenant results.",
            LastCheckpointAt = DateTimeOffset.UtcNow
        }, ct);

        var tenantResults = await _tenantRepository.ListByRunAsync(work.RunId, ct);
        var correlation = await _findingCorrelator.CorrelateAsync(tenantResults, ct);

        await _runRepository.UpdateAsync(run with
        {
            Status = correlation.Status,
            CompletedAt = DateTimeOffset.UtcNow,
            TenantsCompleted = correlation.TenantsCompleted,
            FindingsCount = correlation.FindingsCount,
            CurrentStage = correlation.FinalStage,
            ProgressMessage = correlation.ProgressMessage,
            LastCheckpointAt = DateTimeOffset.UtcNow
        }, ct);
        await _workQueue.MarkCompletedAsync(work.RunId, work.Id, correlation.ProgressMessage, ct);
    }

    private async Task HandleRunOrTenantFailureAsync(WorkQueueItem work, string errorMessage, CancellationToken ct)
    {
        switch (work.WorkType)
        {
            case WorkItemType.Research:
            case WorkItemType.Aggregation:
            {
                var run = await GetRequiredRunAsync(work.RunId, ct);
                await _runRepository.UpdateAsync(run with
                {
                    Status = InvestigationStatus.Failed,
                    CompletedAt = DateTimeOffset.UtcNow,
                    CurrentStage = WorkflowStage.Failed,
                    ProgressMessage = errorMessage,
                    LastCheckpointAt = DateTimeOffset.UtcNow
                }, ct);
                break;
            }

            case WorkItemType.TenantInvestigation when !string.IsNullOrWhiteSpace(work.TenantId):
            {
                var tenant = await _tenantRepository.GetAsync(work.RunId, work.TenantId, ct);
                if (tenant is not null)
                {
                    await _tenantRepository.UpdateAsync(tenant with
                    {
                        Status = InvestigationStatus.Failed,
                        CompletedAt = DateTimeOffset.UtcNow,
                        CurrentStage = WorkflowStage.Failed,
                        ProgressMessage = errorMessage
                    }, ct);
                }
                break;
            }
        }
    }

    private async Task<InvestigationRun> GetRequiredRunAsync(string runId, CancellationToken ct)
    {
        return await _runRepository.GetByIdAsync(runId, ct)
            ?? throw new KeyNotFoundException($"Investigation run not found: {runId}");
    }
}