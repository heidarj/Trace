using Trace.Api.Repositories;
using Trace.Contracts;

namespace Trace.Api.Services;

public class InvestigationService : IInvestigationService
{
    private readonly IInvestigationRepository _runRepo;
    private readonly ITenantResultRepository _tenantRepo;
    private readonly IWorkQueueService _workQueue;
    private readonly IWorkDispatcher _dispatcher;
    private readonly ILogger<InvestigationService> _logger;

    public InvestigationService(
        IInvestigationRepository runRepo,
        ITenantResultRepository tenantRepo,
        IWorkQueueService workQueue,
        IWorkDispatcher dispatcher,
        ILogger<InvestigationService> logger)
    {
        _runRepo = runRepo;
        _tenantRepo = tenantRepo;
        _workQueue = workQueue;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<InvestigationRun> StartInvestigationAsync(CveInvestigationRequest request, string createdBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting investigation for CVE {CveId} across {TenantCount} tenants",
            request.CveId, request.TenantIds.Count);

        var runId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        var run = new InvestigationRun(
            runId,
            request.CveId,
            request.Title,
            request.Description,
            InvestigationStatus.Pending,
            now,
            null,
            request.TenantIds.Count,
            0,
            0,
            createdBy,
            WorkflowStage.Queued,
            "Queued CVE research.",
            now
        );

        var created = await _runRepo.CreateAsync(run, ct);

        var tenantTasks = request.TenantIds.Select(tenantId =>
            _tenantRepo.CreateAsync(new TenantInvestigationResult(
                $"{runId}:{tenantId}:summary",
                runId,
                tenantId,
                $"Tenant {tenantId[..Math.Min(8, tenantId.Length)]}",
                ExposureVerdict.Unknown,
                InvestigationStatus.Pending,
                ReviewStatus.Pending,
                now,
                null,
                0,
                null,
                null,
                null,
                WorkflowStage.Queued,
                "Queued for tenant investigation."
            ), ct));

        await Task.WhenAll(tenantTasks);

        var researchWork = await _workQueue.QueueResearchAsync(created.Id, ct);
        await _dispatcher.QueueAsync(new QueuedWorkReference(created.Id, researchWork.Id), ct);

        return created;
    }

    public async Task<InvestigationRun?> GetRunAsync(string runId, CancellationToken ct = default)
    {
        var run = await _runRepo.GetByIdAsync(runId, ct);
        if (run is null)
        {
            return null;
        }

        var tenantResults = await _tenantRepo.ListByRunAsync(runId, ct);
        return HydrateRun(run, tenantResults);
    }

    public async Task<IReadOnlyList<InvestigationRun>> ListRecentRunsAsync(int limit = 20, CancellationToken ct = default)
    {
        var runs = await _runRepo.ListRecentAsync(limit, ct);
        var hydrated = new List<InvestigationRun>(runs.Count);

        foreach (var run in runs)
        {
            var tenantResults = await _tenantRepo.ListByRunAsync(run.Id, ct);
            hydrated.Add(HydrateRun(run, tenantResults));
        }

        return hydrated;
    }

    public async Task<IReadOnlyList<TenantInvestigationResult>> GetTenantResultsAsync(string runId, CancellationToken ct = default) =>
        await _tenantRepo.ListByRunAsync(runId, ct);

    public async Task<TenantInvestigationResult?> GetTenantResultAsync(string runId, string tenantId, CancellationToken ct = default) =>
        await _tenantRepo.GetAsync(runId, tenantId, ct);

    public async Task<TenantInvestigationResult> ReviewTenantResultAsync(string runId, string tenantId, ReviewDecision decision, CancellationToken ct = default)
    {
        _logger.LogInformation("Reviewing tenant result {RunId}/{TenantId} as {Decision}", runId, tenantId, decision.Decision);

        var existing = await _tenantRepo.GetAsync(runId, tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant result not found: {runId}/{tenantId}");

        var updated = existing with
        {
            ReviewStatus = decision.Decision,
            ReviewedBy = decision.ReviewedBy,
            ReviewedAt = DateTimeOffset.UtcNow,
            ReviewNotes = decision.Notes
        };

        return await _tenantRepo.UpdateAsync(updated, ct);
    }

    private static InvestigationRun HydrateRun(InvestigationRun run, IReadOnlyList<TenantInvestigationResult> tenantResults)
    {
        if (tenantResults.Count == 0)
        {
            return run;
        }

        var tenantsCompleted = tenantResults.Count(result => result.Status == InvestigationStatus.Completed);
        var findingsCount = tenantResults.Sum(result => result.FindingsCount);

        var progressMessage = run.CurrentStage == WorkflowStage.TenantFanOut && run.Status is InvestigationStatus.Pending or InvestigationStatus.Running
            ? $"Tenant investigations completed for {tenantsCompleted} of {run.TotalTenants} tenants."
            : run.ProgressMessage;

        return run with
        {
            TenantsCompleted = tenantsCompleted,
            FindingsCount = findingsCount,
            ProgressMessage = progressMessage
        };
    }
}
